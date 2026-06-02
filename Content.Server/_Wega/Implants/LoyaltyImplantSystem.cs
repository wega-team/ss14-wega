using Content.Server.Antag;
using Content.Server.Objectives;
using Content.Server.Popups;
using Content.Shared._Wega.Implants.Components;
using Content.Shared.Bed.Sleep;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Emp;
using Content.Shared.Implants;
using Content.Shared.Mind;
using Content.Shared.Mindshield.Components;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Player;

namespace Content.Server._Wega.Implants;

public sealed partial class MindControlSystem : EntitySystem
{
    private const string FollowOrdersObjectiveId = "MindControlledFollowOrders";

    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private ObjectivesSystem _objectives = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private AntagSelectionSystem _antag = default!;
    [Dependency] private SharedStaminaSystem _stamina = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MindControlImplantComponent, AddImplantAttemptEvent>(OnAttemptImplant);
        SubscribeLocalEvent<MindControlImplantComponent, ImplantImplantedEvent>(OnImplanted);
        SubscribeLocalEvent<MindControlImplantComponent, ImplantRemovedEvent>(OnRemoved);
        SubscribeLocalEvent<MindControlComponent, EmpPulseEvent>(OnEmpPulse);
    }

    private void OnAttemptImplant(Entity<MindControlImplantComponent> ent, ref AddImplantAttemptEvent args)
    {
        if (args.User == args.Target)
        {
            args.Cancel();
            return;
        }

        if (!_mind.TryGetMind(args.Target, out _, out var targetMind) || _mind.IsCharacterDeadIc(targetMind))
        {
            _popup.PopupEntity(Loc.GetString("mind-control-invalid"), args.User, args.User, PopupType.Small);
            args.Cancel();
            return;
        }

        if (HasComp<MindShieldComponent>(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("mind-control-prevented"), args.User, args.User, PopupType.Small);
            args.Cancel();
            return;
        }

        ent.Comp.Master = args.User;
    }

    private void OnImplanted(Entity<MindControlImplantComponent> ent, ref ImplantImplantedEvent args)
    {
        if (!TryComp<ActorComponent>(args.Implanted, out var actor))
            return;

        if (!_mind.TryGetMind(ent.Comp.Master, out _, out var masterMind) || masterMind.CharacterName == null)
            return;

        EnsureComp<MindControlComponent>(args.Implanted);
        _antag.SendBriefing(actor.PlayerSession,
            Loc.GetString(ent.Comp.BriefingText, ("master-name", masterMind.CharacterName)),
            null,
            ent.Comp.BriefingSound);

        _status.TryAddStatusEffectDuration(args.Implanted, SleepingSystem.StatusEffectForcedSleeping, TimeSpan.FromSeconds(2));
        AssignObjective(args.Implanted);
    }

    private void OnRemoved(Entity<MindControlImplantComponent> ent, ref ImplantRemovedEvent args)
    {
        if (TerminatingOrDeleted(args.Implanted))
            return;

        if (TryComp<ActorComponent>(args.Implanted, out var actor))
            _antag.SendBriefing(actor.PlayerSession, Loc.GetString(ent.Comp.DebriefingText), null, null);

        RemoveObjective(args.Implanted);
        RemCompDeferred<MindControlComponent>(args.Implanted);
        _status.TryAddStatusEffectDuration(args.Implanted, SleepingSystem.StatusEffectForcedSleeping, TimeSpan.FromSeconds(2));
    }

    private void OnEmpPulse(Entity<MindControlComponent> ent, ref EmpPulseEvent args)
    {
        if (!TryComp<StaminaComponent>(ent.Owner, out var stamina))
            return;

        _stamina.TakeStaminaDamage(ent.Owner, stamina.CritThreshold, stamina);
        args.Affected = true;
        args.Disabled = true;
    }

    private void RemoveObjective(EntityUid uid)
    {
        if (!_mind.TryGetMind(uid, out var mindId, out var mind))
            return;

        if (_mind.TryFindObjective((mindId, mind), FollowOrdersObjectiveId, out var objective) &&
            objective != null &&
            mind.Objectives.IndexOf(objective.Value) is var index &&
            index >= 0)
        {
            _mind.TryRemoveObjective(mindId, mind, index);
        }

        _popup.PopupEntity(Loc.GetString("mind-control-user-freed"), uid, uid, PopupType.Medium);
    }

    private void AssignObjective(EntityUid uid)
    {
        if (!_mind.TryGetMind(uid, out var mindId, out var mind))
            return;

        var objective = _objectives.TryCreateObjective(mindId, mind, FollowOrdersObjectiveId);
        if (objective == null)
            return;

        _mind.AddObjective(mindId, mind, objective.Value);
    }
}
