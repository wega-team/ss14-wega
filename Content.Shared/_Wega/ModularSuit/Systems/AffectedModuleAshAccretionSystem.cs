using Content.Shared.Armor;
using Content.Shared.Clothing.Upgrades;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Explosion;
using Content.Shared.Inventory;
using Content.Shared.Movement.Systems;

namespace Content.Shared.Modular.Suit;

public sealed partial class AffectedModuleAshAccretionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AffectedModuleAshAccretionComponent, InventoryRelayedEvent<DamageModifyEvent>>(OnDamageModify,
            after: [typeof(SharedArmorSystem), typeof(ClothingUpgradeSystem)]);
        SubscribeLocalEvent<AffectedModuleAshAccretionComponent, InventoryRelayedEvent<RefreshMovementSpeedModifiersEvent>>(OnRefreshMovementSpeed);
        SubscribeLocalEvent<AffectedModuleAshAccretionComponent, InventoryRelayedEvent<GetExplosionResistanceEvent>>(OnGetExplosionResistance);
    }

    private void OnDamageModify(Entity<AffectedModuleAshAccretionComponent> ent, ref InventoryRelayedEvent<DamageModifyEvent> args)
    {
        if (!ent.Comp.Engaged)
            return;

        args.Args.Damage = DamageSpecifier.ApplyModifierSet(args.Args.Damage, ent.Comp.Modifiers);
    }

    private void OnRefreshMovementSpeed(Entity<AffectedModuleAshAccretionComponent> ent, ref InventoryRelayedEvent<RefreshMovementSpeedModifiersEvent> args)
    {
        if (!ent.Comp.Engaged)
            return;

        args.Args.ModifySpeed(ent.Comp.WalkModifier, ent.Comp.SprintModifier);
    }

    private void OnGetExplosionResistance(Entity<AffectedModuleAshAccretionComponent> ent, ref InventoryRelayedEvent<GetExplosionResistanceEvent> args)
    {
        if (!ent.Comp.Engaged)
            return;

        args.Args.DamageCoefficient *= ent.Comp.ExplosionCoefficient;
    }
}
