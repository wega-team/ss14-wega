using Content.Server.Atmos.EntitySystems;
using Content.Shared.Audio;
using Content.Shared.Audio.Components;
using Robust.Shared.Audio.Components;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Audio;

/// <summary>
/// Handles server side vacuum checks for audio and players.
/// Assigns the barotrauma marker so the client can pick the correct reverb preset.
/// </summary>
public sealed partial class DynamicAudioSystem : SharedDynamicAudioSystem
{
    [Dependency] private AtmosphereSystem _atmos = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private IGameTiming _timing = default!;

    private static readonly TimeSpan RecheckInterval = TimeSpan.FromMilliseconds(100);
    private const float SoundBarotraumaMoles = 10f;
    private TimeSpan _nextRecheck;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextRecheck)
            return;

        _nextRecheck = _timing.CurTime + RecheckInterval;
        foreach (var session in _player.Sessions)
        {
            if (session.AttachedEntity is { Valid: true } mob)
                DoDynamicChecks(mob);
        }

        var query = AllEntityQuery<AudioComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (TerminatingOrDeleted(uid) || Paused(uid))
                continue;

            DoDynamicChecks(uid);
        }
    }

    /// <summary>
    /// Assigns or removes the vacuum marker based on the local atmosphere.
    /// </summary>
    private void DoDynamicChecks(EntityUid uid)
    {
        if (TerminatingOrDeleted(uid) || Paused(uid))
            return;

        var mixture = _atmos.GetTileMixture((uid, Transform(uid)));
        var inBarotrauma = mixture is null || mixture.TotalMoles < SoundBarotraumaMoles;

        if (inBarotrauma)
        {
            EnsureComp<InBarotraumaAudioEffectComponent>(uid);
        }
        else if (HasComp<InBarotraumaAudioEffectComponent>(uid))
        {
            RemComp<InBarotraumaAudioEffectComponent>(uid);
        }
    }
}
