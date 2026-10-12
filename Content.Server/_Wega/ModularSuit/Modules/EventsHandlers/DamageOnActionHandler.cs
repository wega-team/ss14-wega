using Content.Shared.Modular.Suit;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Physics;
using Content.Server.Popups;
using Robust.Shared.Serialization;
using Robust.Shared.Map;

namespace Content.Server.Modular.Suit;

public sealed partial class DamageOnActionHandler : ModuleActionHandler
{
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SatiationSystem _satiation = default!;
    [Dependency] private PopupSystem _popup = default!;


    public override void Initialize()
    {
        SubscribeLocalEvent<ModularSuitActionHolderComponent, DamageOnActionModuleEvent>(OnActivate);
    }

    private void OnActivate(Entity<ModularSuitActionHolderComponent> ent, ref DamageOnActionModuleEvent args)
    {
        if (args.Handled)
            return;

        if (!TryFindModuleByAction(ent, args.Action, out var moduleEnt))
            return;

        if (!TryComp<ModularSuitModuleComponent>(moduleEnt, out var moduleComp) || !moduleComp.IsActive)
            return;

        var attemptEvent = new ModularSuitModuleAttemptEvent(ent.Owner);
        RaiseLocalEvent(moduleEnt.Value, ref attemptEvent);

        if (attemptEvent.Cancelled)
            return;

        if (PerformerDamage(args.Performer, args.Damage, args.HungerPerUse))
        {
            ModularSuit.UseCoreCharge(ent.Owner, moduleComp.PowerInstanceUsage);
        }

        args.Handled = true;
    }

    private bool PerformerDamage(EntityUid user, DamageSpecifier damageSpecifier ,float hunger)
    {
        if (!TryComp<SatiationComponent>(user, out var satiation))
            return false;

        if (!HasComp<DamageableComponent>(user))
            return false;

        if (_satiation.GetValueOrNull((user, satiation), SatiationSystem.Hunger) < hunger)
        {
            _popup.PopupEntity(Loc.GetString("damage-action-too-hungry"), user, user);
            return false;
        }

        _satiation.ModifyValue((user, satiation), SatiationSystem.Hunger, -hunger);
        _damageable.TryChangeDamage(user, damageSpecifier, true, false);
		
		return true;
    }
}
