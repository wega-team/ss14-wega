using System.Linq;
using Content.Shared.Buckle.Components;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Surgery;
using Content.Shared.Surgery.Components;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Medical.Surgery
{
    public sealed class BodyScannerConsoleSystem : EntitySystem
    {
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly SharedDeviceLinkSystem _deviceLink = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<BodyScannerConsoleComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<BodyScannerConsoleComponent, AfterActivatableUIOpenEvent>(OnUIOpen);
            SubscribeLocalEvent<BodyScannerConsoleComponent, NewLinkEvent>(OnNewLink);
            SubscribeLocalEvent<BodyScannerConsoleComponent, PortDisconnectedEvent>(OnPortDisconnected);
            SubscribeLocalEvent<BodyScannerConsoleComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<BodyScannerConsoleComponent>();
            while (query.MoveNext(out var uid, out var component))
            {
                if (component.NextUpdate > _timing.CurTime)
                    continue;

                component.NextUpdate = _timing.CurTime + component.UpdateInterval;

                if (component.SurgeryTable != null)
                {
                    Transform(component.SurgeryTable.Value).Coordinates.TryDistance(
                        EntityManager,
                        Transform(uid).Coordinates,
                        out float scannerDistance);

                    component.SurgeryTableInRange = scannerDistance <= component.MaxDistance;
                }

                UpdateUserInterface(uid, component);
            }
        }

        private void OnInit(EntityUid uid, BodyScannerConsoleComponent component, ComponentInit args)
        {
            _deviceLink.EnsureSourcePorts(uid, BodyScannerConsoleComponent.ScannerPort);
        }

        private void OnNewLink(EntityUid uid, BodyScannerConsoleComponent component, NewLinkEvent args)
        {
            if (args.SourcePort == BodyScannerConsoleComponent.ScannerPort &&
                HasComp<OperatingTableComponent>(args.Sink))
            {
                component.SurgeryTable = args.Sink;
            }
            RecheckConnections(uid, component.SurgeryTable, component);
        }

        private void OnPortDisconnected(EntityUid uid, BodyScannerConsoleComponent component, PortDisconnectedEvent args)
        {
            if (args.Port == BodyScannerConsoleComponent.ScannerPort)
                component.SurgeryTable = null;

            UpdateUserInterface(uid, component);
        }

        private void OnAnchorChanged(EntityUid uid, BodyScannerConsoleComponent component, ref AnchorStateChangedEvent args)
        {
            if (args.Anchored)
            {
                RecheckConnections(uid, component.SurgeryTable, component);
                return;
            }
            UpdateUserInterface(uid, component);
        }

        private void OnUIOpen(EntityUid uid, BodyScannerConsoleComponent component, AfterActivatableUIOpenEvent args)
        {
            UpdateUserInterface(uid, component);
        }

        public void RecheckConnections(EntityUid console, EntityUid? table, BodyScannerConsoleComponent? consoleComp = null)
        {
            if (!Resolve(console, ref consoleComp))
                return;

            if (table != null)
            {
                Transform(table.Value).Coordinates.TryDistance(
                    EntityManager,
                    Transform(console).Coordinates,
                    out float scannerDistance);

                consoleComp.SurgeryTableInRange = scannerDistance <= consoleComp.MaxDistance;
            }

            UpdateUserInterface(console, consoleComp);
        }

        public void UpdateUserInterface(EntityUid uid, BodyScannerConsoleComponent? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            if (!_uiSystem.HasUi(uid, BodyScannerUiKey.Key))
                return;

            EntityUid? patient = null;
            if (component.SurgeryTable != null &&
                component.SurgeryTableInRange &&
                TryComp<StrapComponent>(component.SurgeryTable, out var table))
            {
                patient = table.BuckledEntities.FirstOrDefault();
            }

            if (patient == null || !TryComp<OperatedComponent>(patient, out var operated))
            {
                _uiSystem.SetUiState(uid, BodyScannerUiKey.Key,
                    new BodyScannerBoundUserInterfaceState(null, null, null, false));
                return;
            }

            var damages = new List<BodyScannerDamageInfo>();
            foreach (var (damageId, bodyParts) in operated.InternalDamages)
            {
                if (!_prototypeManager.TryIndex(damageId, out var damageProto))
                    continue;

                damages.Add(new BodyScannerDamageInfo(
                    Loc.GetString(damageProto.Name),
                    bodyParts.Select(p => Loc.GetString(p)).ToList()
                ));
            }

            string? patientStatus = null;
            if (TryComp<MobStateComponent>(patient, out var mobState))
            {
                patientStatus = mobState.CurrentState switch
                {
                    MobState.Alive => Loc.GetString("body-scanner-status-alive"),
                    MobState.PreCritical => Loc.GetString("body-scanner-status-critical"),
                    MobState.Critical => Loc.GetString("body-scanner-status-critical"),
                    MobState.Dead => Loc.GetString("body-scanner-status-dead"),
                    _ => Loc.GetString("body-scanner-status-unknown")
                };
            }

            _uiSystem.SetUiState(uid, BodyScannerUiKey.Key,
                new BodyScannerBoundUserInterfaceState(
                    MetaData(patient.Value).EntityName,
                    patientStatus,
                    damages,
                    component.SurgeryTableInRange
                ));
        }
    }
}
