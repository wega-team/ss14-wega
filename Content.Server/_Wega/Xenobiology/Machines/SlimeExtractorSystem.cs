using System.Numerics;
using Content.Server.Power.Components;
using Content.Shared.Interaction;
using Robust.Shared.Physics.Components;
using Content.Shared.Power;
using Content.Shared.Xenobiology.Components;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Content.Shared.Jittering;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Throwing;
using Robust.Shared.Random;
using Content.Shared.Climbing.Events;
using Content.Shared.Inventory;
using Content.Shared.Construction.Components;
using Content.Shared.Xenobiology;
using Content.Shared.Mobs.Systems;
using Content.Shared.Body.Components;
using Robust.Shared.Audio;

namespace Content.Server.Xenobiology
{
    public sealed class SlimeExtractorSystem : EntitySystem
    {
        [Dependency] private readonly IEntityManager _entManager = default!;
        [Dependency] private readonly MobStateSystem _mobState = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedJitteringSystem _jittering = default!;
        [Dependency] private readonly PuddleSystem _puddle = default!;
        [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
        [Dependency] private readonly ThrowingSystem _throwing = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly InventorySystem _inventory = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;
        [Dependency] private readonly SharedAmbientSoundSystem _ambient = default!;

        public const string SlimeExtractPrefix = "MaterialSlimeExtract";

        private readonly Dictionary<SlimeStage, int> _extractYieldByStage = new()
        {
            { SlimeStage.Young, 1 },
            { SlimeStage.Adult, 2 },
            { SlimeStage.Old, 3 },
            { SlimeStage.Ancient, 4 }
        };

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SlimeExtractorComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
            SubscribeLocalEvent<SlimeExtractorComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<SlimeExtractorComponent, UnanchorAttemptEvent>(OnUnanchorAttempt);
            SubscribeLocalEvent<SlimeExtractorComponent, ClimbedOnEvent>(OnClimbedOn);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<SlimeExtractorComponent>();
            while (query.MoveNext(out var uid, out var extractor))
            {
                if (extractor.ProcessingTimer <= 0)
                {
                    if (extractor.IsActive)
                    {
                        OnProcessingFinished(uid, extractor);
                    }
                    continue;
                }

                extractor.ProcessingTimer -= frameTime;
                extractor.RandomMessTimer -= frameTime;

                if (extractor.RandomMessTimer <= 0)
                {
                    DoRandomEffects(uid, extractor);
                    extractor.RandomMessTimer = extractor.RandomMessInterval;
                }

                Dirty(uid, extractor);
            }
        }

        private void OnProcessingFinished(EntityUid uid, SlimeExtractorComponent component)
        {
            component.IsActive = false;
            component.BloodReagent = null;
            RemComp<JitteringComponent>(uid);
            _ambient.SetAmbience(uid, false);
            Dirty(uid, component);

            if (!string.IsNullOrEmpty(component.SlimeType) && component.SlimeStage.HasValue)
            {
                var extractId = $"{SlimeExtractPrefix}{component.SlimeType}";
                var yield = GetExtractYield(component.SlimeStage.Value);
                if (component.Reinforced)
                    yield += 1;

                for (int i = 0; i < yield; i++)
                {
                    _entManager.SpawnEntity(extractId, Transform(uid).Coordinates);
                }

                component.SlimeType = null;
                component.SlimeStage = null;
                component.Reinforced = false;
            }
        }

        private void DoRandomEffects(EntityUid uid, SlimeExtractorComponent component)
        {
            if (_random.Prob(0.2f) && component.BloodReagent != null)
            {
                var blood = new Solution();
                blood.AddReagent(component.BloodReagent, 50);
                _puddle.TrySpillAt(uid, blood, out _);
            }

            if (_random.Prob(0.15f))
            {
                _audio.PlayPvs(new SoundPathSpecifier("/Audio/Voice/Slime/slime_squish.ogg"), uid);
            }
        }

        private void OnAfterInteractUsing(Entity<SlimeExtractorComponent> extractor, ref AfterInteractUsingEvent args)
        {
            if (!args.CanReach || args.Target == null)
                return;

            if (!CanProcess(extractor, args.Used))
                return;

            StartProcessing(args.Used, extractor);
        }

        private void OnPowerChanged(EntityUid uid, SlimeExtractorComponent component, ref PowerChangedEvent args)
        {
            if (!args.Powered && component.IsActive)
            {
                component.ProcessingTimer = 0;
                component.IsActive = false;
                Dirty(uid, component);
            }
        }

        private void OnUnanchorAttempt(EntityUid uid, SlimeExtractorComponent component, UnanchorAttemptEvent args)
        {
            if (component.IsActive)
                args.Cancel();
        }

        private void OnClimbedOn(Entity<SlimeExtractorComponent> extractor, ref ClimbedOnEvent args)
        {
            if (!CanProcess(extractor, args.Climber))
            {
                var direction = new Vector2(_random.Next(-2, 2), _random.Next(-2, 2));
                _throwing.TryThrow(args.Climber, direction, 0.5f);
                return;
            }

            _adminLogger.Add(LogType.Action, LogImpact.High,
                $"{ToPrettyString(args.Instigator):player} used slime extractor on {ToPrettyString(args.Climber):target}");

            StartProcessing(args.Climber, extractor);
        }

        private bool CanProcess(Entity<SlimeExtractorComponent> extractor, EntityUid slime)
        {
            if (!HasComp<SlimeGrowthComponent>(slime) || !_mobState.IsDead(slime))
                return false;

            if (extractor.Comp.IsActive)
                return false;

            if (!Transform(extractor).Anchored)
                return false;

            if (TryComp<ApcPowerReceiverComponent>(extractor, out var power) && !power.Powered)
                return false;

            return true;
        }

        private void StartProcessing(EntityUid slime, Entity<SlimeExtractorComponent> extractor)
        {
            if (!TryComp<PhysicsComponent>(slime, out var physics) || !TryComp<SlimeGrowthComponent>(slime, out var slimeGrowth))
                return;

            var component = extractor.Comp;
            component.IsActive = true;
            component.ProcessingTimer = physics.FixturesMass * component.ProcessingTimePerUnitMass;
            component.RandomMessTimer = component.RandomMessInterval;

            component.SlimeType = slimeGrowth.SlimeType.ToString();
            component.SlimeStage = slimeGrowth.CurrentStage;
            component.Reinforced = slimeGrowth.Reinforced;

            _jittering.AddJitter(extractor, -10, 100);
            _audio.PlayPvs(new SoundPathSpecifier("/Audio/Machines/reclaimer_startup.ogg"), extractor);
            _ambient.SetAmbience(extractor, true);

            if (TryComp<BloodstreamComponent>(slime, out var bloodstream))
            {
                component.BloodReagent = bloodstream.BloodReagent;
            }

            foreach (var item in _inventory.GetHandOrInventoryEntities(slime))
            {
                _transform.DropNextTo(item, extractor.Owner);
            }

            QueueDel(slime);
            Dirty(extractor, component);
        }

        private int GetExtractYield(SlimeStage stage)
        {
            return _extractYieldByStage.TryGetValue(stage, out var yield) ? yield : 0;
        }
    }
}
