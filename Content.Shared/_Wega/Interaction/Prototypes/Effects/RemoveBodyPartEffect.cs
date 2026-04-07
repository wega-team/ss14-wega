using Content.Shared.Body;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

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
    /// The category of the body part to be removed (e.g., "Head", "LeftArm").
    /// </summary>
    [DataField]
    public ProtoId<OrganCategoryPrototype>? BodyPart { get; private set; }

    /// <summary>
    /// Whether to attempt to automatically select the deleted part by the user.
    /// </summary>
    [DataField]
    public bool TryPickup { get; private set; }

    public override void Apply(EntityUid user, EntityUid target, IEntityManager entityManager)
    {
        var containerSystem = entityManager.System<SharedContainerSystem>();
        if (!entityManager.TryGetComponent<BodyComponent>(target, out var bodyComp) || bodyComp.Organs == null)
            return;

        var organsList = new List<EntityUid>();
        foreach (var organ in bodyComp.Organs.ContainedEntities)
            organsList.Add(organ);

        EntityUid? organToRemove = null;
        foreach (var organ in organsList)
        {
            if (entityManager.TryGetComponent<OrganComponent>(organ, out var organComp)
                && organComp.Category == BodyPart)
            {
                organToRemove = organ;
                break;
            }
        }

        if (organToRemove == null)
            return;

        containerSystem.Remove(organToRemove.Value, bodyComp.Organs);

        if (TryPickup)
        {
            var handsSystem = entityManager.System<SharedHandsSystem>();
            handsSystem.TryPickupAnyHand(user, organToRemove.Value);
        }
    }
}
