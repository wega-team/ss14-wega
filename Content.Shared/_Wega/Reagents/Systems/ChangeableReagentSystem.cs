using Content.Shared.Access.Systems;
using Content.Shared.Verbs;
using Content.Shared.Popups;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.ChangeableReagent.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.ChangeableReagent;

public sealed partial class ChangeableReagentSystem : EntitySystem
{
    [Dependency] private AccessReaderSystem _accessReaderSystem = default!;
    [Dependency] private SharedPopupSystem _popupSystem = default!;
    [Dependency] private SolutionRegenerationSystem _solutionrRagents = default!;
    [Dependency] private IPrototypeManager _protoMan = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangeableReagentComponent, UseInHandEvent>(OnUseInHandEvent);
        SubscribeLocalEvent<ChangeableReagentComponent, GetVerbsEvent<Verb>>(OnGetVerb);
        SubscribeLocalEvent<ChangeableReagentComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(EntityUid uid, ChangeableReagentComponent component, ExaminedEvent args)
    {
        if (component.Reagents.Count < 2)
            return;

        var account = GetReagent(component);
        if (account.Reagent.Count == 0) 
            return;

        if (!_protoMan.TryIndex<ReagentPrototype>(account.Reagent[0].Reagent.Prototype, out var proto)) 
            return;

        args.PushMarkup(Loc.GetString("set-reagent", ("reagent", Loc.GetString(proto.LocalizedName))));
    }

    private ChangeableReagents GetReagent(ChangeableReagentComponent component)
    {
        return component.Reagents[component.CurrentReagent];
    }

    private void OnGetVerb(EntityUid uid, ChangeableReagentComponent component, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.CanComplexInteract)
            return;

        if (component.Reagents.Count < 2)
            return;

        if (!_accessReaderSystem.IsAllowed(args.User, uid))
            return;

        for (var i = 0; i < component.Reagents.Count; i++)
        {
            var reagent = component.Reagents[i];
            var entProto = _protoMan.Index<ReagentPrototype>(reagent.Reagent[0].Reagent.Prototype);
            var index = i;

            var v = new Verb
            {
                Priority = 1,
                Category = VerbCategory.ReagentChange,
                Text = Loc.GetString(entProto.LocalizedName),
                Disabled = i == component.CurrentReagent,
                Impact = LogImpact.Low,
                DoContactInteraction = true,
                Act = () =>
                {
                    TrySetReagent((uid, component), index, args.User);
                }
            };

            args.Verbs.Add(v);
        }
    }

    private void OnUseInHandEvent(Entity<ChangeableReagentComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        TryCycleReagent(ent, args.User);
    }

    public void TryCycleReagent(Entity<ChangeableReagentComponent> ent, EntityUid? user = null)
    {
        if (ent.Comp.Reagents.Count < 2)
            return;

        var index = (ent.Comp.CurrentReagent + 1) % ent.Comp.Reagents.Count;
        TrySetReagent(ent, index, user);
    }

    public bool TrySetReagent(Entity<ChangeableReagentComponent> ent, int index, EntityUid? user = null)
    {
        if (index < 0 || index >= ent.Comp.Reagents.Count)
            return false;

        if (user != null && !_accessReaderSystem.IsAllowed(user.Value, ent))
            return false;

        SetReagent(ent, index, user);
		
        return true;
    }

    private void SetReagent(Entity<ChangeableReagentComponent> ent, int index, EntityUid? user = null)
    {
        var reagent = ent.Comp.Reagents[index];
        ent.Comp.CurrentReagent = index;
        Dirty(ent);

		if (_protoMan.TryIndex<ReagentPrototype>(reagent.Reagent[0].Reagent.Prototype, out var prototype))
		{
			if (user != null)
				_popupSystem.PopupEntity(Loc.GetString("set-reagent", ("reagent", Loc.GetString(prototype.LocalizedName))),ent, user.Value);
		}
		
        if (TryComp<SolutionRegenerationComponent>(ent, out var regenComp))
        {
            var newSolution = new Solution();
			foreach (var r in reagent.Reagent)
			{
				newSolution.AddReagent(r.Reagent.Prototype, r.Quantity);
			}

            _solutionrRagents.SetReagent((ent, regenComp), newSolution);
            Dirty(ent, regenComp);
        }
    }
}
