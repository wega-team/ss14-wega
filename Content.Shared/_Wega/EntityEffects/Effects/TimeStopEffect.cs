using Content.Shared.Coordinates;
using Content.Shared.Interaction;
using Content.Shared.Administration;
using Content.Shared.Mobs.Components;
using Robust.Shared.Audio;
using System.Linq;
using Content.Shared.CombatMode.Pacification;
using Robust.Shared.Prototypes;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Shared.EntityEffects.Effects.Transform;

/// <summary>
/// Creates a Flash at this entity's coordinates.
/// Range is modified by scale.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class TimeStopEntityEffectSystem : EntityEffectSystem<TransformComponent, TimeStopEffect>
{
    [Dependency] private EntityLookupSystem _entityLookup = default!;
    [Dependency] private SharedTransformSystem _xform = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<TimeStopEffect> args)
    {
        var transform = Transform(entity);
        var mapPosition = _xform.GetMapCoordinates(transform);

        Spawn("Chronofield", mapPosition);
        var nearbyTargets = _entityLookup.GetEntitiesInRange<MobStateComponent>(Transform(entity).Coordinates, 2.5f);

        foreach (var target in nearbyTargets)
        {
			EnsureComp<AdminFrozenComponent>(target);
			Timer.Spawn(args.Effect.Time, () => RemComp<AdminFrozenComponent>(target));
        }
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class TimeStopEffect : EntityEffectBase<TimeStopEffect>
{
    [DataField]
    public TimeSpan Time = TimeSpan.FromSeconds(7);

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-timestop-reaction-effect");
}

