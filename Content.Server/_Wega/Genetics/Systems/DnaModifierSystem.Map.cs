using Content.Shared.GameTicking;
using Robust.Server.GameObjects;

namespace Content.Server.Genetics.System;

public sealed partial class DnaModifierSystem
{
    [Dependency] private MapSystem _map = default!;

    public EntityUid? PausedMap { get; private set; }

    [SubscribeLocalEvent]
    private void OnRoundRestart(RoundRestartCleanupEvent _)
    {
        if (PausedMap == null || !Exists(PausedMap))
            return;

        Del(PausedMap.Value);
    }

    private void EnsurePausedMap()
    {
        if (PausedMap != null && Exists(PausedMap))
            return;

        PausedMap = _map.CreateMap(false);
    }
}
