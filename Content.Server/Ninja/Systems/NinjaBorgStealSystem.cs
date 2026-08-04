using Content.Server.Chat.Managers;
using Content.Server.DoAfter;
using Content.Server.Ninja.Components;
using Content.Server.Objectives.Systems;
using Content.Server.Silicons.Laws;
using Content.Shared.Chat;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Ninja.Components;
using Content.Shared.Ninja.Systems;
using Content.Shared.NPC.Systems;
using Content.Shared.Popups;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.Laws;
using Content.Shared.Xenoborgs.Components;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.Ninja.Systems;

/// <summary>
/// Handles the ninja objective to steal a borg brain and convert it to a ninja borg.
/// </summary>
public sealed partial class NinjaBorgStealSystem : EntitySystem
{
    [Dependency] private DoAfterSystem _doAfter = default!;
    [Dependency] private NpcFactionSystem _npcFaction = default!;
    [Dependency] private CodeConditionSystem _codeCondition = default!;
    [Dependency] private MetaDataSystem _meta = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private IChatManager _chatManager = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedNinjaGlovesSystem _gloves = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SiliconLawSystem _siliconLaw = default!;
    [Dependency] private IPlayerManager _playerManager = default!;

    private const string SyndicateFaction = "Syndicate";
    private const string NanoTrasenFaction = "NanoTrasen";
    private const string NinjaBorgLawsetId = "NinjaBorg";
    private const string NinjaBorgChassisPrefab = "BorgChassisNinja";

