using Content.Shared.Inventory;

namespace Content.Shared.Lavaland.Events;

[ByRefEvent]
public record struct AshProtectionAttemptEvent(float Modifier = 0f, bool StormShielded = false) : IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.WITHOUT_POCKET;
}
