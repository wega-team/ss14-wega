namespace Content.Shared.Flash;

[ByRefEvent]
public record struct FlashAttemptDamageEvent(EntityUid Target, TimeSpan FlashDuration, bool Cancelled = false);