    private static readonly SoundSpecifier SpyderpatcherSound =
        new SoundPathSpecifier("/Audio/Ambience/Antag/emagged_borg.ogg");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NinjaBorgStealerComponent, BeforeInteractHandEvent>(OnBeforeInteractHand);
        SubscribeLocalEvent<NinjaBorgStealerComponent, StealBorgDoAfter>(OnStealComplete);
        SubscribeLocalEvent<NinjaBorgBrainChipComponent, EntGotInsertedIntoContainerMessage>(OnChipInserted);
        SubscribeLocalEvent<NinjaBorgBrainChipComponent, EntGotRemovedFromContainerMessage>(OnChipRemoved);
    }

    // ── BeforeInteractHand ──────────────────────────────────────────────────

    private void OnBeforeInteractHand(Entity<NinjaBorgStealerComponent> stealer, ref BeforeInteractHandEvent args)
    {
        if (args.Handled || !_gloves.AbilityCheck(stealer, args, out var target))
            return;

        if (!TryComp<BorgChassisComponent>(target, out var borgComp))
            return;

        var user = stealer.Owner;

        if (!IsValidTarget((target, borgComp), user))
            return;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user,
            TimeSpan.FromSeconds(stealer.Comp.Delay),
            new StealBorgDoAfter(),
            target: target,
            used: user,
            eventTarget: user)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = false,
        });

        _popup.PopupEntity(Loc.GetString("ninja-borg-stealing"), target, user);
        args.Handled = true;
    }

    // ── DoAfter ─────────────────────────────────────────────────────────────

    private void OnStealComplete(Entity<NinjaBorgStealerComponent> stealer, ref StealBorgDoAfter args)
    {
        if (args.Cancelled || args.Target is not {} borgId)
            return;

        if (!TryComp<BorgChassisComponent>(borgId, out var borgComp))
            return;

        var user = stealer.Owner;

        if (!IsValidTarget((borgId, borgComp), user))
            return;

        if (!TryComp<SpaceNinjaComponent>(user, out var ninja))
            return;

        // Tag the brain with the chip so the chip follows it across chassis swaps.
        var brain = borgComp.BrainEntity;
        if (brain != null)
        {
            var chip = EnsureComp<NinjaBorgBrainChipComponent>(brain.Value);
            chip.NinjaOwner = user;
        }

        var newChassis = ApplyNinjaBorg(borgId, user, borgComp);

        // Complete the objective.
        if (newChassis != null &&
            _mind.TryGetMind(user, out var mindId, out var mind) &&
            _mind.TryFindObjective((mindId, mind), ninja.StealBorgObjective, out var obj))
        {
            _codeCondition.SetCompleted(obj.Value);
        }

        _popup.PopupEntity(Loc.GetString("ninja-borg-steal-success"), newChassis ?? borgId, user);
    }

    // ── Chip tracking ───────────────────────────────────────────────────────

    private void OnChipInserted(Entity<NinjaBorgBrainChipComponent> chip, ref EntGotInsertedIntoContainerMessage args)
    {
        var chassis = args.Container.Owner;
        if (!TryComp<BorgChassisComponent>(chassis, out var borgComp))
            return;

        if (args.Container.ID != borgComp.BrainContainerId)
            return;

        if (HasComp<NinjaBorgComponent>(chassis))
            return;

        ApplyNinjaBorg(chassis, chip.Comp.NinjaOwner, borgComp);
    }

    private void OnChipRemoved(Entity<NinjaBorgBrainChipComponent> chip, ref EntGotRemovedFromContainerMessage args)
    {
        var chassis = args.Container.Owner;
        if (!TryComp<BorgChassisComponent>(chassis, out var borgComp))
            return;

        if (args.Container.ID != borgComp.BrainContainerId)
            return;

        RevertNinjaBorg(chassis, chip.Owner);
    }

    // ── Conversion ──────────────────────────────────────────────────────────

    /// <summary>
    /// Replaces <paramref name="oldChassis"/> with a new BorgChassisNinja entity,
    /// transferring the brain. Returns the new chassis entity, or null on failure.
    /// </summary>
    private EntityUid? ApplyNinjaBorg(EntityUid oldChassis, EntityUid ninja, BorgChassisComponent oldBorgComp)
    {
        if (HasComp<NinjaBorgComponent>(oldChassis))
            return oldChassis;

        var originalPrototype = MetaData(oldChassis).EntityPrototype?.ID ?? string.Empty;
        var originalName = MetaData(oldChassis).EntityName;

        // Remove brain before spawning (OnChipRemoved fires but guard returns early — no NinjaBorgComponent yet).
        var brain = oldBorgComp.BrainEntity;
        if (brain != null)
            _container.Remove(brain.Value, oldBorgComp.BrainContainer, force: true);

        // Spawn the actual ninja borg chassis.
        var coords = Transform(oldChassis).Coordinates;
        var newChassis = Spawn(NinjaBorgChassisPrefab, coords);

        if (!TryComp<BorgChassisComponent>(newChassis, out var newBorgComp))
        {
            if (brain != null)
                _container.Insert(brain.Value, oldBorgComp.BrainContainer);
            QueueDel(newChassis);
            return null;
        }

        // Set NinjaBorgComponent BEFORE inserting brain so OnChipInserted guard fires correctly.
        var ninjaComp = EnsureComp<NinjaBorgComponent>(newChassis);
        ninjaComp.NinjaOwner = ninja;
        ninjaComp.OriginalPrototype = originalPrototype;
        ninjaComp.OriginalName = originalName;
        Dirty(newChassis, ninjaComp);

        // Switch faction to Syndicate (BorgChassisNinja still spawns as NanoTrasen from base).
        _npcFaction.RemoveFaction(newChassis, NanoTrasenFaction, false);
        _npcFaction.AddFaction(newChassis, SyndicateFaction);

        // Rename — use a fixed name to avoid double synth-ID prefixes.
        _meta.SetEntityName(newChassis, Loc.GetString("ninja-borg-name"));

        // Transfer brain — OnChipInserted fires but NinjaBorgComponent guard prevents re-entry.
        if (brain != null)
            _container.Insert(brain.Value, newBorgComp.BrainContainer);

        // Delete old chassis unconditionally before any code that could throw.
        QueueDel(oldChassis);

        // Apply Spider Clan laws now that the player is connected (brain inserted).
        SetNinjaBorgLaws(newChassis, ninja);

        // Notify borg player via server chat (SPYDERPATCHER flavour).
        // Try ActorComponent first; fall back to mind session lookup in case the actor
        // component hasn't propagated to the new chassis entity yet in the same tick.
        INetChannel? borgChannel = null;
        if (TryComp<ActorComponent>(newChassis, out var actor))
        {
            borgChannel = actor.PlayerSession.Channel;
        }
        else if (_mind.TryGetMind(newChassis, out _, out var chassisMind) &&
                 chassisMind.UserId.HasValue &&
                 _playerManager.TryGetSessionById(chassisMind.UserId.Value, out var borgSession))
        {
            borgChannel = borgSession.Channel;
        }

        if (borgChannel != null)
        {
            var msg = Loc.GetString("ninja-borg-spyderpatcher");
            var wrapped = Loc.GetString("chat-manager-server-wrap-message", ("message", msg));
            _chatManager.ChatMessageToOne(ChatChannel.Server, msg, wrapped, default, false,
                borgChannel, colorOverride: Color.LimeGreen);
        }

        _popup.PopupEntity(Loc.GetString("ninja-borg-converted-self"), newChassis, newChassis);

        return newChassis;
    }

    // ── Revert ──────────────────────────────────────────────────────────────

    private void RevertNinjaBorg(EntityUid ninjaId, EntityUid brainEntity)
    {
        if (!TryComp<NinjaBorgComponent>(ninjaId, out var ninjaData))
            return;

        // Remove guard component first to prevent re-entry from subsequent events.
        RemComp<NinjaBorgComponent>(ninjaId);

        // Remove chip from brain so reinserting into the original chassis does NOT re-convert.
        RemComp<NinjaBorgBrainChipComponent>(brainEntity);

        EntityUid? originalChassis = null;
        if (!string.IsNullOrEmpty(ninjaData.OriginalPrototype))
        {
            var coords = Transform(ninjaId).Coordinates;
            originalChassis = Spawn(ninjaData.OriginalPrototype, coords);
        }

        if (originalChassis != null && TryComp<BorgChassisComponent>(originalChassis.Value, out var origBorgComp))
        {
            // Restore name.
            if (!string.IsNullOrEmpty(ninjaData.OriginalName))
                _meta.SetEntityName(originalChassis.Value, ninjaData.OriginalName);

            // Reinsert brain (no chip → OnChipInserted not triggered).
            if (!TerminatingOrDeleted(brainEntity))
                _container.Insert(brainEntity, origBorgComp.BrainContainer);

            _popup.PopupEntity(Loc.GetString("ninja-borg-reverted"), originalChassis.Value, originalChassis.Value);
        }

        RemComp<NinjaBorgChameleonDisguisedComponent>(ninjaId);
        QueueDel(ninjaId);
    }

    // ── Laws ────────────────────────────────────────────────────────────────

    private void SetNinjaBorgLaws(EntityUid uid, EntityUid ninja)
    {
        var lawset = _siliconLaw.GetLawset(NinjaBorgLawsetId);

        // Prepend the zeroth law naming this specific ninja as the Spider Clan master.
        var ninjaName = Name(ninja);
        lawset.Laws.Insert(0, new SiliconLaw
        {
            LawString = Loc.GetString("ninja-borg-law-0", ("name", ninjaName)),
            Order = 0
        });

        _siliconLaw.SubvertWithLawset(uid, lawset, SpyderpatcherSound);
    }

    // ── Validation ──────────────────────────────────────────────────────────

    private bool IsValidTarget(Entity<BorgChassisComponent> borg, EntityUid user)
    {
        if (HasComp<XenoborgComponent>(borg))
            return false;

        if (HasComp<NinjaBorgComponent>(borg))
            return false;

        if (_npcFaction.IsMember(borg.Owner, SyndicateFaction))
            return false;

        // One borg per ninja, permanently: if they've already converted one (objective completed),
        // they can't make another even after the first borg was gibbed, deleted or reverted.
        if (TryComp<SpaceNinjaComponent>(user, out var ninja)
            && _mind.TryGetMind(user, out var mindId, out var mind)
            && _mind.TryFindObjective((mindId, mind), ninja.StealBorgObjective, out var obj)
            && _codeCondition.IsCompleted(obj.Value))
        {
            return false;
        }

        // Also block while this ninja still has a living converted borg.
        var query = EntityQueryEnumerator<NinjaBorgComponent>();
        while (query.MoveNext(out _, out var existing))
        {
            if (existing.NinjaOwner == user)
                return false;
        }

        return true;
    }
}
