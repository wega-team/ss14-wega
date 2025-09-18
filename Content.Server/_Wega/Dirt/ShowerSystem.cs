using Content.Server.Fluids.EntitySystems;
using Content.Server.Power.EntitySystems;
using Content.Shared.Audio;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.DirtVisuals;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Shower
{
    public sealed class ShowerSystem : EntitySystem
    {
        [Dependency] private readonly SharedAmbientSoundSystem _ambient = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
        [Dependency] private readonly PuddleSystem _puddle = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly ReactiveSystem _reactive = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ShowerComponent, GetVerbsEvent<AlternativeVerb>>(AddShowerVerb);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<ShowerComponent>();
            while (query.MoveNext(out var uid, out var shower))
            {
                if (!shower.IsSpraying)
                    continue;

                shower.RemainingTime -= frameTime;
                if (shower.RemainingTime <= 0)
                {
                    SprayWater(uid, shower);
                    shower.RemainingTime = shower.SprayTime;
                }
            }
        }

        private void AddShowerVerb(EntityUid uid, ShowerComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract || !this.IsPowered(uid, EntityManager))
                return;

            AlternativeVerb verb = new()
            {
                Act = () => ToggleSpraying(uid, component),
                Text = component.IsSpraying
                    ? Loc.GetString("shower-verb-stop")
                    : Loc.GetString("shower-verb-start"),
                Priority = 2
            };

            args.Verbs.Add(verb);
        }

        public void ToggleSpraying(EntityUid uid, ShowerComponent component)
        {
            if (component.IsSpraying)
                StopSpraying(uid, component);
            else
                StartSpraying(uid, component);
        }

        public void StartSpraying(EntityUid uid, ShowerComponent component)
        {
            if (component.IsSpraying)
                return;

            component.IsSpraying = true;
            component.RemainingTime = component.SprayTime;

            _audio.PlayPvs(component.SprayStartSound, uid);
            _ambient.SetAmbience(uid, true);

            _appearance.SetData(uid, ShowerVisuals.Spraying, true);
        }

        private void StopSpraying(EntityUid uid, ShowerComponent component)
        {
            if (!component.IsSpraying)
                return;

            component.IsSpraying = false;

            _audio.PlayPvs(component.SprayEndSound, uid);
            _ambient.SetAmbience(uid, false);

            _appearance.SetData(uid, ShowerVisuals.Spraying, false);
        }

        private void SprayWater(EntityUid uid, ShowerComponent component)
        {
            if (!_solutionContainer.TryGetSolution(uid, "shower", out var showerSol, out var solution) || solution.Volume == 0)
            {
                StopSpraying(uid, component);
                return;
            }

            var coordinates = Transform(uid).Coordinates;
            var mobsInRange = _lookup.GetEntitiesInRange<ReactiveComponent>(coordinates, 0.5f);
            var amountPerTarget = component.WaterAmount / (mobsInRange.Count + 1);

            foreach (var mob in mobsInRange)
            {
                var mobSolution = _solutionContainer.SplitSolution(showerSol.Value, amountPerTarget);
                _reactive.DoEntityReaction(mob, mobSolution, ReactionMethod.Touch);
            }

            var floorSolution = _solutionContainer.SplitSolution(showerSol.Value, amountPerTarget);
            _puddle.TrySpillAt(coordinates, floorSolution, out _);
        }
    }
}
