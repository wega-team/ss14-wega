#if DEBUG
using System.Numerics;
using Content.Shared.Audio;
using Robust.Client.Audio;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Audio.Components;
using Robust.Shared.Enums;
using Robust.Shared.Timing;

namespace Content.Client.Audio;

/// <summary>
/// Debug overlay drawing sound rays from sources to the player.
/// </summary>
public sealed partial class SoundInsulationOverlay : Overlay
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IEyeManager _eyeManager = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    // Timing and animation speeds
    private static readonly TimeSpan RetraceInterval = TimeSpan.FromMilliseconds(300);
    private static readonly TimeSpan CycleDuration = TimeSpan.FromMilliseconds(2500);
    private const float WaveSpeed = 15f;

    // Visual rendering parameters
    private const float SourceCircleRadius = 0.35f;
    private const float SourceCircleAlpha = 0.35f;
    private const float WavefrontRadius = 0.10f;
    private const float MaxFadeAtTail = 0.65f;

    // Runtime tracking and rendering buffers
    private readonly Dictionary<EntityUid, AnimatedPath> _paths = new();
    private readonly List<EntityUid> _removeBuffer = new();
    private readonly HashSet<EntityUid> _seenBuffer = new();

    public SoundInsulationOverlay()
    {
        IoCManager.InjectDependencies(this);
    }

    /// <summary>
    /// Handles the animation and rendering of the sound waves.
    /// </summary>
    protected override void Draw(in OverlayDrawArgs args)
    {
        var worldHandle = args.WorldHandle;
        var now = _timing.CurTime;
        var listenerPos = _eyeManager.CurrentEye?.Position.Position ?? Vector2.Zero;

        if (_player.LocalEntity is not { Valid: true } localPlayer)
        {
            _paths.Clear();
            return;
        }

        var tracer = _entManager.System<SoundRayTracer>();
        var lookup = _entManager.System<EntityLookupSystem>();
        var xformSystem = _entManager.System<SharedTransformSystem>();
        var audioSystem = _entManager.System<AudioSystem>();

        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();
        var audioQuery = _entManager.GetEntityQuery<AudioComponent>();
        var ambientQuery = _entManager.GetEntityQuery<AmbientSoundComponent>();

        _seenBuffer.Clear();

        foreach (var ent in lookup.GetEntitiesIntersecting(args.MapId, args.WorldBounds))
        {
            if (!xformQuery.TryGetComponent(ent, out var xform))
                continue;

            var hasAudio = audioQuery.TryGetComponent(ent, out var audio);
            var hasAmbient = ambientQuery.TryGetComponent(ent, out var ambient);

            if (!hasAudio && !hasAmbient)
                continue;

            var sourcePos = xformSystem.GetWorldPosition(xform);
            var distanceToListener = (sourcePos - listenerPos).Length();

            if (hasAudio && audio is not null)
            {
                if (audio.Global || (audio.Flags & AudioFlags.NoOcclusion) != 0)
                    hasAudio = false;
                else if (audioSystem.GetAudioDistance(distanceToListener) > audio.MaxDistance)
                    hasAudio = false;
            }

            if (hasAmbient && ambient is not null)
            {
                if (!ambient.Enabled)
                    hasAmbient = false;
                else if (distanceToListener > ambient.Range)
                    hasAmbient = false;
            }

            if (!hasAudio && !hasAmbient)
                continue;

            _seenBuffer.Add(ent);

            if (!_paths.TryGetValue(ent, out var path))
            {
                path = new AnimatedPath { LastTrace = now - RetraceInterval };
                _paths[ent] = path;
            }

            path.SourcePos = sourcePos;

            if (hasAudio)
                path.SourceColor = Color.LightGreen.WithAlpha(SourceCircleAlpha * 2f);
            else if (hasAmbient)
                path.SourceColor = Color.Orange.WithAlpha(SourceCircleAlpha);
            else
                path.SourceColor = Color.Red.WithAlpha(SourceCircleAlpha);

            if (now - path.LastTrace >= RetraceInterval)
            {
                var result = tracer.Trace(ent, localPlayer, args.MapId);
                path.Segments = result.Path;
                path.TotalLength = 0f;
                foreach (var seg in result.Path)
                {
                    path.TotalLength += (seg.End - seg.Start).Length();
                }

                path.LastTrace = now;
            }
        }

        _removeBuffer.Clear();
        foreach (var uid in _paths.Keys)
        {
            if (!_seenBuffer.Contains(uid))
                _removeBuffer.Add(uid);
        }

        foreach (var uid in _removeBuffer)
            _paths.Remove(uid);

        var cycleMs = CycleDuration.TotalMilliseconds;
        var cycleAge = now.TotalMilliseconds % cycleMs;
        var elapsed = (float)(cycleAge / 1000.0);

        var globalAlpha = 1f;
        var lifeT = (float)(cycleAge / cycleMs);
        if (lifeT > 0.7f)
        {
            globalAlpha = MathF.Max(0.15f, 1f - (lifeT - 0.7f) / 0.3f);
        }

        foreach (var path in _paths.Values)
        {
            worldHandle.DrawCircle(path.SourcePos, SourceCircleRadius, path.SourceColor);

            if (path.Segments.Count == 0)
                continue;

            var revealDist = MathF.Min(path.TotalLength, elapsed * WaveSpeed);
            var waveArrived = revealDist >= path.TotalLength - 0.01f;
            var waveMoving = revealDist > 0f && !waveArrived;

            var cumDist = 0f;
            var wavefront = path.SourcePos;

            foreach (var seg in path.Segments)
            {
                var segLen = (seg.End - seg.Start).Length();
                if (cumDist >= revealDist)
                    break;

                var t = segLen < 1e-4f ? 1f : MathF.Min(1f, (revealDist - cumDist) / segLen);

                var drawEnd = Vector2.Lerp(seg.Start, seg.End, t);
                wavefront = drawEnd;

                var color = Color.InterpolateBetween(Color.Red, Color.Blue, Math.Clamp(seg.AbsorptionAtEnd, 0f, 1f));

                var tailT = path.TotalLength < 0.01f ? 0f : cumDist / path.TotalLength;
                var segAlpha = globalAlpha * (1f - MaxFadeAtTail * tailT);

                worldHandle.DrawLine(seg.Start, drawEnd, color.WithAlpha(segAlpha * 0.9f));
                cumDist += segLen;
            }

            if (waveMoving)
            {
                worldHandle.DrawCircle(wavefront, WavefrontRadius, Color.White.WithAlpha(globalAlpha));
            }
        }
    }

    /// <summary>
    /// Data class holding the visual path state.
    /// </summary>
    private sealed class AnimatedPath
    {
        public Vector2 SourcePos;
        public Color SourceColor;
        public List<SoundRaySegment> Segments = new();
        public float TotalLength;
        public TimeSpan LastTrace;
    }
}
#endif
