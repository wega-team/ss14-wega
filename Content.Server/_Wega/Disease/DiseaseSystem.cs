using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Shared.Body.Events;
using Content.Shared.Chat;
using Content.Shared.Clothing.Components;
using Content.Shared.Disease;
using Content.Shared.Disease.Components;
using Content.Shared.Disease.Events;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Rejuvenate;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Disease
{

    /// <summary>
    /// Handles disease propagation & curing
    /// </summary>
    public sealed class DiseaseSystem : SharedDiseaseSystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
        [Dependency] private readonly InventorySystem _inventorySystem = default!;
        [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
        [Dependency] private readonly ChatSystem _chatSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DiseaseCarrierComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<DiseaseCarrierComponent, CureDiseaseAttemptEvent>(OnTryCureDisease);
            SubscribeLocalEvent<DiseaseCarrierComponent, RejuvenateEvent>(OnRejuvenate);
            SubscribeLocalEvent<DiseasedComponent, ContactInteractionEvent>(OnContactInteraction);
            SubscribeLocalEvent<DiseasedComponent, EntitySpokeEvent>(OnEntitySpeak);
            SubscribeLocalEvent<DiseaseProtectionComponent, GotEquippedEvent>(OnEquipped);
            SubscribeLocalEvent<DiseaseProtectionComponent, GotUnequippedEvent>(OnUnequipped);
            SubscribeLocalEvent<DiseaseVaccineComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<DiseaseVaccineComponent, ExaminedEvent>(OnExamined);
            // Handling stuff from other systems
            SubscribeLocalEvent<DiseaseCarrierComponent, ApplyMetabolicMultiplierEvent>(OnApplyMetabolicMultiplier);
            // Private events stuff
            SubscribeLocalEvent<DiseaseVaccineComponent, VaccineDoAfterEvent>(OnDoAfter);
        }

        private Queue<(EntityUid uid, DiseaseCarrierComponent carrier, DiseasePrototype disease)> _cureQueue = new();

        /// <summary>
        /// First, adds or removes diseased component from the queues and clears them.
        /// Then, iterates over every diseased component to check for their effects
        /// and cures
        /// </summary>
        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var entity in AddQueue)
            {
                EnsureComp<DiseasedComponent>(entity);
            }

            AddQueue.Clear();

            foreach (var tuple in _cureQueue)
            {
                if (tuple.carrier.Diseases.Count == 1) //This is reliable unlike testing Count == 0 right after removal for reasons I don't quite get
                    RemComp<DiseasedComponent>(tuple.uid);
                tuple.carrier.PastDiseases.Add(tuple.disease);
                tuple.carrier.Diseases.Remove(tuple.disease);
            }
            _cureQueue.Clear();

            var query = EntityQueryEnumerator<DiseasedComponent, DiseaseCarrierComponent, MobStateComponent>();
            while (query.MoveNext(out var uid, out var diseased, out var carrierComp, out var mobState))
            {
                DebugTools.Assert(carrierComp.Diseases.Count > 0);

                if (_mobStateSystem.IsDead(uid, mobState))
                {
                    if (_random.Prob(0.005f * frameTime)) //Mean time to remove is 200 seconds per disease
                        CureDisease(uid, carrierComp, _random.Pick(carrierComp.Diseases));

                    continue;
                }

                for (var i = 0; i < carrierComp.Diseases.Count; i++) //this is a for-loop so that it doesn't break when new diseases are added
                {
                    var disease = carrierComp.Diseases[i];
                    disease.Accumulator += frameTime;
                    disease.TotalAccumulator += frameTime;

                    if (disease.Accumulator < disease.TickTime) continue;

                    // if the disease is on the silent disease list, don't do effects
                    var doEffects = carrierComp.CarrierDiseases?.Contains(disease.ID) != true;
                    var args = new DiseaseEffectArgs(uid, disease, EntityManager);
                    disease.Accumulator -= disease.TickTime;

                    int stage = 0; //defaults to stage 0 because you should always have one
                    float lastThreshold = 0;
                    for (var j = 0; j < disease.Stages.Count; j++)
                    {
                        if (disease.TotalAccumulator >= disease.Stages[j] &&
                            disease.Stages[j] > lastThreshold)
                        {
                            lastThreshold = disease.Stages[j];
                            stage = j;
                        }
                    }

                    foreach (var cure in disease.Cures)
                    {
                        if (cure.Stages.AsSpan().Contains(stage) && cure.Cure(args))
                            CureDisease(uid, carrierComp, disease);
                    }

                    if (doEffects)
                    {
                        foreach (var effect in disease.Effects)
                        {
                            if (effect.Stages.AsSpan().Contains(stage) && _random.Prob(effect.Probability))
                                effect.Effect(args);
                        }
                    }
                }
            }
        }

        ///
        /// Event Handlers
        ///

        /// <summary>
        /// Fill in the natural immunities of this entity.
        /// </summary>
        private void OnInit(EntityUid uid, DiseaseCarrierComponent component, ComponentInit args)
        {
            if (component.NaturalImmunities == null || component.NaturalImmunities.Count == 0)
                return;

            foreach (var immunity in component.NaturalImmunities)
            {
                if (_prototypeManager.TryIndex<DiseasePrototype>(immunity, out var disease))
                    component.PastDiseases.Add(disease);
                else
                {
                    Log.Error($"Failed to index disease prototype {immunity} for {uid}");
                }
            }
        }

        /// <summary>
        /// Used when something is trying to cure ANY disease on the target,
        /// not for special disease interactions. Randomly
        /// tries to cure every disease on the target.
        /// </summary>
        private void OnTryCureDisease(EntityUid uid, DiseaseCarrierComponent component, CureDiseaseAttemptEvent args)
        {
            foreach (var disease in component.Diseases)
            {
                var cureProb = ((args.CureChance / component.Diseases.Count) - disease.CureResist);
                if (cureProb < 0)
                    return;
                if (cureProb > 1)
                {
                    CureDisease(uid, component, disease);
                    return;
                }
                if (_random.Prob(cureProb))
                {
                    CureDisease(uid, component, disease);
                    return;
                }
            }
        }

        private void OnRejuvenate(EntityUid uid, DiseaseCarrierComponent component, RejuvenateEvent args)
        {
            CureAllDiseases(uid, component);
        }

        /// <summary>
        /// Called when a component with disease protection
        /// is equipped so it can be added to the person's
        /// total disease resistance
        /// </summary>
        private void OnEquipped(EntityUid uid, DiseaseProtectionComponent component, GotEquippedEvent args)
        {
            // This only works on clothing
            if (!TryComp<ClothingComponent>(uid, out var clothing))
                return;
            // Is the clothing in its actual slot?
            if (!clothing.Slots.HasFlag(args.SlotFlags))
                return;
            // Give the user the component's disease resist
            if (TryComp<DiseaseCarrierComponent>(args.Equipee, out var carrier))
                carrier.DiseaseResist += component.Protection;
            // Set the component to active to the unequip check isn't CBT
            component.IsActive = true;
        }

        /// <summary>
        /// Called when a component with disease protection
        /// is unequipped so it can be removed from the person's
        /// total disease resistance
        /// </summary>
        private void OnUnequipped(EntityUid uid, DiseaseProtectionComponent component, GotUnequippedEvent args)
        {
            // Only undo the resistance if it was affecting the user
            if (!component.IsActive)
                return;
            if (TryComp<DiseaseCarrierComponent>(args.Equipee, out var carrier))
                carrier.DiseaseResist -= component.Protection;
            component.IsActive = false;
        }

        /// <summary>
        /// Called when it's already decided a disease will be cured
        /// so it can be safely queued up to be removed from the target
        /// and added to past disease history (for immunity)
        /// </summary>
        private void CureDisease(EntityUid uid, DiseaseCarrierComponent carrier, DiseasePrototype disease)
        {
            var cureTuple = (uid, carrier, disease);
            _cureQueue.Enqueue(cureTuple);
            _popupSystem.PopupEntity(Loc.GetString("disease-cured"), uid, uid);
        }

        public void CureAllDiseases(EntityUid uid, DiseaseCarrierComponent? carrier = null)
        {
            if (!Resolve(uid, ref carrier))
                return;

            foreach (var disease in carrier.Diseases)
            {
                CureDisease(uid, carrier, disease);
            }
        }

        /// <summary>
        /// When a diseased person interacts with something, check infection.
        /// </summary>
        private void OnContactInteraction(EntityUid uid, DiseasedComponent component, ContactInteractionEvent args)
        {
            InteractWithDiseased(uid, args.Other);
        }

        private void OnEntitySpeak(EntityUid uid, DiseasedComponent component, EntitySpokeEvent args)
        {
            if (TryComp<DiseaseCarrierComponent>(uid, out var carrier))
            {
                SneezeCough(uid, _random.Pick(carrier.Diseases), string.Empty);
            }
        }

        /// <summary>
        /// Called when a vaccine is used on someone
        /// to handle the vaccination doafter
        /// </summary>
        private void OnAfterInteract(EntityUid uid, DiseaseVaccineComponent vaxx, AfterInteractEvent args)
        {
            if (args.Target == null || !args.CanReach || args.Handled)
                return;

            args.Handled = true;

            if (vaxx.Used)
            {
                _popupSystem.PopupEntity(Loc.GetString("vaxx-already-used"), args.User, args.User);
                return;
            }

            _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, vaxx.InjectDelay, new VaccineDoAfterEvent(), uid, target: args.Target, used: uid)
            {
                BreakOnMove = true,
                NeedHand = true,
            });
        }

        /// <summary>
        /// Called when a vaccine is examined.
        /// Currently doesn't do much because
        /// vaccines don't have unique art with a seperate
        /// state visualizer.
        /// </summary>
        private void OnExamined(EntityUid uid, DiseaseVaccineComponent vaxx, ExaminedEvent args)
        {
            if (args.IsInDetailsRange)
            {
                if (vaxx.Used)
                    args.PushMarkup(Loc.GetString("vaxx-used"));
                else
                    args.PushMarkup(Loc.GetString("vaxx-unused"));
            }
        }

        private void OnApplyMetabolicMultiplier(EntityUid uid, DiseaseCarrierComponent component, ApplyMetabolicMultiplierEvent args)
        {
            foreach (var disease in component.Diseases)
            {
                disease.TickTime /= args.Multiplier;
                if (disease.Accumulator >= disease.TickTime)
                    disease.Accumulator = disease.TickTime;
            }
        }

        ///
        /// Helper functions
        ///

        /// <summary>
        /// Tries to infect anyone that
        /// interacts with a diseased person or body
        /// </summary>
        private void InteractWithDiseased(EntityUid diseased, EntityUid target, DiseaseCarrierComponent? diseasedCarrier = null)
        {
            if (!Resolve(diseased, ref diseasedCarrier, false) ||
                diseasedCarrier.Diseases.Count == 0 ||
                !TryComp<DiseaseCarrierComponent>(target, out var carrier))
                return;

            var disease = _random.Pick(diseasedCarrier.Diseases);
            TryInfect(target, carrier, disease, 0.4f);
        }

        /// <summary>
        /// Pits the infection chance against the
        /// person's disease resistance and
        /// rolls the dice to see if they get
        /// the disease.
        /// </summary>
        /// <param name="uid">The target entity UID</param>
        /// <param name="carrier">The target of the disease</param>
        /// <param name="disease">The disease to apply</param>
        /// <param name="chance">% chance of the disease being applied, before considering resistance</param>
        /// <param name="forced">Bypass the disease's infectious trait.</param>
        public void TryInfect(EntityUid uid, DiseaseCarrierComponent carrier, DiseasePrototype? disease, float chance = 0.7f, bool forced = false)
        {
            if (disease == null || !forced && !disease.Infectious)
                return;
            var infectionChance = chance - carrier.DiseaseResist;
            if (infectionChance <= 0)
                return;
            if (_random.Prob(infectionChance))
                TryAddDisease(uid, disease, carrier);
        }

        public void TryInfect(EntityUid uid, DiseaseCarrierComponent carrier, string? disease, float chance = 0.7f, bool forced = false)
        {
            if (disease == null || !_prototypeManager.TryIndex<DiseasePrototype>(disease, out var d))
                return;

            TryInfect(uid, carrier, d, chance, forced);
        }

        /// <summary>
        /// Raises an event for systems to cancel the snough if needed
        /// Plays a sneeze/cough sound and popup if applicable
        /// and then tries to infect anyone in range
        /// if the snougher is not wearing a mask.
        /// </summary>
        public bool SneezeCough(EntityUid uid, DiseasePrototype? disease, string emoteId, bool airTransmit = true, TransformComponent? xform = null)
        {
            if (!Resolve(uid, ref xform))
                return false;

            if (_mobStateSystem.IsDead(uid)) return false;

            var attemptSneezeCoughEvent = new AttemptSneezeCoughEvent(uid, emoteId);
            RaiseLocalEvent(uid, ref attemptSneezeCoughEvent);
            if (attemptSneezeCoughEvent.Cancelled) return false;

            _chatSystem.TryEmoteWithChat(uid, emoteId);

            if (disease is not { Infectious: true } || !airTransmit)
                return true;

            if (_inventorySystem.TryGetSlotEntity(uid, "mask", out var maskUid) &&
                EntityManager.TryGetComponent<IngestionBlockerComponent>(maskUid, out var blocker) &&
                blocker.Enabled)
                return true;

            var carrierQuery = GetEntityQuery<DiseaseCarrierComponent>();

            foreach (var entity in _lookup.GetEntitiesInRange(xform.Coordinates, 2f))
            {
                if (!carrierQuery.TryGetComponent(entity, out var carrier) ||
                    !_interactionSystem.InRangeUnobstructed(uid, entity)) continue;

                TryInfect(entity, carrier, disease, 0.3f);
            }
            return true;
        }

        /// <summary>
        /// Adds a disease to the carrier's
        /// past diseases to give them immunity
        /// IF they don't already have the disease.
        /// </summary>
        public void Vaccinate(DiseaseCarrierComponent carrier, DiseasePrototype disease)
        {
            foreach (var currentDisease in carrier.Diseases)
            {
                if (currentDisease.ID == disease.ID) //ID because of the way protoypes work
                    return;
            }
            carrier.PastDiseases.Add(disease);
        }

        private void OnDoAfter(EntityUid uid, DiseaseVaccineComponent component, DoAfterEvent args)
        {
            if (args.Handled || args.Cancelled || !TryComp<DiseaseCarrierComponent>(args.Args.Target, out var carrier) || component.Disease == null)
                return;

            Vaccinate(carrier, component.Disease);
            EntityManager.DeleteEntity(uid);
            args.Handled = true;
        }
    }

    /// <summary>
    /// Controls whether the snough is a sneeze, cough
    /// or neither. If none, will not create
    /// a popup. Mostly used for talking
    /// </summary>
    public enum SneezeCoughType
    {
        Sneeze,
        Cough,
        None
    }
}
