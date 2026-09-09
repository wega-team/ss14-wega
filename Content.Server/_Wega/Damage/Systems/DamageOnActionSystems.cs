using Content.Shared.Damage.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.Actions;
using Content.Server.Popups;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;

namespace Content.Server.Damage.Systems;

public sealed partial class DamageOnActionSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SatiationSystem _satiation = default!;
    [Dependency] private PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageOnActionComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<DamageOnActionComponent, DamageOnActionEvent>(OnAction);
    }

    private void OnCompInit(Entity<DamageOnActionComponent> ent, ref ComponentInit args)
    {
        _actions.AddAction(ent.Owner, ref ent.Comp.ActionEntity, ent.Comp.Action, ent.Owner);
    }

    private void OnAction(Entity<DamageOnActionComponent> ent, ref DamageOnActionEvent args)
    {
        if (!TryComp<SatiationComponent>(ent.Owner, out var satiation))
            return;

        if (!HasComp<DamageableComponent>(ent.Owner))
            return;

        if (_satiation.GetValueOrNull((ent.Owner, satiation), SatiationSystem.Hunger) < ent.Comp.HungerPerUse)
        {
            _popup.PopupEntity(Loc.GetString("damage-action-too-hungry"), ent.Owner, ent.Owner);
            return;
        }

        _satiation.ModifyValue((ent.Owner, satiation), SatiationSystem.Hunger, -ent.Comp.HungerPerUse);
        _damageable.TryChangeDamage(ent.Owner, ent.Comp.Damage, true, false);
        _actions.SetCooldown(ent.Comp.ActionEntity, ent.Comp.Delay);
    }
}
