using Content.Server.Body.Systems;
using Content.Server.Doors.Systems;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared._Wega.SPAI;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Doors.Components;
using Content.Shared.Electrocution;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Overlays;
using Content.Shared._Wega.NinjaVisor;
using Content.Shared.PAI;
using Content.Shared.Popups;
using Content.Server.SurveillanceCamera;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server._Wega.SPAI;

public sealed partial class SpaiSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private ActionContainerSystem _actionContainer = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private BloodstreamSystem _bloodstream = default!;
    [Dependency] private SharedElectrocutionSystem _electrify = default!;
    [Dependency] private AirlockSystem _airlocks = default!;
    [Dependency] private DoorSystem _doors = default!;
    [Dependency] private ApcSystem _apc = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private SurveillanceCameraMonitorSystem _cameraMonitor = default!;

    private static readonly EntProtoId ChemInjectorOpenAction = "ActionSPAIChemInjectorOpen";

    private static readonly (string Id, int Cost)[] ChemReagentDefs =
    [
        ("Kelotane",      5),
        ("Bicaridine",    5),
        ("Epinephrine",   5),
        ("Dexalin",       5),
        ("Tricordrazine", 5),
        ("Mannitol",      5),
        ("Omnizine",      15),
    ];

    private static readonly EntProtoId[] WeakAiActions =
    [
        "ActionSPAIWeakAIToggleDoor",
        "ActionSPAIWeakAIBolt",
        "ActionSPAIWeakAIElectrify",
        "ActionSPAIWeakAIEmergencyAccess",
        "ActionSPAIWeakAIApc",
    ];

    private static readonly TimeSpan WeakAiGroupCooldown = TimeSpan.FromSeconds(10);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PAIComponent, SpaiToggleThermalVisionEvent>(OnToggleThermalVision);
        SubscribeLocalEvent<PAIComponent, SpaiToggleSecurityHudEvent>(OnToggleSecurityHud);
        SubscribeLocalEvent<PAIComponent, SpaiInstallChemInjectorEvent>(OnInstallChemInjector);
        SubscribeLocalEvent<PAIComponent, SpaiInstallWeakAiEvent>(OnInstallWeakAi);

        SubscribeLocalEvent<SurveillanceCameraMonitorComponent, BoundUIOpenedEvent>(OnCameraMonitorOpened);
        SubscribeLocalEvent<SpaiChemInjectorComponent, BoundUIOpenedEvent>(OnChemInjectorUiOpened);
        SubscribeLocalEvent<SpaiChemInjectorComponent, SpaiChemInjectorInjectMessage>(OnChemInjectorMessage);

        SubscribeLocalEvent<SpaiWeakAiCapabilityComponent, SpaiToggleDoorEvent>(OnWeakAiToggleDoor);
        SubscribeLocalEvent<SpaiWeakAiCapabilityComponent, SpaiAirlockElectrifyEvent>(OnWeakAiElectrify);
        SubscribeLocalEvent<SpaiWeakAiCapabilityComponent, SpaiAirlockBoltEvent>(OnWeakAiBolt);
