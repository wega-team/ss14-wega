using Content.Shared.Actions;
using Content.Shared.Inventory.Events;
using Content.Shared.Overlays;
using Content.Shared.Toggleable;
using Content.Shared.Actions.Components;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.Overlay;

public abstract class SharedToggleableEquipmentHudSystem<T> : EntitySystem where T : ToggleableHudComponent
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
	[Dependency] private readonly SharedAudioSystem _audio = default!; 

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<T, ToggleActionEvent>(OnToggleAction);
        SubscribeLocalEvent<T, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<T, GotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<T, ComponentShutdown>(OnShutdown);
    }

    private void OnGotEquipped(Entity<T> ent, ref GotEquippedEvent args)
    {
        _actions.AddAction(args.EquipTarget, ref ent.Comp.ActionEntity, ent.Comp.ToggleAction, ent);
        _actions.SetToggled(ent.Comp.ActionEntity, ent.Comp.Enabled);
    }

    private void OnGotUnequipped(Entity<T> ent, ref GotUnequippedEvent args)
    {
        _actions.RemoveAction(args.EquipTarget, ent.Comp.ActionEntity);
    }

    private void OnShutdown(Entity<T> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.ActionEntity is { } action && TryComp<ActionComponent>(action, out var actionComp))
        {
			if (actionComp.Container != null && TryComp<ActionsComponent>(actionComp.Container, out var container))
				_actions.RemoveAction((actionComp.Container.Value, container), action);
        }
    }

    private void OnToggleAction(Entity<T> ent, ref ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        ent.Comp.Enabled = !ent.Comp.Enabled;
        
        _actions.SetToggled(ent.Comp.ActionEntity, ent.Comp.Enabled);
        Dirty(ent);
		if (ent.Comp.ActivateSound != null && ent.Comp.DeactivateSound != null)
		{
			if (ent.Comp.Enabled)
				_audio.PlayGlobal(_audio.ResolveSound(ent.Comp.ActivateSound), args.Performer);
			else
				_audio.PlayGlobal(_audio.ResolveSound(ent.Comp.DeactivateSound), args.Performer);
		}
	}
}
