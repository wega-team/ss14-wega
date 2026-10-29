using System.Numerics;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Throwing;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Interaction;

/// <summary>
/// The effect of spawning entities near the target.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class SpawnEntitiesEffect : InteractionEffect
{
    /// <summary>
    /// List of entity prototype IDs for spawning.
    /// </summary>
    [DataField(required: true)]
    public List<EntProtoId> SpawnEntities { get; private set; } = new();

    /// <summary>
    /// Maximum offset (in map units) for random placement.
    /// </summary>
    [DataField]
    public float Offset { get; private set; } = 0f;

    /// <summary>
    /// Try to select the entity created by the user.
    /// </summary>
    [DataField]
    public bool TryPickup { get; private set; }

    /// <summary>
    /// Throw the created entities in a random direction.
    /// </summary>
    [DataField]
    public bool ThrowEntities { get; private set; }

    public override void Apply(EntityUid user, EntityUid target, IEntityManager entityManager)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        var handsSystem = entityManager.System<SharedHandsSystem>();
        var throwingSystem = entityManager.System<ThrowingSystem>();

        var transformSystem = entityManager.System<SharedTransformSystem>();
        var position = transformSystem.GetMapCoordinates(target);

        foreach (var ent in SpawnEntities)
        {
            var finalPosition = position;

            var offsetX = random.NextFloat(-Offset, Offset);
            var offsetY = random.NextFloat(-Offset, Offset);
            finalPosition.Offset(new Vector2(offsetX, offsetY));

            var spawned = entityManager.SpawnEntity(ent, finalPosition);
            if (TryPickup)
            {
                handsSystem.TryPickupAnyHand(user, spawned);
            }
            else if (ThrowEntities)
            {
                throwingSystem.TryThrow(spawned, random.NextVector2());
            }
        }
    }
}
