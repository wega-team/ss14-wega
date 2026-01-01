using Content.Server.Disease.Components;
using Content.Server.Nutrition.Components;
using Content.Server.Popups;
using Content.Server.Power.EntitySystems;
using Content.Shared.Power;
using Content.Server.Station.Systems;
using Content.Shared.Paper;
using Content.Shared.Disease;
using Content.Shared.Disease.Components;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Swab;
using Content.Shared.Tools.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Content.Shared.Nutrition.Components;

namespace Content.Server.Disease
{
    /// <summary>
    /// Everything that's about disease diangosis and machines is in here
    /// </summary>
    public sealed class DiseaseDiagnosisSystem : EntitySystem
    {
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly MetaDataSystem _metaData = default!;
        [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly InventorySystem _inventorySystem = default!;
        [Dependency] private readonly PaperSystem _paperSystem = default!;
        [Dependency] private readonly StationSystem _stationSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<DiseaseSwabComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<DiseaseSwabComponent, ExaminedEvent>(OnExamined);
            SubscribeLocalEvent<DiseaseDiagnoserComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
            SubscribeLocalEvent<DiseaseVaccineCreatorComponent, AfterInteractUsingEvent>(OnAfterInteractUsingVaccine);
            // Visuals
            SubscribeLocalEvent<DiseaseMachineComponent, PowerChangedEvent>(OnPowerChanged);
            // Private Events
            SubscribeLocalEvent<DiseaseDiagnoserComponent, DiseaseMachineFinishedEvent>(OnDiagnoserFinished);
            SubscribeLocalEvent<DiseaseVaccineCreatorComponent, DiseaseMachineFinishedEvent>(OnVaccinatorFinished);
            SubscribeLocalEvent<DiseaseSwabComponent, DiseaseSwabDoAfterEvent>(OnSwabDoAfter);
        }

        private Queue<EntityUid> _addQueue = new();
        private Queue<EntityUid> _removeQueue = new();

        /// <summary>
        /// This handles running disease machines
        /// to handle their delay and visuals.
        /// </summary>
        public override void Update(float frameTime)
        {
            foreach (var uid in _addQueue)
            {
                EnsureComp<DiseaseMachineRunningComponent>(uid);
            }

            _addQueue.Clear();
            foreach (var uid in _removeQueue)
            {
                RemComp<DiseaseMachineRunningComponent>(uid);
            }

            _removeQueue.Clear();

            var query = EntityQueryEnumerator<DiseaseMachineRunningComponent, DiseaseMachineComponent>();
            while (query.MoveNext(out var uid, out _, out var diseaseMachine))
            {
                diseaseMachine.Accumulator += frameTime;

                while (diseaseMachine.Accumulator >= diseaseMachine.Delay)
                {
                    diseaseMachine.Accumulator -= diseaseMachine.Delay;
                    var ev = new DiseaseMachineFinishedEvent(diseaseMachine);
                    RaiseLocalEvent(uid, ev);
                    _removeQueue.Enqueue(uid);
                }
            }
        }

        ///
        /// Event Handlers
        ///

