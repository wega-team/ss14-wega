using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Containers;

namespace Content.Shared.Interaction;

/// <summary>
/// The effect of removing a body part from the target (for example: chopping off).
/// </summary>
[Serializable]
[DataDefinition]
// Here, tell me, I'm creating this to make it possible to create more interactions.
// But tell me, are you a maniac? Seriously?
public sealed partial class RemoveBodyPartEffect : InteractionEffect
{
    /// <summary>
    /// The identifier of the body part to be removed.
    /// </summary>
    [DataField]
    public string BodyPart { get; private set; } = string.Empty;

    /// <summary>
    /// Whether to attempt to automatically select the deleted part by the user.
    /// </summary>
    [DataField]
    public bool TryPickup { get; private set; }

    public override void Apply(EntityUid user, EntityUid target, IEntityManager entityManager)
    {
        var bodySystem = entityManager.System<SharedBodySystem>();
        var part = bodySystem.GetBodyPartById(target, BodyPart);
        if (part == null)
            return;

        var containerSystem = entityManager.System<SharedContainerSystem>();
        if (entityManager.HasComponent<BodyPartComponent>(part.Value))
        {
            var containerId = SharedBodySystem.GetPartSlotContainerId(BodyPart);
            if (containerSystem.TryGetContainer(part.Value, containerId, out var container))
            {
                containerSystem.Remove(part.Value, container);
                if (TryPickup)
                {
                    var handsSystem = entityManager.System<SharedHandsSystem>();
                    handsSystem.TryPickupAnyHand(user, part.Value);
                }
            }
        }
    }
}
