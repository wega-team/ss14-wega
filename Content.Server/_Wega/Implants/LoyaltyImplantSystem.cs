using Content.Server.Antag;
using Content.Server.Objectives;
using Content.Server.Popups;
using Content.Server._Wega.Implants.Components;
using Content.Shared._Wega.Implants.Components;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Mind;
using Content.Shared.Mindshield.Components;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Content.Shared.Bed.Sleep;
using Content.Shared.Roles;
using Content.Shared.NPC.Systems;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.IdentityManagement;
using Content.Shared.IdentityManagement.Components;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._Wega.Implants;

public sealed partial class MindControlSystem : EntitySystem
{
    private const string FollowOrdersObjectiveId = "MindControlledFollowOrders";
    private const string SyndicateFaction = "Syndicate";

    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private ObjectivesSystem _objectives = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private ServerAntagSelectionSystem _antag = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private SharedRoleSystem _roles = default!;
    [Dependency] private NpcFactionSystem _npcFaction = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ImplanterComponent, AddImplantAttemptEvent>(OnAttemptImplant);
        SubscribeLocalEvent<MindControlImplantComponent, ImplantImplantedEvent>(OnImplanted);
        SubscribeLocalEvent<MindControlImplantComponent, ImplantRemovedEvent>(OnRemoved);
        SubscribeLocalEvent<MindControlComponent, ExaminedEvent>(OnMindControlExamined);
    }

    private void OnAttemptImplant(Entity<ImplanterComponent> ent, ref AddImplantAttemptEvent args)
    {
        if (!TryComp<MindControlImplantComponent>(args.Implant, out var implantComp))
            return;

        if (args.User == args.Target)
        {
            args.Cancel();
            return;
        }

        if (!_mind.TryGetMind(args.Target, out _, out var targetMind) ||
            _mind.IsCharacterDeadIc(targetMind))
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

        implantComp.Master = args.User;
    }

    private void OnImplanted(Entity<MindControlImplantComponent> ent, ref ImplantImplantedEvent args)
    {
        if (!TryComp<ActorComponent>(args.Implanted, out var actor))
            return;

        if (HasComp<MindControlComponent>(args.Implanted))
            return;

        if (!_mind.TryGetMind(ent.Comp.Master, out var masterMindId, out var masterMind) ||
            masterMind.CharacterName == null)
            return;

        var control = EnsureComp<MindControlComponent>(args.Implanted);
        control.Master = ent.Comp.Master;

        control.HadSyndicateFaction = _npcFaction.IsMember(args.Implanted, SyndicateFaction);
        _npcFaction.AddFaction(args.Implanted, SyndicateFaction);

        if (_mind.TryGetMind(args.Implanted, out var mindId, out var mind))
        {
            var info = EnsureComp<MindControlRoundInfoComponent>(mindId);
            info.ControlledName = Name(args.Implanted);
            info.MasterMind = masterMindId;
            info.MasterName = masterMind.CharacterName;

            _roles.MindAddRole(mindId, "MindRoleBrainwashed", mind, silent: true);
        }

        _antag.SendBriefing(
            actor.PlayerSession,
            Loc.GetString(ent.Comp.BriefingText, ("master-name", masterMind.CharacterName)),
            null,
            ent.Comp.BriefingSound);

        _status.TryAddStatusEffectDuration(
            args.Implanted,
            SleepingSystem.StatusEffectForcedSleeping,
            TimeSpan.FromSeconds(5));

        AssignObjective(args.Implanted, masterMind.CharacterName);
        control.SideEffectsAt = _timing.CurTime + TimeSpan.FromMinutes(25);
    }

    private void OnRemoved(Entity<MindControlImplantComponent> ent, ref ImplantRemovedEvent args)
    {
        if (TerminatingOrDeleted(args.Implanted))
            return;

        if (TryComp<ActorComponent>(args.Implanted, out var actor))
            _antag.SendBriefing(actor.PlayerSession, Loc.GetString(ent.Comp.DebriefingText), null, ent.Comp.DebriefingSound);

        if (_mind.TryGetMind(args.Implanted, out var mindId, out var mind))
            _roles.MindRemoveRole((mindId, mind), "MindRoleBrainwashed");

        if (TryComp<MindControlComponent>(args.Implanted, out var control))
        {
            if (!control.HadSyndicateFaction)
                _npcFaction.RemoveFaction(args.Implanted, SyndicateFaction);
        }

        RemoveObjective(args.Implanted);
        RemCompDeferred<MindControlComponent>(args.Implanted);
        _status.TryAddStatusEffectDuration(args.Implanted, SleepingSystem.StatusEffectForcedSleeping, TimeSpan.FromSeconds(5));
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
    }

    private void AssignObjective(EntityUid uid, string masterName)
    {
        if (!_mind.TryGetMind(uid, out var mindId, out var mind))
            return;

        var objective = _objectives.TryCreateObjective(mindId, mind, FollowOrdersObjectiveId);
        if (objective == null)
            return;

        _metaData.SetEntityDescription(
            objective.Value,
            Loc.GetString("ent-MindControlledFollowOrders.desc", ("master-name", masterName)));

        _mind.AddObjective(mindId, mind, objective.Value);
    }

    private void OnMindControlExamined(EntityUid uid, MindControlComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange ||
            component.SideEffectsAt == null ||
            _timing.CurTime < component.SideEffectsAt.Value)
            return;

        var clothes = _inventory.GetSlotEnumerator((uid, null), SlotFlags.WITHOUT_POCKET);
        while (clothes.NextItem(out var cloth, out var slot))
        {
            if (TryComp<IdentityBlockerComponent>(cloth, out var blocker) &&
                blocker.Coverage.HasFlag(IdentityBlockerCoverage.EYES) &&
                blocker.Enabled)
                return;
        }

        var name = Identity.Name(uid, EntityManager, args.Examiner);
        args.PushMarkup(Loc.GetString("mind-control-side-effects-examined", ("name", name)));
    }
}
