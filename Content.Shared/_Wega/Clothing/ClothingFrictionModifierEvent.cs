using Content.Shared.Inventory;

namespace Content.Shared.Clothing;

[ByRefEvent]
public record struct ClothingFrictionModifierEvent : IInventoryRelayEvent
{
    public float FrictionModifier { get; set; }
    public float FrictionNoInputModifier { get; set; }
    public float AccelerationModifier { get; set; }

    public ClothingFrictionModifierEvent(float frictionModifier = 1f, float accelerationModifier = 1f)
    {
        FrictionModifier = frictionModifier;
        FrictionNoInputModifier = frictionModifier;
        AccelerationModifier = accelerationModifier;
    }

    public ClothingFrictionModifierEvent(float frictionModifier, float frictionNoInputModifier, float accelerationModifier)
    {
        FrictionModifier = frictionModifier;
        FrictionNoInputModifier = frictionNoInputModifier;
        AccelerationModifier = accelerationModifier;
    }

    SlotFlags IInventoryRelayEvent.TargetSlots => ~SlotFlags.POCKET;
}
