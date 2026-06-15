using Robust.Shared.Prototypes;
using Content.Shared.Verbs;
using Content.Shared.Access.Systems;
using Content.Shared.Popups;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Reagent.Ranged.Components;
using Robust.Shared.Prototypes;
using Content.Shared.Chemistry.Reagent;

namespace Content.Shared.Reagent.Ranged.Systems;

public sealed partial class ChangeableReagentSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private SharedPopupSystem _popupSystem = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;

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

        var account = GetReagents(component);

        if (!_prototypeManager.TryIndex<ReagentPrototype>(account.Reagent, out var proto))
            return;

        args.PushMarkup(Loc.GetString("reagents-set", ("reagent", Loc.GetString(proto.Name))));
    }

    private ChangeableReagentComponent GetReagents(ChangeableReagentComponent component)
    {
        return component.Reagents[component.CurrentAccount];
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
            var account = component.Reagents[i];
            var proto = _prototypeManager.Index<ReagentPrototype>(account.Reagent);
            var index = i;

            var v = new Verb
            {
                Priority = 1,
                Category = VerbCategory.ReagentChange,
                Text = Loc.GetString(proto.Code),
                Disabled = i == component.CurrentAccount,
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
        var account = component.Reagents[index];
        component.CurrentAccount = index;
        Dirty(uid, component);

        if (_prototypeManager.TryIndex<ReagentPrototype>(account.Reagent, out var prototype))
        {
            if (user != null)
                _popupSystem.PopupClient(Loc.GetString("reagents-set", ("reagent", Loc.GetString(prototype.Name))), uid, user.Value);
        }

        if (TryComp(uid, out SolutionRegenerationComponent? sprayComp))
        {
            sprayComp.Generated  = account.Reagent;

            Dirty(uid, sprayComp);
        }
    }
}
