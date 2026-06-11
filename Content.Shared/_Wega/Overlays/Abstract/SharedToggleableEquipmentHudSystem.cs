using Content.Shared.Actions;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Overlays;
using Content.Shared.Popups;
using Content.Shared.PowerCell;
using Content.Shared.Toggleable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Overlay;

public abstract partial class SharedToggleableEquipmentHudSystem<T> : EntitySystem where T : ToggleableHudComponent
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private PowerCellSystem _powerCell = default!;
    [Dependency] private ActionContainerSystem _actionContainer = default!;
    [Dependency] private ItemToggleSystem _toggle = default!;
    [Dependency] private IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<T, ToggleActionEvent>(OnToggleAction);
        SubscribeLocalEvent<T, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<T, GetItemActionsEvent>(OnGetItemActions);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<T>();
        while (query.MoveNext(out var uid, out var hud))
        {
            if (!hud.Enabled)
                continue;

            if (hud.NextChargeCheck >= _gameTiming.CurTime)
                continue;

            if (!_powerCell.HasDrawCharge(uid))
            {
                TurnOff((uid, hud));
                continue;
            }

            hud.NextChargeCheck = _gameTiming.CurTime + hud.ChargeCheckInterval;
        }
    }

    private void OnMapInit(Entity<T> ent, ref MapInitEvent args)
    {
        _actionContainer.EnsureAction(ent, ref ent.Comp.ActionEntity, ent.Comp.ToggleAction);
    }

    private void OnGetItemActions(Entity<T> ent, ref GetItemActionsEvent args)
    {
        if (args.InHands)
            return;

        args.AddAction(ref ent.Comp.ActionEntity, ent.Comp.ToggleAction);
        _actions.SetToggled(ent.Comp.ActionEntity, ent.Comp.Enabled);
    }

    private void OnToggleAction(Entity<T> ent, ref ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (!ent.Comp.Enabled)
        {
            if (!_powerCell.HasDrawCharge(ent.Owner, user: args.Performer))
            {
                _audio.PlayPvs(ent.Comp.ActivateFailSound, ent);
                _popup.PopupEntity(Loc.GetString("toggleable-hud-no-power"), ent, args.Performer);
                return;
            }

            TryActivate(ent);
        }
        else
        {
            TurnOff(ent);
        }
    }

    private bool TryActivate(Entity<T> ent)
    {
        if (!_toggle.TryActivate(ent.Owner))
            return false;

        ent.Comp.Enabled = true;
        ent.Comp.NextChargeCheck = _gameTiming.CurTime;

        _actions.SetToggled(ent.Comp.ActionEntity, ent.Comp.Enabled);
        Dirty(ent);

        return true;
    }

    private void TurnOff(Entity<T> ent)
    {
        if (!ent.Comp.Enabled)
            return;

        ent.Comp.Enabled = false;
        _actions.SetToggled(ent.Comp.ActionEntity, ent.Comp.Enabled);
        _toggle.TryDeactivate(ent.Owner);
        Dirty(ent);
    }
}
