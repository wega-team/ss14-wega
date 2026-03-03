using Content.Shared.Inventory;

namespace Content.Shared.Lavaland.Events;

[ByRefEvent]
public record struct AshProtectionAttemptEvent(float Modifier = 0f) : IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.WITHOUT_POCKET;
}
