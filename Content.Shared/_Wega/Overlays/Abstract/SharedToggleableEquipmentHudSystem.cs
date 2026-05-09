using Content.Shared.Actions;
using Content.Shared.Inventory.Events;
using Content.Shared.Overlays;
using Content.Shared.Toggleable;
using Content.Shared.Actions.Components;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Overlay;

public abstract class SharedToggleableEquipmentHudSystem<T> : EntitySystem where T : ToggleableHudComponent
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!; 
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly SharedBatterySystem _battery = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

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
            if (hud.Wattage == 0 || !hud.Enabled)
                continue;
            
            if (!_powerCell.TryGetBatteryFromSlotOrEntity(uid, out var battery))
            {
                TurnOff((uid, hud));
                continue;
            }
            
            if (hud.Enabled && !_battery.TryUseCharge(battery.Value.AsNullable(), hud.Wattage * frameTime))
            {           
                TurnOff((uid, hud));
                continue;
            }
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
        if (!ent.Comp.Enabled && ent.Comp.Wattage != 0)
        {
            if (!_powerCell.TryGetBatteryFromSlotOrEntity(ent.Owner, out var battery))
            {
                _audio.PlayPvs(_audio.ResolveSound(ent.Comp.ActivateFailSound), ent);
                _popup.PopupEntity(Loc.GetString("handheld-light-component-cell-missing-message"), ent, args.Performer);
                return;
            }

            if (ent.Comp.Wattage > _battery.GetCharge(battery.Value.AsNullable()))
            {
                _audio.PlayPvs(_audio.ResolveSound(ent.Comp.ActivateFailSound), ent);
                _popup.PopupEntity(Loc.GetString("handheld-light-component-cell-dead-message"), ent, args.Performer);
                return;
            }
        }
        ent.Comp.Enabled = !ent.Comp.Enabled;
        
        _actions.SetToggled(ent.Comp.ActionEntity, ent.Comp.Enabled);
        _appearance.SetData(ent.Owner, ToggleableVisuals.Enabled, ent.Comp.Enabled);
        Dirty(ent);
        
        if (!ent.Comp.Enabled && ent.Comp.ActivateSound != null)
            _audio.PlayPvs(_audio.ResolveSound(ent.Comp.ActivateSound), ent.Owner);
        
        else if (ent.Comp.Enabled && ent.Comp.DeactivateSound != null)
            _audio.PlayPvs(_audio.ResolveSound(ent.Comp.DeactivateSound), ent.Owner);
    }
    
    private void TurnOff(Entity<T> ent)
    {
        ent.Comp.Enabled = false;
        _appearance.SetData(ent.Owner, ToggleableVisuals.Enabled, ent.Comp.Enabled);
        Dirty(ent.Owner, ent.Comp);
        if (ent.Comp.DeactivateSound != null)
            _audio.PlayPvs(_audio.ResolveSound(ent.Comp.DeactivateSound), ent.Owner);
    }
}
