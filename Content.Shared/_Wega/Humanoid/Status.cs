namespace Content.Shared.Humanoid
{
    public enum Status : byte
    {
        No,
        Semi,
        Full,
        Absolute,
    }

    public record struct StatusChangedEvent(Status OldStatus, Status NewStatus);
}
