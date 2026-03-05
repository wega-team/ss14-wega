using Content.Shared.Damage.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.Actions;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Components;
using Content.Server.Popups;

namespace Content.Server.Damage.Systems;

public sealed class DamageOnActionSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

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
        if (!TryComp<HungerComponent>(ent.Owner, out var hunger))
            return;

        if (!HasComp<DamageableComponent>(ent.Owner))
            return;

        if (_hunger.GetHunger(hunger) < ent.Comp.HungerPerUse)
        {
            _popup.PopupEntity(Loc.GetString("damage-action-too-hungry"), ent.Owner, ent.Owner);
            return;
        }

        _hunger.ModifyHunger(ent, -ent.Comp.HungerPerUse, hunger);
        _damageable.TryChangeDamage(ent.Owner, ent.Comp.Damage, true, false);
        _actions.SetCooldown(ent.Comp.ActionEntity, ent.Comp.Delay);
    }
}