        /// <summary>
        /// This handles using swabs on other people
        /// and checks that the swab isn't already used
        /// and the other person's mouth is accessible
        /// and then adds a random disease from that person
        /// to the swab if they have any
        /// </summary>
        private void OnAfterInteract(EntityUid uid, DiseaseSwabComponent swab, AfterInteractEvent args)
        {
            if (args.Target is not { } target || !args.CanReach || !HasComp<DiseaseCarrierComponent>(target))
                return;

            if (swab.Used)
            {
                _popupSystem.PopupEntity(Loc.GetString("swab-already-used"), args.User, args.User);
                return;
            }

            if (_inventorySystem.TryGetSlotEntity(args.Target.Value, "mask", out var maskUid) &&
                EntityManager.TryGetComponent<IngestionBlockerComponent>(maskUid, out var blocker) &&
                blocker.Enabled)
            {
                _popupSystem.PopupEntity(Loc.GetString("swab-mask-blocked", ("target", Identity.Entity(args.Target.Value, EntityManager)), ("mask", maskUid)), args.User, args.User);
                return;
            }

            _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, swab.SwabDelay, new DiseaseSwabDoAfterEvent(), uid, target: args.Target, used: uid)
            {
                Broadcast = true,
                BreakOnMove = true,
                NeedHand = true,
            });
        }

        /// <summary>
        /// This handles the disease diagnoser machine up
        /// until it's turned on. It has some slight
        /// differences in checks from the vaccinator.
        /// </summary>
        private void OnAfterInteractUsing(EntityUid uid, DiseaseDiagnoserComponent component, AfterInteractUsingEvent args)
        {
            if (args.Handled || !args.CanReach)
                return;

            var machine = Comp<DiseaseMachineComponent>(uid);
            if (HasComp<DiseaseMachineRunningComponent>(uid) || !this.IsPowered(uid, EntityManager))
                return;

            if (!HasComp<HandsComponent>(args.User) || HasComp<ToolComponent>(args.Used)) // Don't want to accidentally breach wrenching or whatever
                return;

            if (!TryComp<DiseaseSwabComponent>(args.Used, out var swab) || swab is null)
            {
                _popupSystem.PopupEntity(Loc.GetString("diagnoser-cant-use-swab", ("machine", uid), ("swab", args.Used)), uid, args.User);
                return;
            }
            _popupSystem.PopupEntity(Loc.GetString("machine-insert-item", ("machine", uid), ("item", args.Used), ("user", args.User)), uid, args.User);

            machine.Disease = swab.Disease;

            _addQueue.Enqueue(uid);
            UpdateAppearance(uid, true, true);
            _audio.PlayPvs(new SoundPathSpecifier("/Audio/Machines/diagnoser_printing.ogg"), uid, AudioParams.Default.WithLoop(false));
            EntityManager.DeleteEntity(args.Used);
        }

        /// <summary>
        /// This handles the vaccinator machine up
        /// until it's turned on. It has some slight
        /// differences in checks from the diagnoser.
        /// </summary>
        private void OnAfterInteractUsingVaccine(EntityUid uid, DiseaseVaccineCreatorComponent component, AfterInteractUsingEvent args)
        {
            if (args.Handled || !args.CanReach)
                return;

            if (HasComp<DiseaseMachineRunningComponent>(uid) || !this.IsPowered(uid, EntityManager))
                return;

            if (!HasComp<HandsComponent>(args.User) || HasComp<ToolComponent>(args.Used)) //This check ensures tools don't break without yaml ordering jank
                return;

            if (!TryComp<DiseaseSwabComponent>(args.Used, out var swab) || swab.Disease == null || !swab.Disease.Infectious)
            {
                _popupSystem.PopupEntity(Loc.GetString("diagnoser-cant-use-swab", ("machine", uid), ("swab", args.Used)), uid, args.User);
                return;
            }
            _popupSystem.PopupEntity(Loc.GetString("machine-insert-item", ("machine", uid), ("item", args.Used), ("user", args.User)), uid, args.User);
            var machine = Comp<DiseaseMachineComponent>(uid);
            machine.Disease = swab.Disease;

            _addQueue.Enqueue(uid);
            UpdateAppearance(uid, true, true);
            _audio.PlayPvs(new SoundPathSpecifier("/Audio/Machines/vaccinator_running.ogg"), uid, AudioParams.Default.WithLoop(false));
            EntityManager.DeleteEntity(args.Used);
        }

        /// <summary>
        /// This handles swab examination text
        /// so you can tell if they are used or not.
        /// </summary>
        private void OnExamined(EntityUid uid, DiseaseSwabComponent swab, ExaminedEvent args)
        {
            if (args.IsInDetailsRange)
            {
                if (swab.Used)
                    args.PushMarkup(Loc.GetString("swab-used"));
                else
                    args.PushMarkup(Loc.GetString("swab-unused"));
            }
        }

        ///
        /// Helper functions
        ///

        /// <summary>
        /// This assembles a disease report
        /// With its basic details and
        /// specific cures (i.e. not spaceacillin).
        /// The cure resist field tells you how
        /// effective spaceacillin etc will be.
        /// </summary>
        private FormattedMessage AssembleDiseaseReport(DiseasePrototype disease)
        {
            FormattedMessage report = new();
            var diseaseName = Loc.GetString(disease.Name);
            report.TryAddMarkup(Loc.GetString("diagnoser-disease-report-name", ("disease", diseaseName)), out _);
            report.PushNewline();

            if (disease.Infectious)
            {
                report.TryAddMarkup(Loc.GetString("diagnoser-disease-report-infectious"), out _);
                report.PushNewline();
            }
            else
            {
                report.TryAddMarkup(Loc.GetString("diagnoser-disease-report-not-infectious"), out _);
                report.PushNewline();
            }

            string cureResistLine = string.Empty;
            cureResistLine += disease.CureResist switch
            {
                < 0f => Loc.GetString("diagnoser-disease-report-cureresist-none"),
                <= 0.05f => Loc.GetString("diagnoser-disease-report-cureresist-low"),
                <= 0.14f => Loc.GetString("diagnoser-disease-report-cureresist-medium"),
                _ => Loc.GetString("diagnoser-disease-report-cureresist-high")
            };
            report.TryAddMarkup(cureResistLine, out _);
            report.PushNewline();

            // Add Cures
            if (disease.Cures.Count == 0)
            {
                report.TryAddMarkup(Loc.GetString("diagnoser-no-cures"), out _);
            }
            else
            {
                report.PushNewline();
                report.TryAddMarkup(Loc.GetString("diagnoser-cure-has"), out _);
                report.PushNewline();

                foreach (var cure in disease.Cures)
                {
                    report.TryAddMarkup(cure.CureText(), out _);
                    report.PushNewline();
                }
            }

            return report;
        }

        public bool ServerHasDisease(DiseaseServerComponent server, DiseasePrototype disease)
        {
            bool has = false;
            foreach (var serverDisease in server.Diseases)
            {
                if (serverDisease.ID == disease.ID)
                    has = true;
            }
            return has;
        }
        ///
        /// Appearance stuff
        ///

        /// <summary>
        /// Appearance helper function to
        /// set the component's power and running states.
        /// </summary>
        private void UpdateAppearance(EntityUid uid, bool isOn, bool isRunning)
        {
            if (!TryComp<AppearanceComponent>(uid, out var appearance))
                return;

            _appearance.SetData(uid, DiseaseMachineVisuals.IsOn, isOn, appearance);
            _appearance.SetData(uid, DiseaseMachineVisuals.IsRunning, isRunning, appearance);
        }
        /// <summary>
        /// Makes sure the machine is visually off/on.
        /// </summary>
        private void OnPowerChanged(EntityUid uid, DiseaseMachineComponent component, ref PowerChangedEvent args)
        {
            UpdateAppearance(uid, args.Powered, false);
        }

        /// <summary>
        /// Copies a disease prototype to the swab
        /// after the doafter completes.
        /// </summary>
        private void OnSwabDoAfter(EntityUid uid, DiseaseSwabComponent component, DoAfterEvent args)
        {
            if (args.Handled || args.Cancelled || !TryComp<DiseaseCarrierComponent>(args.Args.Target, out var carrier) || !TryComp<DiseaseSwabComponent>(args.Args.Used, out var swab))
                return;

            swab.Used = true;
            _popupSystem.PopupEntity(Loc.GetString("swab-swabbed", ("target", Identity.Entity(args.Args.Target.Value, EntityManager))), args.Args.Target.Value, args.Args.User);

            if (swab.Disease != null || carrier.Diseases.Count == 0)
                return;

            swab.Disease = _random.Pick(carrier.Diseases);
            args.Handled = true;
        }

        /// <summary>
        /// Prints a diagnostic report with its findings.
        /// Also cancels the animation.
        /// </summary>
        private void OnDiagnoserFinished(EntityUid uid, DiseaseDiagnoserComponent component, DiseaseMachineFinishedEvent args)
        {
            var isPowered = this.IsPowered(uid, EntityManager);
            UpdateAppearance(uid, isPowered, false);
            // spawn a piece of paper.
            var printed = Spawn(args.Machine.MachineOutput, Transform(uid).Coordinates);

            if (!TryComp<PaperComponent>(printed, out var paper))
                return;

            string reportTitle;
            FormattedMessage contents = new();
            if (args.Machine.Disease != null)
            {
                var diseaseName = Loc.GetString(args.Machine.Disease.Name);
                reportTitle = Loc.GetString("diagnoser-disease-report", ("disease", diseaseName));
                contents = AssembleDiseaseReport(args.Machine.Disease);

                var known = false;

                var serverQuery = EntityQueryEnumerator<DiseaseServerComponent>();
                var currentStation = _stationSystem.GetOwningStation(uid);

                while (serverQuery.MoveNext(out var serverUid, out var server))
                {
                    if (_stationSystem.GetOwningStation(serverUid) != currentStation)
                        continue;

                    if (ServerHasDisease(server, args.Machine.Disease))
                    {
                        known = true;
                    }
                    else
                    {
                        server.Diseases.Add(args.Machine.Disease);
                    }
                }

                if (!known)
                {
                    known = true;
                    Spawn("ResearchDisk5000", Transform(uid).Coordinates);
                }
            }
            else
            {
                reportTitle = Loc.GetString("diagnoser-disease-report-none");
                contents.AddMarkupOrThrow(Loc.GetString("diagnoser-disease-report-none-contents"));
            }

            _metaData.SetEntityName(printed, reportTitle);
            _paperSystem.SetContent((printed, paper), contents.ToMarkup());
        }

        /// <summary>
        /// Prints a vaccine that will vaccinate
        /// against the disease on the inserted swab.
        /// </summary>
        private void OnVaccinatorFinished(EntityUid uid, DiseaseVaccineCreatorComponent component, DiseaseMachineFinishedEvent args)
        {
            UpdateAppearance(uid, this.IsPowered(uid, EntityManager), false);

            // spawn a vaccine
            var vaxx = Spawn(args.Machine.MachineOutput, Transform(uid).Coordinates);

            if (!TryComp<DiseaseVaccineComponent>(vaxx, out var vaxxComp))
                return;

            vaxxComp.Disease = args.Machine.Disease;
        }

        /// <summary>
        /// Fires when a disease machine is done
        /// with its production delay and ready to
        /// create a report or vaccine
        /// </summary>
        private sealed class DiseaseMachineFinishedEvent : EntityEventArgs
        {
            public DiseaseMachineComponent Machine { get; }
            public DiseaseMachineFinishedEvent(DiseaseMachineComponent machine)
            {
                Machine = machine;
            }
        }
    }
}
