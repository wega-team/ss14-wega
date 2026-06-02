using Content.Server.Chat.Systems;
using Content.Server.Objectives.Systems;
using Content.Shared._Wega.Ninja;
using Content.Shared.ActionBlocker;
using Content.Shared.Bed.Sleep;
using Content.Shared.Body;
using Content.Shared.Chat;
using Content.Shared.Climbing.Systems;
using Content.Shared.Damage.Systems;
using Content.Shared.DragDrop;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Events;
using Content.Shared.Ninja.Components;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Content.Shared.Verbs;
using Content.Shared.Station.Components;
using Content.Shared.Warps;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Ninja.Systems;

public sealed partial class MindScanMachineSystem : EntitySystem
{
    [Dependency] private ActionBlockerSystem _blocker = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private ClimbSystem _climbSystem = default!;
    [Dependency] private ContainerSystem _container = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private KidnapConditionSystem _kidnap = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;

    private static readonly (TimeSpan Delay, string LocKey)[] ScanStages =
    {
        (TimeSpan.FromSeconds(3),  "mind-scan-machine-stage-1"),
        (TimeSpan.FromSeconds(3),  "mind-scan-machine-stage-2"),
        (TimeSpan.FromSeconds(5),  "mind-scan-machine-stage-3"),
        (TimeSpan.FromSeconds(3),  "mind-scan-machine-stage-complete"),
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindScanMachineComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<MindScanMachineComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<MindScanMachineComponent, DragDropTargetEvent>(OnDragDrop);
        SubscribeLocalEvent<MindScanMachineComponent, ContainerRelayMovementEntityEvent>(OnRelayMovement);
        SubscribeLocalEvent<MindScanMachineComponent, GetVerbsEvent<InteractionVerb>>(AddInsertVerb);
        SubscribeLocalEvent<MindScanMachineComponent, GetVerbsEvent<AlternativeVerb>>(AddAlternativeVerbs);

        Subs.BuiEvents<MindScanMachineComponent>(MindScanMachineUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnUiOpened);
            subs.Event<MindScanScanMessage>(OnScanMessage);
            subs.Event<MindScanEjectMessage>(OnEjectMessage);
            subs.Event<MindScanTeleportMessage>(OnTeleportMessage);
        });
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<MindScanMachineComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.Scanning || comp.NextStageTime == null)
                continue;
            if (_timing.CurTime < comp.NextStageTime.Value)
                continue;

            AdvanceScanStage(uid, comp);
        }
    }

    private void OnInit(EntityUid uid, MindScanMachineComponent comp, ComponentInit args)
    {
        comp.BodyContainer = _container.EnsureContainer<ContainerSlot>(uid, "mind-scan-body");
    }

    private void OnShutdown(EntityUid uid, MindScanMachineComponent comp, ComponentShutdown args)
    {
        EjectOccupant(uid, comp);
    }

    private void OnDragDrop(EntityUid uid, MindScanMachineComponent comp, ref DragDropTargetEvent args)
    {
        InsertOccupant(uid, comp, args.Dragged, args.User);
        args.Handled = true;
    }

    private void OnRelayMovement(EntityUid uid, MindScanMachineComponent comp, ref ContainerRelayMovementEntityEvent args)
    {
        // Only eject if the occupant is capable of interacting (i.e. awake).
        // Sleeping entities cannot interact, so they stay inside.
        if (!_blocker.CanInteract(args.Entity, uid))
            return;

        EjectOccupant(uid, comp);
    }

    private void AddInsertVerb(EntityUid uid, MindScanMachineComponent comp, GetVerbsEvent<InteractionVerb> args)
    {
        if (args.Using == null || !args.CanAccess || !args.CanInteract)
            return;
        if (comp.BodyContainer.ContainedEntity != null || !HasComp<BodyComponent>(args.Using.Value))
            return;

        var name = MetaData(args.Using.Value).EntityName;
        args.Verbs.Add(new InteractionVerb
        {
            Act = () => InsertOccupant(uid, comp, args.Using.Value, args.User),
            Category = VerbCategory.Insert,
            Text = name,
        });
    }

    private void AddAlternativeVerbs(EntityUid uid, MindScanMachineComponent comp, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (comp.BodyContainer.ContainedEntity != null)
        {
            args.Verbs.Add(new AlternativeVerb
            {
                Act = () => EjectOccupant(uid, comp),
                Category = VerbCategory.Eject,
                Text = Loc.GetString("mind-scan-machine-verb-eject"),
                Priority = 1,
            });
        }
    }

    private void OnUiOpened(EntityUid uid, MindScanMachineComponent comp, BoundUIOpenedEvent args)
    {
        UpdateUiStateAll(uid, comp);
    }

    private void OnScanMessage(EntityUid uid, MindScanMachineComponent comp, MindScanScanMessage args)
    {
        StartScan(uid, comp, args.Actor);
    }

    private void OnEjectMessage(EntityUid uid, MindScanMachineComponent comp, MindScanEjectMessage args)
    {
        EjectOccupant(uid, comp);
    }

    private void OnTeleportMessage(EntityUid uid, MindScanMachineComponent comp, MindScanTeleportMessage args)
    {
        TeleportOccupant(uid, comp);
    }

    private void InsertOccupant(EntityUid uid, MindScanMachineComponent comp, EntityUid target, EntityUid? inserter = null)
    {
        if (comp.BodyContainer.ContainedEntity != null || !HasComp<BodyComponent>(target))
            return;

        if (HasComp<SpaceNinjaComponent>(target))
        {
            if (inserter != null)
                _popup.PopupEntity(Loc.GetString("mind-scan-machine-ninja-self-insert"), inserter.Value, inserter.Value);
            return;
        }

        if (!_container.Insert(target, comp.BodyContainer))
            return;

        _statusEffects.TryAddStatusEffectDuration(target, SleepingSystem.StatusEffectForcedSleeping, comp.SleepDuration);
        _popup.PopupEntity(Loc.GetString("mind-scan-machine-occupant-entered"), target, target);
        _appearance.SetData(uid, MindScanMachineVisuals.Status, MindScanMachineStatus.Occupied);
        SayMessage(uid, Loc.GetString("mind-scan-machine-inserted"));
        UpdateUiStateAll(uid, comp);
    }

    private void EjectOccupant(EntityUid uid, MindScanMachineComponent comp)
    {
        if (comp.BodyContainer.ContainedEntity is not { } contained)
            return;

        comp.Scanning = false;
        comp.ScanComplete = false;
        comp.ScanStage = 0;
        comp.NextStageTime = null;
        comp.ScanInitiator = null;

        _container.Remove(contained, comp.BodyContainer);
        _climbSystem.ForciblySetClimbing(contained, uid);
        _appearance.SetData(uid, MindScanMachineVisuals.Status, MindScanMachineStatus.Open);
        UpdateUiStateAll(uid, comp);
    }

    private void StartScan(EntityUid uid, MindScanMachineComponent comp, EntityUid initiator)
    {
        if (comp.BodyContainer.ContainedEntity == null || comp.Scanning)
            return;

        comp.Scanning = true;
        comp.ScanStage = 0;
        comp.NextStageTime = _timing.CurTime;
        comp.ScanInitiator = initiator;

        SayMessage(uid, Loc.GetString("mind-scan-machine-scan-start"));
        UpdateUiStateAll(uid, comp);
    }

    private void AdvanceScanStage(EntityUid uid, MindScanMachineComponent comp)
    {
        if (comp.ScanStage >= ScanStages.Length)
        {
            FinishScan(uid, comp);
            return;
        }

        var (delay, locKey) = ScanStages[comp.ScanStage];
        string message = comp.ScanStage switch
        {
            0 => Loc.GetString(locKey, ("occupantName", comp.BodyContainer.ContainedEntity is { } occ ? MetaData(occ).EntityName : "???")),
            1 => Loc.GetString(locKey, ("percent", _random.Next(1, 51))),
            2 => Loc.GetString(locKey, ("percent", _random.Next(51, 100))),
            _ => Loc.GetString(locKey),
        };
        SayMessage(uid, message);
        comp.ScanStage++;
        comp.NextStageTime = _timing.CurTime + delay;
    }

    private void FinishScan(EntityUid uid, MindScanMachineComponent comp)
    {
        comp.Scanning = false;
        comp.NextStageTime = null;

        var occupant = comp.BodyContainer.ContainedEntity;
        if (occupant == null)
            return;

        var ninja = comp.ScanInitiator;
        if (ninja == null || !HasComp<SpaceNinjaComponent>(ninja))
        {
            var query = EntityQueryEnumerator<SpaceNinjaComponent>();
            if (query.MoveNext(out var ninjaUid, out _))
                ninja = ninjaUid;
        }

        if (ninja == null)
        {
            SayMessage(uid, Loc.GetString("mind-scan-machine-no-ninja"));
            return;
        }

        _kidnap.TryRecordScan(ninja.Value, occupant.Value, out var message);

        if (message != null)
            SayMessage(uid, message);

        comp.ScanComplete = true;
        UpdateUiStateAll(uid, comp);
    }

    private void TeleportOccupant(EntityUid uid, MindScanMachineComponent comp)
    {
        var occupant = comp.BodyContainer.ContainedEntity;
        if (occupant == null)
            return;

        // Collect grids that belong to an actual station to exclude Lavaland dungeons etc.
        var stationGrids = new HashSet<EntityUid>();
        var stationQuery = EntityQueryEnumerator<StationDataComponent>();
        while (stationQuery.MoveNext(out _, out var station))
        {
            foreach (var grid in station.Grids)
                stationGrids.Add(grid);
        }

        var warps = new List<EntityUid>();
        var warpQuery = EntityQueryEnumerator<WarpPointComponent>();
        while (warpQuery.MoveNext(out var warpUid, out var warp))
        {
            if (warp.Location == null)
                continue;
            var warpGrid = Transform(warpUid).GridUid;
            if (warpGrid == null || !stationGrids.Contains(warpGrid.Value))
                continue;
            warps.Add(warpUid);
        }

        if (warps.Count == 0)
        {
            SayMessage(uid, Loc.GetString("mind-scan-machine-no-warp"));
            return;
        }

        var targetWarp = _random.Pick(warps);
        var warpCoords = Transform(targetWarp).Coordinates;

        _container.Remove(occupant.Value, comp.BodyContainer);
        _transform.SetCoordinates(occupant.Value, warpCoords);

        comp.ScanComplete = false;
        _appearance.SetData(uid, MindScanMachineVisuals.Status, MindScanMachineStatus.Open);
        SayMessage(uid, Loc.GetString("mind-scan-machine-teleported"));
        UpdateUiStateAll(uid, comp);
    }

    private void UpdateUiStateAll(EntityUid uid, MindScanMachineComponent comp)
    {
        _ui.SetUiState(uid, MindScanMachineUiKey.Key, BuildState(uid, comp));
    }

    private MindScanMachineBuiState BuildState(EntityUid uid, MindScanMachineComponent comp)
    {
        var occupant = comp.BodyContainer.ContainedEntity;
        NetEntity? occupantNet = null;
        float totalDamage = 0f;
        var isAlive = false;

        if (occupant != null)
        {
            occupantNet = GetNetEntity(occupant.Value);
            totalDamage = (float) _damageable.GetTotalDamage(occupant.Value);

            if (TryComp<MobStateComponent>(occupant, out var mobState))
                isAlive = _mobState.IsAlive(occupant.Value, mobState);
        }

        var scannedNames = new List<string>();
        var ninjaQuery = EntityQueryEnumerator<SpaceNinjaComponent>();
        if (ninjaQuery.MoveNext(out var ninjaUid, out _))
        {
            if (TryComp<MindContainerComponent>(ninjaUid, out var mindContainer)
                && mindContainer.Mind is { } mindId
                && TryComp<MindComponent>(mindId, out var mind))
            {
                foreach (var obj in mind.Objectives)
                {
                    if (!TryComp<Content.Server.Objectives.Components.KidnapConditionComponent>(obj, out var cond))
                        continue;

                    foreach (var scannedMindId in cond.ScannedMinds)
                    {
                        if (TryComp<MindComponent>(scannedMindId, out var scannedMind))
                            scannedNames.Add(scannedMind.CharacterName ?? Loc.GetString("mind-scan-machine-unknown"));
                    }
                    break;
                }
            }
        }

        return new MindScanMachineBuiState(occupantNet, totalDamage, isAlive, comp.Scanning, comp.ScanComplete, scannedNames);
    }

    private void SayMessage(EntityUid uid, string message)
    {
        _chat.TrySendInGameICMessage(uid, message, InGameICChatType.Speak, hideChat: false, ignoreActionBlocker: true);
    }
}
