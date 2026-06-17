using Content.Shared.Administration.Logs;
using Content.Shared.Damage.Components;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Robust.Shared.Random;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Content.Shared.Random;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Effects;
using Content.Shared.Hands;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.DamageOnPickupSystem.Systems;

public sealed partial class DamageOnInteractSystem : EntitySystem
{
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private DamageableSystem _damageableSystem = default!;
    [Dependency] private SharedAudioSystem _audioSystem = default!;
    [Dependency] private SharedPopupSystem _popupSystem = default!;
    [Dependency] private InventorySystem _inventorySystem = default!;
    [Dependency] private ThrowingSystem _throwingSystem = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageOnPickupComponent, BeforeGettingEquippedHandEvent>(OnEquipped);
    }

    public void SetIsDamageActiveTo(Entity<DamageOnPickupComponent> entity, bool mode)
    {
        if (entity.Comp.IsDamageActive == mode)
            return;

        entity.Comp.IsDamageActive = mode;
        Dirty(entity);
    }
	
    private void OnEquipped(Entity<DamageOnPickupComponent> entity, ref BeforeGettingEquippedHandEvent args)
    {
        if (!entity.Comp.Throw || !TryComp<PullableComponent>(entity, out var pullComp) || pullComp.BeingPulled)
            return;

        if (!entity.Comp.IsDamageActive)
            return;

        var totalDamage = entity.Comp.Damage;
		
        if (!entity.Comp.IgnoreResistances)
        {
            _inventorySystem.TryGetInventoryEntity<DamageOnInteractProtectionComponent>(args.User, out var protectiveEntity);
            
            if (protectiveEntity.Comp == null && TryComp<DamageOnInteractProtectionComponent>(args.User, out var protectiveComp))
                protectiveEntity = (args.User, protectiveComp);

            // if protectiveComp isn't null after all that, it means the user has protection
            if (protectiveEntity.Comp != null)
            {
                totalDamage = DamageSpecifier.ApplyModifierSet(totalDamage, protectiveEntity.Comp.DamageProtection);
            }
        }
		
        totalDamage = _damageableSystem.ChangeDamage(args.User, totalDamage, origin: entity.Owner);

        if (totalDamage.AnyPositive())
        {
            _adminLogger.Add(LogType.Damaged, $"{ToPrettyString(args.User):user} injured their hand by picking the {ToPrettyString(entity.Owner):target} and received {totalDamage.GetTotal():damage} damage");
            _audioSystem.PlayPredicted(entity.Comp.InteractSound, entity.Owner, args.User);

            if (entity.Comp.PopupText != null)
                _popupSystem.PopupClient(Loc.GetString(entity.Comp.PopupText), args.User, args.User);
			
			args.Cancelled = true;
			_transform.SetCoordinates(entity, Transform(args.User).Coordinates);
			_transform.AttachToGridOrMap(entity);
			_throwingSystem.TryThrow(entity, _random.NextVector2(), entity.Comp.ThrowSpeed, doSpin: true);
		}
    }
}
