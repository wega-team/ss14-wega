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
using Content.Shared.Humanoid;
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

        #region UI logic
        [SubscribeLocalEvent]
        private void OnMapInit(EntityUid uid, DnaModifierConsoleComponent component, MapInitEvent args)
        {
            component.LastInjectorTime = _timing.CurTime + component.InjectorCooldown;
            component.LastSubjectInjectTime = _timing.CurTime + component.SubjectInjectCooldown;
            _signalSystem.EnsureSourcePorts(uid, DnaModifierConsoleComponent.ScannerPort);
            Dirty(uid, component);

            if (!TryComp<DeviceLinkSourceComponent>(uid, out var receiver))
                return;

            foreach (var port in receiver.Outputs.Values.SelectMany(p => p))
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
                    Transform(component.GeneticScanner.Value).Coordinates.TryDistance(EntityManager, transform.Coordinates, out var dist);
                    component.GeneticScannerInRange = dist <= component.MaxDistance;
                    UpdateUserInterface(uid, component);
                }
            }
        }

        [SubscribeLocalEvent]
        private void OnPowerChanged(EntityUid uid, DnaModifierConsoleComponent component, ref PowerChangedEvent args)
            => UpdateUserInterface(uid, component);

        [SubscribeLocalEvent]
        private void OnNewLink(EntityUid uid, DnaModifierConsoleComponent component, NewLinkEvent args)
        {
            if (TryComp<MedicalScannerComponent>(args.Sink, out var scanner) && args.SourcePort == DnaModifierConsoleComponent.ScannerPort)
            {
                component.GeneticScanner = args.Sink;
                scanner.ConnectedConsole = uid;
            }

            RecheckConnections(uid, component.GeneticScanner, component);
        }

        [SubscribeLocalEvent]
        private void OnPortDisconnected(EntityUid uid, DnaModifierConsoleComponent component, SinkPortDisconnectedEvent args)
        {
            if (args.Port == DnaModifierConsoleComponent.ScannerPort)
                component.GeneticScanner = null;

            UpdateUserInterface(uid, component);
        }

        [SubscribeLocalEvent]
        private void OnUIOpen(EntityUid uid, DnaModifierConsoleComponent component, AfterActivatableUIOpenEvent args)
            => UpdateUserInterface(uid, component);

        [SubscribeLocalEvent]
        private void OnAnchorChanged(EntityUid uid, DnaModifierConsoleComponent component, ref AnchorStateChangedEvent args)
        {
            if (args.Anchored)
            {
                RecheckConnections(uid, component.GeneticScanner, component);
                return;
            }

            UpdateUserInterface(uid, component);
        }

        public void UpdateUserInterface(EntityUid consoleUid, DnaModifierConsoleComponent component)
        {
            if (!_uiSystem.HasUi(consoleUid, DnaModifierUiKey.Key))
                return;

            if (!_powerReceiverSystem.IsPowered(consoleUid))
            {
                _uiSystem.CloseUis(consoleUid);
                return;
            }

            _uiSystem.SetUiState(consoleUid, DnaModifierUiKey.Key, GetUserInterfaceState((consoleUid, component)));
        }

        public void RecheckConnections(EntityUid console, EntityUid? scanner, DnaModifierConsoleComponent? comp = null)
        {
            if (!Resolve(console, ref comp))
                return;

            if (scanner != null)
            {
                Transform(scanner.Value).Coordinates.TryDistance(EntityManager, Transform(console).Coordinates, out float d);
                comp.GeneticScannerInRange = d <= comp.MaxDistance;
            }

            UpdateUserInterface(console, comp);
        }

        private DnaModifierBoundUserInterfaceState GetUserInterfaceState(Entity<DnaModifierConsoleComponent> ent)
        {
            EntityUid? inputContainer = null;
            NetEntity console = GetNetEntity(ent);
            string scanBodyInfo = string.Empty, scannerBodyStatus = string.Empty, scannerBodyDna = string.Empty;
            float scannerBodyHealth = -1, scannerBodyRadiation = 0;
            bool hasDisk = false, scannerHasBeaker = false, scannerInRange = ent.Comp.GeneticScannerInRange;
            EnzymeInfo? enzyme = null;
            UniqueIdentifiersData? unique = null;
            List<EnzymesPrototypeInfo>? enzymes = null;
            var currentTime = _timing.CurTime;
            var injectorCd = ent.Comp.LastInjectorTime + ent.Comp.InjectorCooldown;
            var subjectInjectCd = ent.Comp.LastSubjectInjectTime + ent.Comp.SubjectInjectCooldown;
            var buffers = GetAllBuffers(ent);

            if (ent.Comp.GeneticScanner != null && TryComp<MedicalScannerComponent>(ent.Comp.GeneticScanner, out var scanner))
            {
                var body = scanner.BodyContainer.ContainedEntity;
                inputContainer = _itemSlotsSystem.GetItemOrNull(ent.Comp.GeneticScanner.Value, SharedDnaModifier.InputSlotName);

                if (_itemSlotsSystem.TryGetSlot(ent.Owner, SharedDnaModifier.DiskSlotName, out var diskSlot)
                    && diskSlot.HasItem && diskSlot.Item != null)
                {
                    hasDisk = true;
                    if (_dnaModifier.TryGetDataFromDisk(diskSlot.Item.Value, out var data))
                        enzyme = data;
                }

                if (_itemSlotsSystem.TryGetSlot(ent.Comp.GeneticScanner.Value, SharedDnaModifier.InputSlotName, out var beaker)
                    && beaker.HasItem)
                    scannerHasBeaker = true;

                if (body != null && TryComp<MobStateComponent>(body, out var mobState))
                {
                    scanBodyInfo = MetaData(body.Value).EntityName;
                    scannerBodyStatus = mobState.CurrentState != MobState.Invalid
                        ? GetStatus(mobState.CurrentState)
                        : Loc.GetString("dna-modifier-entity-unknown-text");

                    if (TryComp<DnaComponent>(body.Value, out var dna))
                        scannerBodyDna = dna.DNA ?? string.Empty;

                    if (TryComp<DamageableComponent>(body.Value, out var damage))
                    {
                        if (TryComp<MobThresholdsComponent>(body.Value, out var th))
                        {
                            var deathHealth = FixedPoint2.Zero;
                            foreach (var t in th.Thresholds)
                            {
                                if (t.Value == MobState.Dead)
                                {
                                    deathHealth = t.Key;
                                    break;
                                }
                            }

                            if (deathHealth > FixedPoint2.Zero)
                            {
                                var total = _damage.GetTotalDamage(body.Value);
                                scannerBodyHealth = Math.Clamp(1.0f - (total.Float() / deathHealth.Float()), 0f, 1f);
                            }
                        }

                        var pos = _damage.GetPositiveDamage((body.Value, damage));
                        if (pos.DamageDict.TryGetValue(RadDamage, out var rad))
                            scannerBodyRadiation = Math.Clamp(rad.Float() / 200f, 0f, 1f);
                    }

                    if (TryComp<DnaModifierComponent>(body.Value, out var mod))
                    {
                        unique = mod.UniqueIdentifiers;
                        enzymes = mod.EnzymesPrototypes;
                    }
                }
            }

            return new DnaModifierBoundUserInterfaceState(
                console, unique, enzymes, enzyme, scanBodyInfo, scannerBodyStatus, scannerBodyDna,
                scannerBodyHealth, scannerBodyRadiation, scannerHasBeaker,
                BuildInputContainerInfo(inputContainer), scannerInRange, hasDisk, buffers,
                currentTime < injectorCd ? injectorCd - currentTime : TimeSpan.Zero,
                currentTime < subjectInjectCd ? subjectInjectCd - currentTime : TimeSpan.Zero);
        }

        private string GetStatus(MobState mobState) => mobState switch
        {
            MobState.Alive => Loc.GetString("dna-modifier-entity-alive-text"),
            MobState.PreCritical or MobState.Critical => Loc.GetString("dna-modifier-entity-critical-text"),
            MobState.Dead => Loc.GetString("dna-modifier-entity-dead-text"),
            _ => Loc.GetString("dna-modifier-entity-unknown-text"),
        };

        private ContainerInfo? BuildInputContainerInfo(EntityUid? container)
        {
            if (container is not { Valid: true })
                return null;

            if (!TryComp(container, out FitsInDispenserComponent? fits)
                || !_solutionContainerSystem.TryGetSolution(container.Value, fits.Solution, out _, out var solution))
                return null;

            return new ContainerInfo(Name(container.Value), solution.Volume, solution.MaxVolume) { Reagents = solution.Contents };
        }

        public Dictionary<int, EnzymeInfo?> GetAllBuffers(EntityUid uid)
        {
            var buffers = new Dictionary<int, EnzymeInfo?>();
            if (!TryComp<DnaClientComponent>(uid, out var client))
                return buffers;

            for (var i = 1; i <= 3; i++)
            {
                if (_dnaClient.TryGetBufferData((uid, client), i, out var data))
                    buffers[i] = data;
            }

            return buffers;
        }
        #endregion

        #region Console logic
        private bool CanInteract(EntityUid console, EntityUid actor)
            => actor.IsValid() && _interaction.InRangeUnobstructed(actor, console);

        private void PlayClickSound(EntityUid console, DnaModifierConsoleComponent c)
            => _audio.PlayPvs(c.ClickSound, console, AudioParams.Default.WithVolume(-2f));

        [SubscribeLocalEvent]
        private void OnEjectPressed(EntityUid uid, DnaModifierConsoleComponent console, DnaModifierConsoleEjectEvent args)
        {
            if (!CanInteract(uid, args.Actor))
                return;

            if (console.GeneticScanner == null || !HasComp<MedicalScannerComponent>(console.GeneticScanner))
                return;

            if (_container.TryGetContainer(console.GeneticScanner.Value, SharedDnaModifier.OccupantSlotName, out var c))
                _container.EmptyContainer(c);

            PlayClickSound(uid, console);
            UpdateUserInterface(uid, console);
        }

        [SubscribeLocalEvent]
        private void OnEjectRejuvePressed(EntityUid uid, DnaModifierConsoleComponent console, DnaModifierConsoleEjectRejuveEvent args)
        {
            if (!CanInteract(uid, args.Actor))
                return;

            if (console.GeneticScanner == null || !HasComp<MedicalScannerComponent>(console.GeneticScanner))
                return;

            if (_itemSlotsSystem.TryGetSlot(console.GeneticScanner.Value, SharedDnaModifier.InputSlotName, out var slot))
                _itemSlotsSystem.TryEject(console.GeneticScanner.Value, slot, null, out _, true);

            PlayClickSound(uid, console);
            UpdateUserInterface(uid, console);
        }

        [SubscribeLocalEvent]
        private void OnReagentButtonPressed(EntityUid uid, DnaModifierConsoleComponent console, DnaModifierConsoleReagentButtonEvent args)
        {
            if (!CanInteract(uid, args.Actor))
                return;

            if (console.GeneticScanner == null)
                return;

            if (!TryComp<MedicalScannerComponent>(console.GeneticScanner, out var scanner)
                || scanner.BodyContainer.ContainedEntity == null
                || !_itemSlotsSystem.TryGetSlot(console.GeneticScanner.Value, SharedDnaModifier.InputSlotName, out var slot)
                || slot.Item == null)
                return;

            if (!_solutionContainerSystem.TryGetSolution(slot.Item.Value, SharedDnaModifier.SolutionSlotName, out var srcSol, out var srcComp))
                return;

            var target = scanner.BodyContainer.ContainedEntity.Value;
            if (!_solutionContainerSystem.TryGetInjectableSolution(target, out var tgtSol, out _))
                return;

            var amount = args.Amount switch
            {
                DnaModifierReagentAmount.U1 => FixedPoint2.New(1),
                DnaModifierReagentAmount.U5 => FixedPoint2.New(5),
                DnaModifierReagentAmount.U10 => FixedPoint2.New(10),
                DnaModifierReagentAmount.U25 => FixedPoint2.New(25),
                DnaModifierReagentAmount.U50 => FixedPoint2.New(50),
                DnaModifierReagentAmount.U100 => FixedPoint2.New(100),
                DnaModifierReagentAmount.All => srcComp.GetReagentQuantity(args.ReagentId),
                _ => FixedPoint2.Zero
            };

            if (amount <= FixedPoint2.Zero || srcComp.GetReagentQuantity(args.ReagentId) < amount)
                return;

            var reagent = new Solution();
            reagent.AddReagent(args.ReagentId, amount);
            _solutionContainerSystem.RemoveReagent(srcSol.Value, args.ReagentId, amount);
            if (!_solutionContainerSystem.TryAddSolution(tgtSol.Value, reagent))
                return;

            PlayClickSound(uid, console);
            UpdateUserInterface(uid, console);
        }

        [SubscribeLocalEvent]
        private void OnSaveServerPressed(EntityUid uid, DnaModifierConsoleComponent console, DnaModifierConsoleSaveServerEvent args)
        {
            if (!CanInteract(uid, args.Actor))
                return;

            if (console.GeneticScanner == null || !TryComp<DnaClientComponent>(uid, out var client))
                return;

            if (!TryComp<MedicalScannerComponent>(console.GeneticScanner, out var scanner))
                return;

            var body = scanner.BodyContainer.ContainedEntity;
            if (!TryComp<DnaModifierComponent>(body, out var mod))
                return;

            EnzymeInfo? data = null;
            switch (args.CurrentType)
            {
                case 1 when mod.UniqueIdentifiers != null:
                    {
                        data = new EnzymeInfo { Identifier = _dnaModifier.CloneUniqueIdentifiers(mod.UniqueIdentifiers) };
                        break;
                    }
                case 2 when mod.UniqueIdentifiers != null && mod.EnzymesPrototypes != null:
                    {
                        data = new EnzymeInfo
                        {
                            Identifier = _dnaModifier.CloneUniqueIdentifiers(mod.UniqueIdentifiers),
                            Info = _dnaModifier.CloneEnzymesPrototypes(mod.EnzymesPrototypes)
                        };
                        break;
                    }
                case 3 when mod.EnzymesPrototypes != null:
                    {
                        data = new EnzymeInfo { Info = _dnaModifier.CloneEnzymesPrototypes(mod.EnzymesPrototypes) };
                        break;
                    }
            }

            if (data == null)
                return;

            _dnaClient.TryAddToBuffer((uid, client), args.CurrentSection, data);
            PlayClickSound(uid, console);
            UpdateUserInterface(uid, console);
        }

        [SubscribeLocalEvent]
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

        [SubscribeLocalEvent]
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

            var session = playerActor.PlayerSession;
            _quickDialog.OpenDialog(session, Loc.GetString("dna-modifier-button-rename"), "", (string name) =>
            {
                if (session.AttachedEntity is not { } user) return;
                if (!_interaction.InRangeUnobstructed(user, uid)) return;
                var finalName = string.IsNullOrWhiteSpace(name) ? data.SampleName : name;
                _dnaClient.TryRenameBuffer((uid, client), args.Index, finalName);
                PlayClickSound(uid, console);
                UpdateUserInterface(uid, console);
            });
        }

        [SubscribeLocalEvent]
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

            _dnaModifier.OnFillingInjector(Spawn(Injector, Transform(uid).Coordinates), data.Identifier, data.Info);
            console.LastInjectorTime = _timing.CurTime;
            PlayClickSound(uid, console);
            UpdateUserInterface(uid, console);
        }

        [SubscribeLocalEvent]
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
            if (targetBlock.EnzymesPrototypeId == null && targetBlock.Order == 0)
                return;

            var single = new List<EnzymesPrototypeInfo> { targetBlock };
            _dnaModifier.OnFillingInjector(Spawn(Injector, Transform(uid).Coordinates), null, single);
            console.LastInjectorTime = _timing.CurTime;
            PlayClickSound(uid, console);
            UpdateUserInterface(uid, console);
        }

        [SubscribeLocalEvent]
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

            var body = scanner.BodyContainer.ContainedEntity;
            if (!body.HasValue || !TryComp<DnaModifierComponent>(body, out var mod))
                return;

            if (!_dnaClient.TryGetBufferData((uid, client), args.Index, out var data))
                return;

            PlayClickSound(uid, console);
            _dnaModifier.ChangeDna((body.Value, mod), data);
            console.LastSubjectInjectTime = _timing.CurTime;
            _damage.TryChangeDamage(body.Value, new DamageSpecifier { DamageDict = { { RadDamage, 20 } } }, true);
            UpdateUserInterface(uid, console);
        }

        [SubscribeLocalEvent]
        private void OnExportOnDiskPressed(EntityUid uid, DnaModifierConsoleComponent console, DnaModifierConsoleExportOnDiskEvent args)
        {
            if (!CanInteract(uid, args.Actor))
                return;

            if (console.GeneticScanner == null || !TryComp<DnaClientComponent>(uid, out var client))
                return;

            if (!_dnaClient.TryGetBufferData((uid, client), args.Index, out var data))
                return;

            if (_itemSlotsSystem.TryGetSlot(uid, SharedDnaModifier.DiskSlotName, out var disk) && disk.Item != null)
            {
                _dnaModifier.TrySaveInDisk(disk.Item.Value, data);
                PlayClickSound(uid, console);
                UpdateUserInterface(uid, console);
            }
        }

        [SubscribeLocalEvent]
        private void OnExportFromDiskPressed(EntityUid uid, DnaModifierConsoleComponent console, DnaModifierConsoleExportFromDiskEvent args)
        {
            if (!CanInteract(uid, args.Actor))
                return;

            if (console.GeneticScanner == null || !TryComp<DnaClientComponent>(uid, out var client))
                return;

            if (_itemSlotsSystem.TryGetSlot(uid, SharedDnaModifier.DiskSlotName, out var disk) && disk.Item != null)
            {
                _dnaModifier.TryGetDataFromDisk(disk.Item.Value, out var data);
                if (data == null)
                    return;

                _dnaClient.TryAddToBufferDisk((uid, client), args.Index, data);
                PlayClickSound(uid, console);
                UpdateUserInterface(uid, console);
            }
        }

        [SubscribeLocalEvent]
        private void OnClearDiskPressed(EntityUid uid, DnaModifierConsoleComponent console, DnaModifierConsoleClearDiskEvent args)
        {
            if (!CanInteract(uid, args.Actor))
                return;

            if (console.GeneticScanner == null)
                return;

            if (_itemSlotsSystem.TryGetSlot(uid, SharedDnaModifier.DiskSlotName, out var disk) && disk.Item != null)
            {
                PlayClickSound(uid, console);
                _dnaModifier.TryClearDiskData(disk.Item.Value);
            }
        }

        [SubscribeLocalEvent]
        private void OnReleverationPressed(EntityUid uid, DnaModifierConsoleComponent console, DnaModifierConsoleReleverationEvent args)
        {
            if (!CanInteract(uid, args.Actor))
                return;

            if (console.GeneticScanner == null)
                return;

            if (!TryComp<MedicalScannerComponent>(console.GeneticScanner, out var scanner))
                return;

            var body = scanner.BodyContainer.ContainedEntity;
            if (!body.HasValue || !TryComp<DnaModifierComponent>(body, out var mod))
                return;

            var intensity = Math.Clamp(args.Intensity, MinRadiationValue, MaxRadiationValue);

            var species = "Human";
            if (TryComp<HumanoidProfileComponent>(body.Value, out var humanoid))
                species = humanoid.Species;

            if (args.CurrentTab == 0 && mod.UniqueIdentifiers != null)
                ModifyUniqueIdentifiers(mod.UniqueIdentifiers, species, args.CurrentBlock, args.CurrentValue, intensity);
            else if (args.CurrentTab == 1 && mod.EnzymesPrototypes != null)
                ModifyEnzymesPrototypes(mod.EnzymesPrototypes, args.CurrentBlock, args.CurrentValue, intensity);
            else return;

            AddRadiationDamage(body.Value, intensity);
            Dirty(body.Value, mod);

            _dnaModifier.ChangeDna((body.Value, mod), args.CurrentTab);
            PlayClickSound(uid, console);
            UpdateUserInterface(uid, console);
        }

        [SubscribeLocalEvent]
        private void OnReleverationsPressed(EntityUid uid, DnaModifierConsoleComponent console, DnaModifierConsoleReleverationsEvent args)
        {
            if (!CanInteract(uid, args.Actor))
                return;

            if (console.GeneticScanner == null)
                return;

            if (!TryComp<MedicalScannerComponent>(console.GeneticScanner, out var scanner))
                return;

            var body = scanner.BodyContainer.ContainedEntity;
            if (!body.HasValue || !TryComp<DnaModifierComponent>(body, out var mod))
                return;

            var intensity = Math.Clamp(args.Intensity, MinRadiationValue, MaxRadiationValue);
            var duration = Math.Clamp(args.Duration, MinRadiationValue, MaxRadiationValue);

            var species = "Human";
            if (TryComp<HumanoidProfileComponent>(body.Value, out var humanoid))
                species = humanoid.Species;

            int type;
            if (args.CurrentTab == 0 && mod.UniqueIdentifiers != null)
            {
                type = 0;
                ModifyUniqueIdentifiers(mod.UniqueIdentifiers, species, intensity, duration);
            }
            else if (args.CurrentTab == 1 && mod.EnzymesPrototypes != null)
            {
                type = 1;
                ModifyEnzymesPrototypes(mod.EnzymesPrototypes, intensity, duration);
            }
            else return;

            AddRadiationDamage(body.Value, intensity);
            Dirty(body.Value, mod);

            _dnaModifier.ChangeDna((body.Value, mod), type);
            PlayClickSound(uid, console);
            UpdateUserInterface(uid, console);
        }

        private void ModifyUniqueIdentifiers(UniqueIdentifiersData u, string species, string block, int value, float intensity)
        {
            if (_random.NextFloat() < 0.025f)
            {
                var actions = new List<Action>
                {
                    () => u.HairColor = MutateColorArray(u.HairColor, intensity),
                    () => u.BeardColor = MutateColorArray(u.BeardColor, intensity),
                    () => u.SkinTone = _dnaModifier.MutateSkinToneDigit(u.SkinTone, 0, intensity),
                    () => u.FurColor = _dnaModifier.MutateColorDigit(u.FurColor, _random.Next(0, 6), intensity),
                    () => u.EyeColor = _dnaModifier.MutateColorDigit(u.EyeColor, _random.Next(0, 6), intensity),
                    () => u.HeadAccessoryColor = MutateColorArray(u.HeadAccessoryColor, intensity),
                    () => u.HeadMarkingColor = MutateColorArray(u.HeadMarkingColor, intensity),
                    () => u.BodyMarkingColor = MutateColorArray(u.BodyMarkingColor, intensity),
                    () => u.TailMarkingColor = MutateColorArray(u.TailMarkingColor, intensity),
                    () => u.TailOverlayColor = MutateColorArray(u.TailOverlayColor, intensity),
                    () => u.SnoutColor = MutateColorArray(u.SnoutColor, intensity),
                    () => u.SnoutCoverColor = MutateColorArray(u.SnoutCoverColor, intensity),
                    () => u.HeadSideColor = MutateColorArray(u.HeadSideColor, intensity),
                    () => u.Sex = (Sex)_random.Next(0, 3)
                };
                _random.Pick(actions)();
                return;
            }

            var parts = block.Split(':');
            if (!int.TryParse(parts[0], out var blockNumber))
                return;

            var layer = parts.Length > 1 && int.TryParse(parts[1], out var l) ? l : 0;

            switch (blockNumber)
            {
                case 1:  u.HairColor = MutateColorArrayAt(u.HairColor, layer, intensity); break;
                case 2:  u.BeardColor = MutateColorArrayAt(u.BeardColor, layer, intensity); break;
                case 3:  u.SkinTone = _dnaModifier.MutateSkinToneDigit(u.SkinTone, 0, intensity); break;
                case 4:  u.FurColor = _dnaModifier.MutateColorDigit(u.FurColor, value, intensity); break;
                case 5:  u.EyeColor = _dnaModifier.MutateColorDigit(u.EyeColor, value, intensity); break;
                case 6:  u.HeadAccessoryColor = MutateColorArrayAt(u.HeadAccessoryColor, layer, intensity); break;
                case 7:  u.HeadMarkingColor = MutateColorArrayAt(u.HeadMarkingColor, layer, intensity); break;
                case 8:  u.BodyMarkingColor = MutateColorArrayAt(u.BodyMarkingColor, layer, intensity); break;
                case 9:  u.TailMarkingColor = MutateColorArrayAt(u.TailMarkingColor, layer, intensity); break;
                case 10: u.TailOverlayColor = MutateColorArrayAt(u.TailOverlayColor, layer, intensity); break;
                case 11: u.SnoutColor = MutateColorArrayAt(u.SnoutColor, layer, intensity); break;
                case 12: u.SnoutCoverColor = MutateColorArrayAt(u.SnoutCoverColor, layer, intensity); break;
                case 13: u.HeadSideColor = MutateColorArrayAt(u.HeadSideColor, layer, intensity); break;
                case 20: u.HairStyle = _dnaModifier.GetRandomMarkingForLayer(HumanoidVisualLayers.Hair, species); break;
                case 21: u.BeardStyle = _dnaModifier.GetRandomMarkingForLayer(HumanoidVisualLayers.FacialHair, species); break;
                case 22: u.HeadAccessoryStyle = _dnaModifier.GetRandomMarkingForLayer(HumanoidVisualLayers.HeadTop, species); break;
                case 23: u.HeadMarkingStyle = _dnaModifier.GetRandomMarkingForLayer(HumanoidVisualLayers.Head, species); break;
                case 24: u.BodyMarkingStyle = _dnaModifier.GetRandomMarkingForLayer(HumanoidVisualLayers.Chest, species); break;
                case 25: u.TailMarkingStyle = _dnaModifier.GetRandomMarkingForLayer(HumanoidVisualLayers.Tail, species); break;
                case 26: u.TailOverlayStyle = _dnaModifier.GetRandomMarkingForLayer(HumanoidVisualLayers.TailOverlay, species); break;
                case 27: u.SnoutStyle = _dnaModifier.GetRandomMarkingForLayer(HumanoidVisualLayers.Snout, species); break;
                case 28: u.SnoutCoverStyle = _dnaModifier.GetRandomMarkingForLayer(HumanoidVisualLayers.SnoutCover, species); break;
                case 29: u.HeadSideStyle = _dnaModifier.GetRandomMarkingForLayer(HumanoidVisualLayers.HeadSide, species); break;
                case 30: u.Sex = (Sex)_random.Next(0, 3); break;
            }
        }

        private void ModifyUniqueIdentifiers(UniqueIdentifiersData u, string species, float intensity, float duration)
        {
            var actions = new List<Action>
            {
                () => u.HairColor = MutateColorArray(u.HairColor, intensity * duration),
                () => u.BeardColor = MutateColorArray(u.BeardColor, intensity * duration),
                () => u.SkinTone = _dnaModifier.MutateSkinToneDigit(u.SkinTone, 0, intensity * duration),
                () => u.FurColor = _dnaModifier.MutateColorDigit(u.FurColor, _random.Next(0, 6), intensity * duration),
                () => u.EyeColor = _dnaModifier.MutateColorDigit(u.EyeColor, _random.Next(0, 6), intensity * duration),
                () => u.HeadAccessoryColor = MutateColorArray(u.HeadAccessoryColor, intensity * duration),
                () => u.HeadMarkingColor = MutateColorArray(u.HeadMarkingColor, intensity * duration),
                () => u.BodyMarkingColor = MutateColorArray(u.BodyMarkingColor, intensity * duration),
                () => u.TailMarkingColor = MutateColorArray(u.TailMarkingColor, intensity * duration),
                () => u.TailOverlayColor = MutateColorArray(u.TailOverlayColor, intensity * duration),
                () => u.SnoutColor = MutateColorArray(u.SnoutColor, intensity * duration),
                () => u.SnoutCoverColor = MutateColorArray(u.SnoutCoverColor, intensity * duration),
                () => u.HeadSideColor = MutateColorArray(u.HeadSideColor, intensity * duration),
                () => u.Sex = (Sex)_random.Next(0, 3)
            };

            var count = Math.Clamp((int)intensity, 1, 3);
            for (var i = 0; i < count; i++)
                _random.Pick(actions)();
        }

        private uint[] MutateColorArray(uint[] colors, float intensity)
        {
            if (colors.Length == 0)
                return new[] { _dnaModifier.GenerateRandomColor() };

            var idx = _random.Next(colors.Length);
            colors[idx] = _dnaModifier.MutateColorDigit(colors[idx], _random.Next(0, 6), intensity);
            return colors;
        }

        private uint[] MutateColorArrayAt(uint[] colors, int layer, float intensity)
        {
            if (layer >= colors.Length)
            {
                var extended = new uint[layer + 1];
                for (var i = 0; i < colors.Length; i++)
                    extended[i] = colors[i];

                var last = colors.Length > 0 ? colors[^1] : 0xFFFFFF;
                for (var i = colors.Length; i < extended.Length; i++)
                    extended[i] = last;

                colors = extended;
            }

            colors[layer] = _dnaModifier.MutateColorDigit(colors[layer], _random.Next(0, 6), intensity);
            return colors;
        }

        private void ModifyEnzymesPrototypes(List<EnzymesPrototypeInfo> list, string block, int value, float intensity)
        {
            if (_random.NextFloat() < 0.025f)
            {
                var idx = _random.Next(list.Count);
                var e = list[idx];
                list[idx] = e with { HexCode = _dnaModifier.MutateHexDigit(e.HexCode, _random.Next(0, 3), intensity) };
                return;
            }

            if (!int.TryParse(block, out var blockNumber))
                return;

            var blockIndex = blockNumber - 1;
            if (blockIndex < 0 || blockIndex >= list.Count)
                return;

            var target = list[blockIndex];
            list[blockIndex] = target with { HexCode = _dnaModifier.MutateHexDigit(target.HexCode, value, intensity) };
        }

        private void ModifyEnzymesPrototypes(List<EnzymesPrototypeInfo> list, float intensity, float duration)
        {
            var count = Math.Clamp((int)intensity, 1, 2);
            for (var i = 0; i < count; i++)
            {
                var idx = _random.Next(list.Count);
                var e = list[idx];
                var hex = e.HexCode;
                for (var j = 0; j < 3; j++)
                {
                    hex = _dnaModifier.MutateHexDigit(hex, j, intensity * duration);
                }

                list[idx] = e with { HexCode = hex };
            }
        }

        private void AddRadiationDamage(EntityUid uid, float intensity)
        {
            var mult = 1.5f + _random.NextFloat() * 1.5f;
            _damage.TryChangeDamage(uid, new DamageSpecifier { DamageDict = { { RadDamage, mult * intensity } } }, true, origin: uid);
        }
        #endregion
    }
}
