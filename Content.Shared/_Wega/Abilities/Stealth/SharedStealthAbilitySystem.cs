using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Examine;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Stealth.Components;
using Content.Shared.Stealth;
using Content.Shared.Cloning.Events;
using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Content.Shared.Stealth;

public sealed partial class SharedStealthAbilitySystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedStealthSystem _stealth = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StealthAbilityComponent, MapInitEvent >(OnInit);
        SubscribeLocalEvent<StealthAbilityComponent, ComponentShutdown>(OnShutdown);
		
        SubscribeLocalEvent<StealthAbilityComponent, SteathAbilityEvent>(OnAction);
		
        SubscribeLocalEvent<StealthAbilityComponent, CloningEvent>(OnClone);
	}

    private void OnInit(Entity<StealthAbilityComponent> entity, ref MapInitEvent args)
    {
        if (!HasComp<StealthComponent>(entity))
        {
            var stealth = EnsureComp<StealthComponent>(entity);
            _stealth.SetVisibility(entity, entity.Comp.StealthСoefficient, stealth);
            _stealth.SetEnabled(entity, false, stealth);
        }	
	
        if (!TryComp(entity, out ActionsComponent? comp))
            return;

        _actions.AddAction(entity, ref entity.Comp.ActionEntity, entity.Comp.Action, component: comp);
	}

    private void OnShutdown(Entity<StealthAbilityComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
		
        if (HasComp<StealthComponent>(ent))
            RemComp<StealthComponent>(ent);
    }

    private void OnAction(Entity<StealthAbilityComponent> ent, ref SteathAbilityEvent args)
    {	
        if (TryComp(ent, out StealthComponent? steal))
        {
            _stealth.SetEnabled(ent, true, steal);
			Timer.Spawn(ent.Comp.Time, () => _stealth.SetEnabled(ent, false, steal));
        }

        args.Handled = true;
    }

    private void OnClone(Entity<StealthAbilityComponent> ent, ref CloningEvent args)
    {
        if (!args.Settings.EventComponents.Contains(Factory.GetRegistration(ent.Comp.GetType()).Name))
            return;

        // Make sure to set the datafields before adding the component so that the correct action gets spawned on map init.
        var targetComp = Factory.GetComponent<StealthAbilityComponent>();
        targetComp.Action = ent.Comp.Action;
        targetComp.Time = ent.Comp.Time;
        targetComp.StealthСoefficient = ent.Comp.StealthСoefficient;
        AddComp(args.CloneUid, targetComp, true);
    }
}