using Robust.Shared.Serialization;

namespace Content.Shared.Wega.Ghost.Respawn;

[Serializable, NetSerializable]
public sealed partial class GhostRespawnEvent(TimeSpan? time) : EntityEventArgs
{
    public readonly TimeSpan? Time = time;
}
