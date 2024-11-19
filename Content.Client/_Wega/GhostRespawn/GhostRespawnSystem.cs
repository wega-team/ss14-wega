using Content.Shared.Wega.Ghost.Respawn;

namespace Content.Client.Wega.Ghost.Respawn;

public sealed class GhostRespawnSystem : EntitySystem
{
    public TimeSpan? GhostRespawnTime { get; private set; }
    public event Action? GhostRespawn;

    public override void Initialize()
    {
        SubscribeNetworkEvent<GhostRespawnEvent>(OnGhostRespawnReset);
    }

    private void OnGhostRespawnReset(GhostRespawnEvent e)
    {
        GhostRespawnTime = e.Time;
        GhostRespawn?.Invoke();
    }
}
