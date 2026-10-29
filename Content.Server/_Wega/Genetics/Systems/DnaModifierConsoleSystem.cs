using System.Linq;
using Content.Server.Administration;
using Content.Server.Medical.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.DeviceLinking.Components;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.DeviceLinking.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Forensics.Components;
using Content.Shared.Genetics;
using Content.Shared.Genetics.Systems;
using Content.Shared.Genetics.UI;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Power;
using Content.Shared.UserInterface;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Genetics.System
{
    [UsedImplicitly]
    public sealed partial class DnaModifierConsoleSystem : EntitySystem
    {
        [Dependency] private SharedAudioSystem _audio = default!;
        [Dependency] private SharedContainerSystem _container = default!;
        [Dependency] private DamageableSystem _damage = default!;
        [Dependency] private DeviceLinkSystem _signalSystem = default!;
        [Dependency] private DnaClientSystem _dnaClient = default!;
        [Dependency] private DnaModifierSystem _dnaModifier = default!;
        [Dependency] private IGameTiming _timing = default!;
        [Dependency] private ItemSlotsSystem _itemSlotsSystem = default!;
        [Dependency] private PowerReceiverSystem _powerReceiverSystem = default!;
        [Dependency] private QuickDialogSystem _quickDialog = default!;
        [Dependency] private IRobustRandom _random = default!;
        [Dependency] private SharedSolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private UserInterfaceSystem _uiSystem = default!;
        [Dependency] private SharedInteractionSystem _interaction = default!;

        private static readonly EntProtoId Injector = "DnaInjector";
        private static readonly ProtoId<DamageTypePrototype> RadDamage = "Radiation";

        private const float MinRadiationValue = 1f;
        private const float MaxRadiationValue = 10f;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DnaModifierConsoleComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<DnaModifierConsoleComponent, AfterActivatableUIOpenEvent>(OnUIOpen);
            SubscribeLocalEvent<DnaModifierConsoleComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<DnaModifierConsoleComponent, NewLinkEvent>(OnNewLink);
            SubscribeLocalEvent<DnaModifierConsoleComponent, SinkPortDisconnectedEvent>(OnPortDisconnected);
            SubscribeLocalEvent<DnaModifierConsoleComponent, AnchorStateChangedEvent>(OnAnchorChanged);

            // mewmewmew
            SubscribeLocalEvent<DnaModifierConsoleComponent, DnaModifierConsoleEjectEvent>(OnEjectPressed);
            SubscribeLocalEvent<DnaModifierConsoleComponent, DnaModifierConsoleEjectRejuveEvent>(OnEjectRejuvePressed);
            SubscribeLocalEvent<DnaModifierConsoleComponent, DnaModifierConsoleReagentButtonEvent>(OnReagentButtonPressed);
            SubscribeLocalEvent<DnaModifierConsoleComponent, DnaModifierConsoleSaveServerEvent>(OnSaveServerPressed);
            SubscribeLocalEvent<DnaModifierConsoleComponent, DnaModifierConsoleClearBufferEvent>(OnClearBufferPressed);
            SubscribeLocalEvent<DnaModifierConsoleComponent, DnaModifierConsoleRenameBufferEvent>(OnRenameBufferPressed);
            SubscribeLocalEvent<DnaModifierConsoleComponent, DnaModifierConsoleInjectorEvent>(OnInjectorPressed);
            SubscribeLocalEvent<DnaModifierConsoleComponent, DnaModifierInjectBlockEvent>(OnInjectBlockPressed);
            SubscribeLocalEvent<DnaModifierConsoleComponent, DnaModifierConsoleSubjectInjectEvent>(OnSubjectInjectPressed);
            SubscribeLocalEvent<DnaModifierConsoleComponent, DnaModifierConsoleExportOnDiskEvent>(OnExportOnDiskPressed);
            SubscribeLocalEvent<DnaModifierConsoleComponent, DnaModifierConsoleExportFromDiskEvent>(OnExportFromDiskPressed);
            SubscribeLocalEvent<DnaModifierConsoleComponent, DnaModifierConsoleClearDiskEvent>(OnClearDiskPressed);
            SubscribeLocalEvent<DnaModifierConsoleComponent, DnaModifierConsoleReleverationEvent>(OnReleverationPressed);
            SubscribeLocalEvent<DnaModifierConsoleComponent, DnaModifierConsoleReleverationsEvent>(OnReleverationsPressed);
        }

        #region UI logic
        private void OnMapInit(EntityUid uid, DnaModifierConsoleComponent component, MapInitEvent args)
        {
            component.LastInjectorTime = _timing.CurTime + component.InjectorCooldown;
            component.LastSubjectInjectTime = _timing.CurTime + component.SubjectInjectCooldown;
            _signalSystem.EnsureSourcePorts(uid, DnaModifierConsoleComponent.ScannerPort);

            Dirty(uid, component);

            if (!TryComp<DeviceLinkSourceComponent>(uid, out var receiver))
                return;

            foreach (var port in receiver.Outputs.Values.SelectMany(ports => ports))
            {
                if (TryComp<MedicalScannerComponent>(port, out var scanner))
                {
                    component.GeneticScanner = port;
                    scanner.ConnectedConsole = uid;
                }
            }
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<DnaModifierConsoleComponent, TransformComponent>();
            while (query.MoveNext(out var uid, out var component, out var transform))
            {
                if (component.NextUpdate > _timing.CurTime)
                    continue;

                component.NextUpdate = _timing.CurTime + component.UpdateInterval;
                if (component.GeneticScanner != null && HasComp<MedicalScannerComponent>(component.GeneticScanner))
                {
                    Transform(component.GeneticScanner.Value).Coordinates.TryDistance(EntityManager, transform.Coordinates, out float scannerDistance);
                    component.GeneticScannerInRange = scannerDistance <= component.MaxDistance;

                    UpdateUserInterface(uid, component);
                }
            }
        }

        private void OnPowerChanged(EntityUid uid, DnaModifierConsoleComponent component, ref PowerChangedEvent args)
        {
            UpdateUserInterface(uid, component);
        }

        private void OnNewLink(EntityUid uid, DnaModifierConsoleComponent component, NewLinkEvent args)
        {
            if (TryComp<MedicalScannerComponent>(args.Sink, out var scanner) && args.SourcePort == DnaModifierConsoleComponent.ScannerPort)
            {
                component.GeneticScanner = args.Sink;
                scanner.ConnectedConsole = uid;
            }

            RecheckConnections(uid, component.GeneticScanner, component);
        }

        private void OnPortDisconnected(EntityUid uid, DnaModifierConsoleComponent component, SinkPortDisconnectedEvent args)
        {
            if (args.Port == DnaModifierConsoleComponent.ScannerPort)
                component.GeneticScanner = null;

            UpdateUserInterface(uid, component);
        }

        private void OnUIOpen(EntityUid uid, DnaModifierConsoleComponent component, AfterActivatableUIOpenEvent args)
        {
            UpdateUserInterface(uid, component);
        }

        private void OnAnchorChanged(EntityUid uid, DnaModifierConsoleComponent component, ref AnchorStateChangedEvent args)
        {
            if (args.Anchored)
            {
                RecheckConnections(uid, component.GeneticScanner, component);
                return;
            }
            UpdateUserInterface(uid, component);
        }

        public void UpdateUserInterface(EntityUid consoleUid, DnaModifierConsoleComponent consoleComponent)
        {
            if (!_uiSystem.HasUi(consoleUid, DnaModifierUiKey.Key))
                return;

            if (!_powerReceiverSystem.IsPowered(consoleUid))
            {
                _uiSystem.CloseUis(consoleUid);
                return;
            }

            var newState = GetUserInterfaceState((consoleUid, consoleComponent));
            _uiSystem.SetUiState(consoleUid, DnaModifierUiKey.Key, newState);
        }

        public void RecheckConnections(EntityUid console, EntityUid? scanner, DnaModifierConsoleComponent? consoleComp = null)
        {
            if (!Resolve(console, ref consoleComp))
                return;

            if (scanner != null)
            {
                Transform(scanner.Value).Coordinates.TryDistance(EntityManager, Transform(console).Coordinates, out float scannerDistance);
                consoleComp.GeneticScannerInRange = scannerDistance <= consoleComp.MaxDistance;
            }

            UpdateUserInterface(console, consoleComp);
        }

        private DnaModifierBoundUserInterfaceState GetUserInterfaceState(Entity<DnaModifierConsoleComponent> ent)
        {
            // genetic scanner info
            EntityUid? inputContainer = null;
            NetEntity console = GetNetEntity(ent);

            string scanBodyInfo = string.Empty;
            string scannerBodyStatus = string.Empty;
            string scannerBodyDna = string.Empty;

            float scannerBodyHealth = -1;
            float scannerBodyRadiation = 0;

            bool hasDisk = false;
            bool scannerHasBeaker = false;
            bool scannerInRange = ent.Comp.GeneticScannerInRange;

            EnzymeInfo? enzyme = null;
            UniqueIdentifiersData? uniqueIdentifiers = null;
            List<EnzymesPrototypeInfo>? enzymesPrototypes = null;

            var currentTime = _timing.CurTime;
            var injectorCooldown = ent.Comp.LastInjectorTime + ent.Comp.InjectorCooldown;
            var subjectInjectCooldown = ent.Comp.LastSubjectInjectTime + ent.Comp.SubjectInjectCooldown;

            var buffer = GetAllBuffers(ent);
            if (ent.Comp.GeneticScanner != null && TryComp<MedicalScannerComponent>(ent.Comp.GeneticScanner, out var scanner))
            {
                EntityUid? scanBody = scanner.BodyContainer.ContainedEntity;
                inputContainer = _itemSlotsSystem.GetItemOrNull(ent.Comp.GeneticScanner.Value, SharedDnaModifier.InputSlotName);

                if (_itemSlotsSystem.TryGetSlot(ent.Owner, SharedDnaModifier.DiskSlotName, out var diskSlot)
                    && diskSlot.HasItem && diskSlot.Item != null)
                {
                    hasDisk = true;
                    if (_dnaModifier.TryGetDataFromDisk(diskSlot.Item.Value, out var data))
                        enzyme = data;
                }

                if (_itemSlotsSystem.TryGetSlot(ent.Comp.GeneticScanner.Value, SharedDnaModifier.InputSlotName, out var beakerSlot)
                    && beakerSlot.HasItem)
                {
                    scannerHasBeaker = true;
                }

                // GET STATE
                if (scanBody != null && TryComp<MobStateComponent>(scanBody, out var mobState))
                {
                    scanBodyInfo = MetaData(scanBody.Value).EntityName;
                    scannerBodyStatus = (mobState.CurrentState != MobState.Invalid)
                        ? GetStatus(mobState.CurrentState)
                        : Loc.GetString("dna-modifier-entity-unknown-text");

                    if (TryComp<DnaComponent>(scanBody.Value, out var dna))
                        scannerBodyDna = dna.DNA ?? string.Empty;

                    if (TryComp<DamageableComponent>(scanBody.Value, out var damage))
                    {
                        if (TryComp<MobThresholdsComponent>(scanBody.Value, out var mobThresholds))
                        {
                            FixedPoint2 deathHealth = FixedPoint2.Zero;
                            foreach (var threshold in mobThresholds.Thresholds)
                            {
                                if (threshold.Value == MobState.Dead)
                                {
                                    deathHealth = threshold.Key;
                                    break;
                                }
                            }

                            if (deathHealth > FixedPoint2.Zero)
                            {
                                var totalDamage = _damage.GetTotalDamage(scanBody.Value);
                                float currentHealth = 1.0f - (totalDamage.Float() / deathHealth.Float());
                                scannerBodyHealth = Math.Clamp(currentHealth, 0f, 1f);
                            }
                        }

                        var positiveDamage = _damage.GetPositiveDamage((scanBody.Value, damage));
                        if (positiveDamage.DamageDict.TryGetValue(RadDamage, out var radiationDamage))
                            scannerBodyRadiation = Math.Clamp(radiationDamage.Float() / 200f, 0f, 1f);
                    }

                    if (TryComp<DnaModifierComponent>(scanBody.Value, out var dnaModifier))
                    {
                        uniqueIdentifiers = dnaModifier.UniqueIdentifiers;
                        enzymesPrototypes = dnaModifier.EnzymesPrototypes;
                    }
                }
            }

            return new DnaModifierBoundUserInterfaceState(
                console,
                uniqueIdentifiers,
                enzymesPrototypes,
                enzyme,
                scanBodyInfo,
                scannerBodyStatus,
                scannerBodyDna,
                scannerBodyHealth,
                scannerBodyRadiation,
                scannerHasBeaker,
                BuildInputContainerInfo(inputContainer),
                scannerInRange,
                hasDisk,
                buffer,
                currentTime < injectorCooldown ? injectorCooldown - currentTime : TimeSpan.Zero,
                currentTime < subjectInjectCooldown ? subjectInjectCooldown - currentTime : TimeSpan.Zero
            );
        }

        private string GetStatus(MobState mobState)
        {
            return mobState switch
            {
                MobState.Alive => Loc.GetString("dna-modifier-entity-alive-text"),
                MobState.PreCritical => Loc.GetString("dna-modifier-entity-critical-text"),
                MobState.Critical => Loc.GetString("dna-modifier-entity-critical-text"),
                MobState.Dead => Loc.GetString("dna-modifier-entity-dead-text"),
                _ => Loc.GetString("dna-modifier-entity-unknown-text"),
            };
        }

        private ContainerInfo? BuildInputContainerInfo(EntityUid? container)
        {
            if (container is not { Valid: true })
                return null;

            if (!TryComp(container, out FitsInDispenserComponent? fits)
                || !_solutionContainerSystem.TryGetSolution(container.Value, fits.Solution, out _, out var solution))
            {
                return null;
            }

            return BuildContainerInfo(Name(container.Value), solution);
        }

        private static ContainerInfo BuildContainerInfo(string name, Solution solution)
        {
            return new ContainerInfo(name, solution.Volume, solution.MaxVolume)
            {
                Reagents = solution.Contents
            };
        }

        public Dictionary<int, EnzymeInfo?> GetAllBuffers(EntityUid uid)
        {
            var buffers = new Dictionary<int, EnzymeInfo?>();
            if (!TryComp<DnaClientComponent>(uid, out var client))
                return buffers;

            for (int i = 1; i <= 3; i++)
            {
                if (_dnaClient.TryGetBufferData((uid, client), i, out var data))
                    buffers[i] = data;
            }

            return buffers;
        }
        #endregion

        #region Console logic

        private bool CanInteract(EntityUid console, EntityUid actor)
        {
            if (!actor.IsValid())
                return false;

            if (!_interaction.InRangeUnobstructed(actor, console))
                return false;

            return true;
        }

        private void PlayClickSound(EntityUid console, DnaModifierConsoleComponent component)
            => _audio.PlayPvs(component.ClickSound, console, AudioParams.Default.WithVolume(-2f));

        private void OnEjectPressed(EntityUid uid, DnaModifierConsoleComponent console, DnaModifierConsoleEjectEvent args)
        {
            if (!CanInteract(uid, args.Actor))
                return;

            if (console.GeneticScanner == null || !HasComp<MedicalScannerComponent>(console.GeneticScanner))
                return;

            if (_container.TryGetContainer(console.GeneticScanner.Value, SharedDnaModifier.OccupantSlotName, out var container))
                _container.EmptyContainer(container);

            PlayClickSound(uid, console);
            UpdateUserInterface(uid, console);
        }

        private void OnEjectRejuvePressed(EntityUid uid, DnaModifierConsoleComponent console, DnaModifierConsoleEjectRejuveEvent args)
        {
            if (!CanInteract(uid, args.Actor))
                return;

            if (console.GeneticScanner == null || !HasComp<MedicalScannerComponent>(console.GeneticScanner))
                return;

            if (_itemSlotsSystem.TryGetSlot(console.GeneticScanner.Value, SharedDnaModifier.InputSlotName, out var slot))
                _itemSlotsSystem.TryEject(console.GeneticScanner.Value, slot, null, out var _, true);

            PlayClickSound(uid, console);
            UpdateUserInterface(uid, console);
        }

        private void OnReagentButtonPressed(EntityUid uid, DnaModifierConsoleComponent console, DnaModifierConsoleReagentButtonEvent args)
        {
            if (!CanInteract(uid, args.Actor))
                return;

            if (console.GeneticScanner == null)
                return;

            if (!TryComp<MedicalScannerComponent>(console.GeneticScanner, out var scanner) || scanner.BodyContainer.ContainedEntity == null
                || !_itemSlotsSystem.TryGetSlot(console.GeneticScanner.Value, SharedDnaModifier.InputSlotName, out var slot)
                || slot.Item == null)
                return;

            if (!_solutionContainerSystem.TryGetSolution(slot.Item.Value, SharedDnaModifier.SolutionSlotName, out var sourceSolution, out var sourceSolutionComp))
                return;

            var targetEntity = scanner.BodyContainer.ContainedEntity.Value;
            if (!_solutionContainerSystem.TryGetInjectableSolution(targetEntity, out var targetSolution, out _))
                return;

            FixedPoint2 transferAmount = args.Amount switch
            {
                DnaModifierReagentAmount.U1 => FixedPoint2.New(1),
                DnaModifierReagentAmount.U5 => FixedPoint2.New(5),
                DnaModifierReagentAmount.U10 => FixedPoint2.New(10),
                DnaModifierReagentAmount.U25 => FixedPoint2.New(25),
                DnaModifierReagentAmount.U50 => FixedPoint2.New(50),
                DnaModifierReagentAmount.U100 => FixedPoint2.New(100),
                DnaModifierReagentAmount.All => sourceSolutionComp.GetReagentQuantity(args.ReagentId),
                _ => FixedPoint2.Zero
            };

            if (transferAmount <= FixedPoint2.Zero || sourceSolutionComp.GetReagentQuantity(args.ReagentId) < transferAmount)
                return;

            var reagentSolution = new Solution();
            reagentSolution.AddReagent(args.ReagentId, transferAmount);

            _solutionContainerSystem.RemoveReagent(sourceSolution.Value, args.ReagentId, transferAmount);
            if (!_solutionContainerSystem.TryAddSolution(targetSolution.Value, reagentSolution))
                return;

            PlayClickSound(uid, console);
            UpdateUserInterface(uid, console);
        }

        private void OnSaveServerPressed(EntityUid uid, DnaModifierConsoleComponent console, DnaModifierConsoleSaveServerEvent args)
        {
            if (!CanInteract(uid, args.Actor))
                return;

            if (console.GeneticScanner == null || !TryComp<DnaClientComponent>(uid, out var client))
                return;

            if (!TryComp<MedicalScannerComponent>(console.GeneticScanner, out var scanner))
                return;

            var scanBody = scanner.BodyContainer.ContainedEntity;
            if (!TryComp<DnaModifierComponent>(scanBody, out var dnaModifier))
                return;

            EnzymeInfo? dataToSend = null;
            switch (args.CurrentType)
            {
                case 1:
                    if (dnaModifier.UniqueIdentifiers != null)
                    {
                        dataToSend = new EnzymeInfo()
                        {
                            Identifier = _dnaModifier.CloneUniqueIdentifiers(dnaModifier.UniqueIdentifiers),
                            Info = null
                        };
                    }
                    break;

                case 2:
                    if (dnaModifier.UniqueIdentifiers != null && dnaModifier.EnzymesPrototypes != null)
                    {
                        dataToSend = new EnzymeInfo()
                        {
                            Identifier = _dnaModifier.CloneUniqueIdentifiers(dnaModifier.UniqueIdentifiers),
                            Info = _dnaModifier.CloneEnzymesPrototypes(dnaModifier.EnzymesPrototypes)
                        };
                    }
                    break;

                case 3:
                    if (dnaModifier.EnzymesPrototypes != null)
                    {
                        dataToSend = new EnzymeInfo()
                        {
                            Identifier = null,
                            Info = _dnaModifier.CloneEnzymesPrototypes(dnaModifier.EnzymesPrototypes)
                        };
                    }
                    break;

                default: return;
            }

            if (dataToSend == null)
                return;

            _dnaClient.TryAddToBuffer((uid, client), args.CurrentSection, dataToSend);

            PlayClickSound(uid, console);
            UpdateUserInterface(uid, console);
        }

        private void OnClearBufferPressed(EntityUid uid, DnaModifierConsoleComponent console, DnaModifierConsoleClearBufferEvent args)
        {
            if (!CanInteract(uid, args.Actor))
                return;

            if (!TryComp<DnaClientComponent>(uid, out var client))
                return;

            _dnaClient.TryClearBuffer((uid, client), args.Index);

            PlayClickSound(uid, console);
            UpdateUserInterface(uid, console);
        }

        private void OnRenameBufferPressed(EntityUid uid, DnaModifierConsoleComponent console, DnaModifierConsoleRenameBufferEvent args)
        {
            if (!CanInteract(uid, args.Actor))
                return;

            if (!TryComp<DnaClientComponent>(uid, out var client))
                return;

            if (!_dnaClient.TryGetBufferData((uid, client), args.Index, out var data))
                return;

            if (!TryComp<ActorComponent>(args.Actor, out var playerActor))
                return;

            var playerSession = playerActor.PlayerSession;
            _quickDialog.OpenDialog(playerSession, Loc.GetString("dna-modifier-button-rename"), "",
                (string name) =>
                {
                    if (playerSession.AttachedEntity is not { } user)
                        return;

                    if (!_interaction.InRangeUnobstructed(user, uid))
                        return;

                    var finalName = string.IsNullOrWhiteSpace(name)
                        ? data.SampleName
                        : name;

                    _dnaClient.TryRenameBuffer((uid, client), args.Index, finalName);

                    PlayClickSound(uid, console);
                    UpdateUserInterface(uid, console);
                });
        }

        private void OnInjectorPressed(EntityUid uid, DnaModifierConsoleComponent console, DnaModifierConsoleInjectorEvent args)
        {
            if (!CanInteract(uid, args.Actor))
                return;

            if (console.GeneticScanner == null || !TryComp<DnaClientComponent>(uid, out var client))
                return;

            if (_timing.CurTime < console.LastInjectorTime + console.InjectorCooldown)
                return;

            if (!_dnaClient.TryGetBufferData((uid, client), args.Index, out var data))
                return;

            _dnaModifier.OnFillingInjector(Spawn(Injector, Transform(uid).Coordinates),
                data.Identifier, data.Info);

            console.LastInjectorTime = _timing.CurTime;

            PlayClickSound(uid, console);
            UpdateUserInterface(uid, console);
        }

        private void OnInjectBlockPressed(EntityUid uid, DnaModifierConsoleComponent console, DnaModifierInjectBlockEvent args)
        {
            if (!CanInteract(uid, args.Actor))
                return;

            if (console.GeneticScanner == null || !TryComp<DnaClientComponent>(uid, out var client))
                return;

            if (_timing.CurTime < console.LastInjectorTime + console.InjectorCooldown)
                return;

            if (!_dnaClient.TryGetBufferData((uid, client), args.Index, out var data) || data.Info == null)
                return;

            var targetBlock = data.Info.FirstOrDefault(e => e.Order == args.CurrentBlock);
            if (targetBlock == null)
                return;

            var singleBlockInfo = new List<EnzymesPrototypeInfo> { targetBlock };
            _dnaModifier.OnFillingInjector(Spawn(Injector, Transform(uid).Coordinates), null, singleBlockInfo);

            console.LastInjectorTime = _timing.CurTime;

            PlayClickSound(uid, console);
            UpdateUserInterface(uid, console);
        }

        private void OnSubjectInjectPressed(EntityUid uid, DnaModifierConsoleComponent console, DnaModifierConsoleSubjectInjectEvent args)
        {
            if (!CanInteract(uid, args.Actor))
                return;

            if (console.GeneticScanner == null || !TryComp<DnaClientComponent>(uid, out var client))
                return;

            if (_timing.CurTime < console.LastSubjectInjectTime + console.SubjectInjectCooldown)
                return;

            if (!TryComp<MedicalScannerComponent>(console.GeneticScanner, out var scanner))
                return;

            var scanBody = scanner.BodyContainer.ContainedEntity;
            if (!TryComp<DnaModifierComponent>(scanBody, out var dnaModifier) || !scanBody.HasValue)
                return;

            if (!_dnaClient.TryGetBufferData((uid, client), args.Index, out var data))
                return;

            PlayClickSound(uid, console);
            _dnaModifier.ChangeDna((scanBody.Value, dnaModifier), data);

            console.LastSubjectInjectTime = _timing.CurTime;

            var damage = new DamageSpecifier { DamageDict = { { RadDamage, 20 } } };
            _damage.TryChangeDamage(scanBody.Value, damage, true);

            UpdateUserInterface(uid, console);
        }

        private void OnExportOnDiskPressed(EntityUid uid, DnaModifierConsoleComponent console, DnaModifierConsoleExportOnDiskEvent args)
        {
            if (!CanInteract(uid, args.Actor))
                return;

            if (console.GeneticScanner == null || !TryComp<DnaClientComponent>(uid, out var client))
                return;

            if (!_dnaClient.TryGetBufferData((uid, client), args.Index, out var data))
                return;

            if (_itemSlotsSystem.TryGetSlot(uid, SharedDnaModifier.DiskSlotName, out var diskSlot) && diskSlot.Item != null)
            {
                _dnaModifier.TrySaveInDisk(diskSlot.Item.Value, data);

                PlayClickSound(uid, console);
                UpdateUserInterface(uid, console);
            }
        }

        private void OnExportFromDiskPressed(EntityUid uid, DnaModifierConsoleComponent console, DnaModifierConsoleExportFromDiskEvent args)
        {
            if (!CanInteract(uid, args.Actor))
                return;

            if (console.GeneticScanner == null || !TryComp<DnaClientComponent>(uid, out var client))
                return;

            if (_itemSlotsSystem.TryGetSlot(uid, SharedDnaModifier.DiskSlotName, out var diskSlot) && diskSlot.Item != null)
            {
                _dnaModifier.TryGetDataFromDisk(diskSlot.Item.Value, out var data);
                if (data == null)
                    return;

                _dnaClient.TryAddToBufferDisk((uid, client), args.Index, data);

                PlayClickSound(uid, console);
                UpdateUserInterface(uid, console);
            }
        }

        private void OnClearDiskPressed(EntityUid uid, DnaModifierConsoleComponent console, DnaModifierConsoleClearDiskEvent args)
        {
            if (!CanInteract(uid, args.Actor))
                return;

            if (console.GeneticScanner == null)
                return;

            if (_itemSlotsSystem.TryGetSlot(uid, SharedDnaModifier.DiskSlotName, out var diskSlot) && diskSlot.Item != null)
            {
                PlayClickSound(uid, console);
                _dnaModifier.TryClearDiskData(diskSlot.Item.Value);
            }
        }

        private void OnReleverationPressed(EntityUid uid, DnaModifierConsoleComponent console, DnaModifierConsoleReleverationEvent args)
        {
            if (!CanInteract(uid, args.Actor))
                return;

            if (console.GeneticScanner == null)
                return;

            if (!TryComp<MedicalScannerComponent>(console.GeneticScanner, out var scanner))
                return;

            var scanBody = scanner.BodyContainer.ContainedEntity;
            if (!TryComp<DnaModifierComponent>(scanBody, out var dnaModifier) || !scanBody.HasValue)
                return;

            var intensity = Math.Clamp(args.Intensity, MinRadiationValue, MaxRadiationValue);

            if (args.CurrentTab == 0 && dnaModifier.UniqueIdentifiers != null)
            {
                ModifyUniqueIdentifiers(dnaModifier.UniqueIdentifiers, args.CurrentBlock, args.CurrentValue, intensity);
            }
            else if (args.CurrentTab == 1 && dnaModifier.EnzymesPrototypes != null)
            {
                ModifyEnzymesPrototypes(dnaModifier.EnzymesPrototypes, args.CurrentBlock, args.CurrentValue, intensity);
            }
            else
            {
                return;
            }

            AddRadiationDamage(scanBody.Value, intensity);
            Dirty(scanBody.Value, dnaModifier);

            _dnaModifier.ChangeDna((scanBody.Value, dnaModifier), args.CurrentTab);

            PlayClickSound(uid, console);
            UpdateUserInterface(uid, console);
        }

        private void OnReleverationsPressed(EntityUid uid, DnaModifierConsoleComponent console, DnaModifierConsoleReleverationsEvent args)
        {
            if (!CanInteract(uid, args.Actor))
                return;

            if (console.GeneticScanner == null)
                return;

            if (!TryComp<MedicalScannerComponent>(console.GeneticScanner, out var scanner))
                return;

            var scanBody = scanner.BodyContainer.ContainedEntity;
            if (!TryComp<DnaModifierComponent>(scanBody, out var dnaModifier) || !scanBody.HasValue)
                return;

            var intensity = Math.Clamp(args.Intensity, MinRadiationValue, MaxRadiationValue);
            var duration = Math.Clamp(args.Duration, MinRadiationValue, MaxRadiationValue);

            int type = -1;
            if (args.CurrentTab == 0 && dnaModifier.UniqueIdentifiers != null)
            {
                type = 0;
                ModifyUniqueIdentifiers(dnaModifier.UniqueIdentifiers, intensity, duration);
            }
            else if (args.CurrentTab == 1 && dnaModifier.EnzymesPrototypes != null)
            {
                type = 1;
                ModifyEnzymesPrototypes(dnaModifier.EnzymesPrototypes, intensity, duration);
            }
            else
            {
                return;
            }

            AddRadiationDamage(scanBody.Value, intensity);
            Dirty(scanBody.Value, dnaModifier);

            _dnaModifier.ChangeDna((scanBody.Value, dnaModifier), type);

            PlayClickSound(uid, console);
            UpdateUserInterface(uid, console);
        }

        private void ModifyUniqueIdentifiers(UniqueIdentifiersData uniqueIdentifiers, string block, int value, float intensity)
        {
            var fields = new List<(string[] Field, string Name)>
            {
                (uniqueIdentifiers.HairColorR, nameof(uniqueIdentifiers.HairColorR)),
                (uniqueIdentifiers.HairColorG, nameof(uniqueIdentifiers.HairColorG)),
                (uniqueIdentifiers.HairColorB, nameof(uniqueIdentifiers.HairColorB)),
                (uniqueIdentifiers.SecondaryHairColorR, nameof(uniqueIdentifiers.SecondaryHairColorR)),
                (uniqueIdentifiers.SecondaryHairColorG, nameof(uniqueIdentifiers.SecondaryHairColorG)),
                (uniqueIdentifiers.SecondaryHairColorB, nameof(uniqueIdentifiers.SecondaryHairColorB)),
                (uniqueIdentifiers.BeardColorR, nameof(uniqueIdentifiers.BeardColorR)),
                (uniqueIdentifiers.BeardColorG, nameof(uniqueIdentifiers.BeardColorG)),
                (uniqueIdentifiers.BeardColorB, nameof(uniqueIdentifiers.BeardColorB)),
                (uniqueIdentifiers.SkinTone, nameof(uniqueIdentifiers.SkinTone)),
                (uniqueIdentifiers.FurColorR, nameof(uniqueIdentifiers.FurColorR)),
                (uniqueIdentifiers.FurColorG, nameof(uniqueIdentifiers.FurColorG)),
                (uniqueIdentifiers.FurColorB, nameof(uniqueIdentifiers.FurColorB)),
                (uniqueIdentifiers.HeadAccessoryColorR, nameof(uniqueIdentifiers.HeadAccessoryColorR)),
                (uniqueIdentifiers.HeadAccessoryColorG, nameof(uniqueIdentifiers.HeadAccessoryColorG)),
                (uniqueIdentifiers.HeadAccessoryColorB, nameof(uniqueIdentifiers.HeadAccessoryColorB)),
                (uniqueIdentifiers.HeadMarkingColorR, nameof(uniqueIdentifiers.HeadMarkingColorR)),
                (uniqueIdentifiers.HeadMarkingColorG, nameof(uniqueIdentifiers.HeadMarkingColorG)),
                (uniqueIdentifiers.HeadMarkingColorB, nameof(uniqueIdentifiers.HeadMarkingColorB)),
                (uniqueIdentifiers.BodyMarkingColorR, nameof(uniqueIdentifiers.BodyMarkingColorR)),
                (uniqueIdentifiers.BodyMarkingColorG, nameof(uniqueIdentifiers.BodyMarkingColorG)),
                (uniqueIdentifiers.BodyMarkingColorB, nameof(uniqueIdentifiers.BodyMarkingColorB)),
                (uniqueIdentifiers.TailMarkingColorR, nameof(uniqueIdentifiers.TailMarkingColorR)),
                (uniqueIdentifiers.TailMarkingColorG, nameof(uniqueIdentifiers.TailMarkingColorG)),
                (uniqueIdentifiers.TailMarkingColorB, nameof(uniqueIdentifiers.TailMarkingColorB)),
                (uniqueIdentifiers.EyeColorR, nameof(uniqueIdentifiers.EyeColorR)),
                (uniqueIdentifiers.EyeColorG, nameof(uniqueIdentifiers.EyeColorG)),
                (uniqueIdentifiers.EyeColorB, nameof(uniqueIdentifiers.EyeColorB)),
                (uniqueIdentifiers.Gender, nameof(uniqueIdentifiers.Gender)),
                (uniqueIdentifiers.BeardStyle, nameof(uniqueIdentifiers.BeardStyle)),
                (uniqueIdentifiers.HairStyle, nameof(uniqueIdentifiers.HairStyle)),
                (uniqueIdentifiers.HeadAccessoryStyle, nameof(uniqueIdentifiers.HeadAccessoryStyle)),
                (uniqueIdentifiers.HeadMarkingStyle, nameof(uniqueIdentifiers.HeadMarkingStyle)),
                (uniqueIdentifiers.BodyMarkingStyle, nameof(uniqueIdentifiers.BodyMarkingStyle)),
                (uniqueIdentifiers.TailMarkingStyle, nameof(uniqueIdentifiers.TailMarkingStyle))
            };

            if (_random.NextFloat() < 0.025f)
            {
                var randomField = fields[_random.Next(fields.Count)];
                int randomIndex = _random.Next(0, randomField.Field.Length);
                if (randomField.Name == nameof(uniqueIdentifiers.SkinTone))
                {
                    randomField.Field[randomIndex] = GenerateSkinToneComponent(randomIndex, intensity, 1.0f);
                }
                else
                {
                    randomField.Field[randomIndex] = GenerateRandomHexValue(randomField.Field[randomIndex], intensity, 1.0f);
                }
                return;
            }

            if (!int.TryParse(block, out int blockNumber))
                return;

            var blockMap = new Dictionary<int, int>
            {
                { 1, 0 }, { 2, 1 }, { 3, 2 }, { 4, 3 }, { 5, 4 }, { 6, 5 }, { 7, 6 },
                { 8, 7 }, { 9, 8 }, { 13, 9 },
                { 14, 10 }, { 15, 11 }, { 16, 12 }, { 17, 13 }, { 18, 14 }, { 19, 15 },
                { 20, 16 }, { 21, 17 }, { 22, 18 }, { 23, 19 }, { 24, 20 }, { 25, 21 },
                { 26, 22 }, { 27, 23 }, { 28, 24 }, { 29, 25 }, { 30, 26 }, { 31, 27 },
                { 32, 28 }, { 33, 29 }, { 34, 30 }, { 35, 31 }, { 36, 32 }, { 37, 33 },
                { 38, 34 }
            };

            if (!blockMap.ContainsKey(blockNumber))
                return;

            int blockIndex = blockMap[blockNumber];
            if (blockIndex < 0 || blockIndex >= fields.Count)
                return;

            var field = fields[blockIndex].Field;
            if (value < 0 || value >= field.Length)
                return;

            if (blockNumber == 13)
            {
                if (value >= 0 && value < uniqueIdentifiers.SkinTone.Length)
                {
                    uniqueIdentifiers.SkinTone[value] = GenerateSkinToneComponent(value, intensity, 1.0f);
                }
                return;
            }

            field[value] = GenerateRandomHexValue(field[value], intensity, 1.0f);
        }

        private void ModifyUniqueIdentifiers(UniqueIdentifiersData uniqueIdentifiers, float intensity, float duration)
        {
            var fields = new List<(string[] Field, string Name)>
            {
                (uniqueIdentifiers.HairColorR, nameof(uniqueIdentifiers.HairColorR)),
                (uniqueIdentifiers.HairColorG, nameof(uniqueIdentifiers.HairColorG)),
                (uniqueIdentifiers.HairColorB, nameof(uniqueIdentifiers.HairColorB)),
                (uniqueIdentifiers.SecondaryHairColorR, nameof(uniqueIdentifiers.SecondaryHairColorR)),
                (uniqueIdentifiers.SecondaryHairColorG, nameof(uniqueIdentifiers.SecondaryHairColorG)),
                (uniqueIdentifiers.SecondaryHairColorB, nameof(uniqueIdentifiers.SecondaryHairColorB)),
                (uniqueIdentifiers.BeardColorR, nameof(uniqueIdentifiers.BeardColorR)),
                (uniqueIdentifiers.BeardColorG, nameof(uniqueIdentifiers.BeardColorG)),
                (uniqueIdentifiers.BeardColorB, nameof(uniqueIdentifiers.BeardColorB)),
                (uniqueIdentifiers.SkinTone, nameof(uniqueIdentifiers.SkinTone)),
                (uniqueIdentifiers.FurColorR, nameof(uniqueIdentifiers.FurColorR)),
                (uniqueIdentifiers.FurColorG, nameof(uniqueIdentifiers.FurColorG)),
                (uniqueIdentifiers.FurColorB, nameof(uniqueIdentifiers.FurColorB)),
                (uniqueIdentifiers.HeadAccessoryColorR, nameof(uniqueIdentifiers.HeadAccessoryColorR)),
                (uniqueIdentifiers.HeadAccessoryColorG, nameof(uniqueIdentifiers.HeadAccessoryColorG)),
                (uniqueIdentifiers.HeadAccessoryColorB, nameof(uniqueIdentifiers.HeadAccessoryColorB)),
                (uniqueIdentifiers.HeadMarkingColorR, nameof(uniqueIdentifiers.HeadMarkingColorR)),
                (uniqueIdentifiers.HeadMarkingColorG, nameof(uniqueIdentifiers.HeadMarkingColorG)),
                (uniqueIdentifiers.HeadMarkingColorB, nameof(uniqueIdentifiers.HeadMarkingColorB)),
                (uniqueIdentifiers.BodyMarkingColorR, nameof(uniqueIdentifiers.BodyMarkingColorR)),
                (uniqueIdentifiers.BodyMarkingColorG, nameof(uniqueIdentifiers.BodyMarkingColorG)),
                (uniqueIdentifiers.BodyMarkingColorB, nameof(uniqueIdentifiers.BodyMarkingColorB)),
                (uniqueIdentifiers.TailMarkingColorR, nameof(uniqueIdentifiers.TailMarkingColorR)),
                (uniqueIdentifiers.TailMarkingColorG, nameof(uniqueIdentifiers.TailMarkingColorG)),
                (uniqueIdentifiers.TailMarkingColorB, nameof(uniqueIdentifiers.TailMarkingColorB)),
                (uniqueIdentifiers.EyeColorR, nameof(uniqueIdentifiers.EyeColorR)),
                (uniqueIdentifiers.EyeColorG, nameof(uniqueIdentifiers.EyeColorG)),
                (uniqueIdentifiers.EyeColorB, nameof(uniqueIdentifiers.EyeColorB)),
                (uniqueIdentifiers.Gender, nameof(uniqueIdentifiers.Gender)),
                (uniqueIdentifiers.BeardStyle, nameof(uniqueIdentifiers.BeardStyle)),
                (uniqueIdentifiers.HairStyle, nameof(uniqueIdentifiers.HairStyle)),
                (uniqueIdentifiers.HeadAccessoryStyle, nameof(uniqueIdentifiers.HeadAccessoryStyle)),
                (uniqueIdentifiers.HeadMarkingStyle, nameof(uniqueIdentifiers.HeadMarkingStyle)),
                (uniqueIdentifiers.BodyMarkingStyle, nameof(uniqueIdentifiers.BodyMarkingStyle)),
                (uniqueIdentifiers.TailMarkingStyle, nameof(uniqueIdentifiers.TailMarkingStyle))
            };

            int fieldsToModify = Math.Clamp((int)intensity, 1, 3);
            for (int i = 0; i < fieldsToModify; i++)
            {
                var fieldIndex = _random.Next(fields.Count);
                var fieldName = fields[fieldIndex].Name;
                var field = fields[fieldIndex].Field;

                if (fieldName == nameof(uniqueIdentifiers.SkinTone))
                {
                    for (int j = 0; j < field.Length; j++)
                    {
                        field[j] = GenerateSkinToneComponent(j, intensity, duration);
                    }
                    continue;
                }

                for (int j = 0; j < field.Length; j++)
                {
                    field[j] = GenerateRandomHexValue(field[j], intensity, duration);
                }
            }
        }

        private void ModifyEnzymesPrototypes(List<EnzymesPrototypeInfo> enzymesPrototypes, string block, int value, float intensity)
        {
            if (_random.NextFloat() < 0.025f)
            {
                var randomEnzyme = enzymesPrototypes[_random.Next(enzymesPrototypes.Count)];
                int randomIndex = _random.Next(0, randomEnzyme.HexCode.Length);
                randomEnzyme.HexCode[randomIndex] = GenerateRandomHexValue(randomEnzyme.HexCode[randomIndex], intensity, 1.0f);
                return;
            }

            if (!int.TryParse(block, out int blockNumber))
                return;

            int blockIndex = blockNumber - 1;
            if (blockIndex < 0 || blockIndex >= enzymesPrototypes.Count)
                return;

            var enzyme = enzymesPrototypes[blockIndex];
            if (value < 0 || value >= enzyme.HexCode.Length)
                return;

            enzyme.HexCode[value] = GenerateRandomHexValue(enzyme.HexCode[value], intensity, 1.0f);
        }

        private void ModifyEnzymesPrototypes(List<EnzymesPrototypeInfo> enzymesPrototypes, float intensity, float duration)
        {
            int itemsToModify = Math.Clamp((int)intensity, 1, 2);
            for (int i = 0; i < itemsToModify; i++)
            {
                var enzymeIndex = _random.Next(enzymesPrototypes.Count);
                var enzyme = enzymesPrototypes[enzymeIndex];
                for (int j = 0; j < enzyme.HexCode.Length; j++)
                {
                    enzyme.HexCode[j] = GenerateRandomHexValue(enzyme.HexCode[j], intensity, duration);
                }
            }
        }

        private string GenerateRandomHexValue(string value, float intensity, float duration)
        {
            int baseValue = Convert.ToInt32(value, 16);
            float changeStrength = Math.Clamp((intensity * duration) / 100f, 0f, 1f);
            changeStrength = (float)Math.Sqrt(changeStrength);

            float threshold = 0.15f;
            if (_random.NextFloat() <= threshold)
                return baseValue.ToString("X1");

            float maxChange = 16 * changeStrength;
            float direction = _random.NextFloat() > 0.5f ? 1 : -1;

            int modifiedValue = (int)(baseValue + (direction * maxChange * _random.NextFloat())) % 16;

            if (modifiedValue < 0) modifiedValue += 16;
            else if (modifiedValue >= 16) modifiedValue -= 16;

            return modifiedValue.ToString("X1");
        }

        private string GenerateSkinToneComponent(int index, float intensity, float duration)
        {
            switch (index)
            {
                case 0: return (_random.NextFloat() < intensity / 100f) ? "1" : "0";
                case 1: int digit1 = _random.Next(0, 10); return digit1.ToString("X1");
                case 2: int digit2 = _random.Next(0, 10); return digit2.ToString("X1");
                default: return "0";
            }
        }

        private void AddRadiationDamage(EntityUid uid, float intensity)
        {
            float randomMultiplier = 1.5f + _random.NextFloat() * 1.5f;
            var damage = new DamageSpecifier { DamageDict = { { RadDamage, randomMultiplier * intensity } } };
            _damage.TryChangeDamage(uid, damage, ignoreResistances: true, origin: uid);
        }
        #endregion
    }
}