SubscribeLocalEvent<SpaiWeakAiCapabilityComponent, SpaiAirlockEmergencyAccessEvent>(OnWeakAiEmergencyAccess);
        SubscribeLocalEvent<SpaiWeakAiCapabilityComponent, SpaiApcDisableEvent>(OnWeakAiApc);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SpaiChemInjectorComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Energy >= comp.EnergyMax)
                continue;

            comp.RefillAccumulator += frameTime;
            if (comp.RefillAccumulator < comp.RefillIntervalSeconds)
                continue;

            comp.RefillAccumulator -= comp.RefillIntervalSeconds;
            comp.Energy = Math.Min(comp.Energy + comp.EnergyPerRefill, comp.EnergyMax);
            Dirty(uid, comp);

            if (_ui.IsUiOpen(uid, SpaiChemInjectorUiKey.Key))
                SendChemInjectorState((uid, comp));
        }
    }

    private void OnToggleThermalVision(Entity<PAIComponent> ent, ref SpaiToggleThermalVisionEvent args)
    {
        if (TryComp<NinjaVisorComponent>(ent.Owner, out var visor) && visor.Mode == NinjaVisorMode.Thermal)
        {
            RemComp<NinjaVisorComponent>(ent.Owner);
            _popup.PopupEntity(Loc.GetString("spai-thermal-vision-off"), ent.Owner, ent.Owner);
        }
        else
        {
            var comp = EnsureComp<NinjaVisorComponent>(ent.Owner);
            comp.Mode = NinjaVisorMode.Thermal;
            Dirty(ent.Owner, comp);
            _popup.PopupEntity(Loc.GetString("spai-thermal-vision-on"), ent.Owner, ent.Owner);
        }
        args.Handled = true;
    }

    private void OnToggleSecurityHud(Entity<PAIComponent> ent, ref SpaiToggleSecurityHudEvent args)
    {
        if (HasComp<ShowCriminalRecordIconsComponent>(ent.Owner))
        {
            RemComp<ShowCriminalRecordIconsComponent>(ent.Owner);
            RemComp<ShowJobIconsComponent>(ent.Owner);
            RemComp<ShowMindShieldIconsComponent>(ent.Owner);
            RemComp<ShowHealthIconsComponent>(ent.Owner);
            _popup.PopupEntity(Loc.GetString("spai-sec-hud-off"), ent.Owner, ent.Owner);
        }
        else
        {
            EnsureComp<ShowCriminalRecordIconsComponent>(ent.Owner);
            EnsureComp<ShowJobIconsComponent>(ent.Owner);
            EnsureComp<ShowMindShieldIconsComponent>(ent.Owner);
            EnsureComp<ShowHealthIconsComponent>(ent.Owner);
            _popup.PopupEntity(Loc.GetString("spai-sec-hud-on"), ent.Owner, ent.Owner);
        }
        args.Handled = true;
    }

    private void OnInstallChemInjector(Entity<PAIComponent> ent, ref SpaiInstallChemInjectorEvent args)
    {
        EnsureComp<SpaiChemInjectorComponent>(ent.Owner);
        // Store in mind's container so the action survives polymorph revert (PVS-safe)
        var container = _mind.TryGetMind(ent.Owner, out var mindId, out _) ? mindId : ent.Owner;
        _actions.AddAction(ent.Owner, ChemInjectorOpenAction, container);
        // Remove install action from mind's container and delete it
        _actionContainer.RemoveAction(new Entity<ActionComponent?>(args.Action.Owner, args.Action.Comp));
        QueueDel(args.Action.Owner);
        _popup.PopupEntity(Loc.GetString("spai-chem-injector-installed"), ent.Owner, ent.Owner);
        args.Handled = true;
    }

    private void OnCameraMonitorOpened(Entity<SurveillanceCameraMonitorComponent> ent, ref BoundUIOpenedEvent args)
    {
        _cameraMonitor.AfterOpenUserInterface(ent.Owner, args.Actor);
    }

    private void OnChemInjectorUiOpened(Entity<SpaiChemInjectorComponent> ent, ref BoundUIOpenedEvent args)
    {
        SendChemInjectorState(ent);
    }

    private void SendChemInjectorState(Entity<SpaiChemInjectorComponent> ent)
    {
        var reagents = new List<SpaiReagentEntry>(ChemReagentDefs.Length);
        foreach (var (id, cost) in ChemReagentDefs)
            reagents.Add(new SpaiReagentEntry(id, Loc.GetString($"reagent-name-{id.ToLower()}"), cost));

        var state = new SpaiChemInjectorBoundUserInterfaceState(ent.Comp.Energy, ent.Comp.EnergyMax, reagents);
        _ui.SetUiState(ent.Owner, SpaiChemInjectorUiKey.Key, state);
    }

    private void OnChemInjectorMessage(Entity<SpaiChemInjectorComponent> ent, ref SpaiChemInjectorInjectMessage args)
    {
        var carrier = FindCarrier(ent.Owner);
        if (carrier == null)
        {
            _popup.PopupEntity(Loc.GetString("spai-inject-no-carrier"), ent.Owner, ent.Owner);
            return;
        }

        if (ent.Comp.Energy < args.EnergyCost)
        {
            _popup.PopupEntity(Loc.GetString("spai-inject-no-energy"), ent.Owner, ent.Owner);
            SendChemInjectorState(ent);
            return;
        }

        if (!TryComp<BloodstreamComponent>(carrier.Value, out var bloodstream))
        {
            _popup.PopupEntity(Loc.GetString("spai-inject-no-bloodstream"), ent.Owner, ent.Owner);
            return;
        }

        var solution = new Solution(args.ReagentId, args.EnergyCost);
        if (_bloodstream.TryAddToBloodstream((carrier.Value, bloodstream), solution))
        {
            ent.Comp.Energy -= args.EnergyCost;
            Dirty(ent.Owner, ent.Comp);
            _popup.PopupEntity(Loc.GetString("spai-inject-success"), ent.Owner, ent.Owner);
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("spai-inject-fail"), ent.Owner, ent.Owner);
        }

        SendChemInjectorState(ent);
    }

    private void OnInstallWeakAi(Entity<PAIComponent> ent, ref SpaiInstallWeakAiEvent args)
    {
        EnsureComp<SpaiWeakAiCapabilityComponent>(ent.Owner);
        // Store in mind's container so actions survive polymorph revert (PVS-safe)
        var container = _mind.TryGetMind(ent.Owner, out var mindId, out _) ? mindId : ent.Owner;
        foreach (var protoId in WeakAiActions)
            _actions.AddAction(ent.Owner, protoId, container);
        // Remove install action from mind's container and delete it
        _actionContainer.RemoveAction(new Entity<ActionComponent?>(args.Action.Owner, args.Action.Comp));
        QueueDel(args.Action.Owner);
        _popup.PopupEntity(Loc.GetString("spai-weak-ai-installed"), ent.Owner, ent.Owner);
        args.Handled = true;
    }

    private void OnWeakAiToggleDoor(Entity<SpaiWeakAiCapabilityComponent> ent, ref SpaiToggleDoorEvent args)
    {
        if (!TryComp<DoorComponent>(args.Target, out var door))
            return;

        _doors.TryToggleDoor(args.Target, door, user: null);
        SetWeakAiGroupCooldown(ent.Owner);
        args.Handled = true;
    }

    private void OnWeakAiBolt(Entity<SpaiWeakAiCapabilityComponent> ent, ref SpaiAirlockBoltEvent args)
    {
        if (!TryComp<DoorBoltComponent>(args.Target, out var bolts))
            return;

        _doors.TrySetBoltDown((args.Target, bolts), !bolts.BoltsDown, args.Performer, predicted: false);
        SetWeakAiGroupCooldown(ent.Owner);
        args.Handled = true;
    }

    private void OnWeakAiElectrify(Entity<SpaiWeakAiCapabilityComponent> ent, ref SpaiAirlockElectrifyEvent args)
    {
        if (!TryComp<ElectrifiedComponent>(args.Target, out var electrified))
            return;

        _electrify.SetElectrified((args.Target, electrified), !electrified.Enabled);
        SetWeakAiGroupCooldown(ent.Owner);
        args.Handled = true;
    }

    private void OnWeakAiEmergencyAccess(Entity<SpaiWeakAiCapabilityComponent> ent, ref SpaiAirlockEmergencyAccessEvent args)
    {
        if (!TryComp<AirlockComponent>(args.Target, out var airlock))
            return;

        _airlocks.SetEmergencyAccess((args.Target, airlock), !airlock.EmergencyAccess, args.Performer, predicted: false);
        SetWeakAiGroupCooldown(ent.Owner);
        args.Handled = true;
    }

    private void OnWeakAiApc(Entity<SpaiWeakAiCapabilityComponent> ent, ref SpaiApcDisableEvent args)
    {
        if (!TryComp<ApcComponent>(args.Target, out _))
            return;

        _apc.ApcToggleBreaker(args.Target);
        SetWeakAiGroupCooldown(ent.Owner);
        args.Handled = true;
    }

    private void SetWeakAiGroupCooldown(EntityUid paiUid)
    {
        foreach (var action in _actions.GetActions(paiUid))
        {
            if (MetaData(action.Owner).EntityPrototype?.ID is not {} id)
                continue;

            foreach (var protoId in WeakAiActions)
            {
                if (protoId.Id == id)
                {
                    _actions.SetCooldown((action.Owner, (ActionComponent?) null), WeakAiGroupCooldown);
                    break;
                }
            }
        }
    }

    private EntityUid? FindCarrier(EntityUid pai)
    {
        var current = Transform(pai).ParentUid;
        while (current.IsValid())
        {
            if (HasComp<MobStateComponent>(current))
                return current;
            current = Transform(current).ParentUid;
        }
        return null;
    }
}
