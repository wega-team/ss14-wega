using Robust.Shared.Prototypes;
using Content.Shared.Verbs;
using Content.Shared.Access.Systems;
using Content.Shared.Popups;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.ChangeableReagent.Components;

namespace Content.Shared.ChangeableReagent;

public sealed partial class ChangeableReagentSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private SharedPopupSystem _popupSystem = default!;
    [Dependency] private SolutionRegenerationSystem _solutionrRagents = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangeableReagentComponent, GetVerbsEvent<Verb>>(OnGetVerb);
        SubscribeLocalEvent<ChangeableReagentComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(EntityUid uid, ChangeableReagentComponent component, ExaminedEvent args)
    {
        if (component.Reagents.Count < 2)
            return;

        var account = GetReagent(component);
		var name = account.Name;

        args.PushMarkup(Loc.GetString("set-reagent", ("reagent", Loc.GetString(name))));
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

        for (var i = 0; i < component.Reagents.Count; i++)
        {
            var Reagent = component.Reagents[i];
            var index = i;

            var v = new Verb
            {
                Priority = 1,
                Category = VerbCategory.ReagentChange,
                Text = Loc.GetString(Reagent.Name),
                Disabled = i == component.CurrentReagent,
                Impact = LogImpact.Low,
                DoContactInteraction = true,
                Act = () =>
                {
                    TrySetReagent(uid, component, index, args.User);
                }
            };

            args.Verbs.Add(v);
        }
    }

    public bool TrySetReagent(EntityUid uid, ChangeableReagentComponent component, int index, EntityUid? user = null)
    {
        if (index < 0 || index >= component.Reagents.Count)
            return false;

        SetReagent(uid, component, index, user);

        return true;
    }

    private void SetReagent(EntityUid uid, ChangeableReagentComponent component, int index, EntityUid? user = null)
    {
        var Reagent = component.Reagents[index];
        component.CurrentReagent = index;
        Dirty(uid, component);

        if (user != null)
			_popupSystem.PopupClient(Loc.GetString("set-reagent", ("reagent", Loc.GetString(Reagent.Name))), uid, user.Value);
		
		if (TryComp(uid, out SolutionRegenerationComponent? reagenComp))
        {
			var newSolution = new Solution(Reagent.Reagent);
            _solutionrRagents.SetReagent((uid,reagenComp), newSolution);
        }
    }
}
