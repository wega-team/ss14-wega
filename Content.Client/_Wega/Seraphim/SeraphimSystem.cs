using System.Linq;
using System.Numerics;
using Content.Shared._Wega.Seraphim;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Spawners;
using Robust.Shared.Timing;

namespace Content.Client._Wega.Seraphim;

public sealed partial class SeraphimSystem : SharedSeraphimSystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IOverlayManager _overlayMan = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private SeraphimOverlay _overlay = default!;

    private readonly Dictionary<EntityUid, Vector2> _lastPositions = new();
    private readonly List<TrailRecord> _trails = new();
    private TimeSpan _nextTrail = TimeSpan.Zero;
    private bool _trailFlip;

    private static readonly TimeSpan TrailInterval = TimeSpan.FromSeconds(0.12);
    private static readonly TimeSpan TrailLifetime = TimeSpan.FromSeconds(0.45);

    private static readonly Color TrailPurple = new(0.72f, 0.45f, 1f, 0.55f);
    private static readonly Color TrailWhite = new(1f, 1f, 1f, 0.45f);

    private sealed record TrailRecord(EntityUid CloneUid, EntityUid SourceUid, Color Tint, TimeSpan SpawnTime);

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new SeraphimOverlay();

        SubscribeLocalEvent<SeraphimOverdriveComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SeraphimOverdriveComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SeraphimOverdriveComponent, LocalPlayerAttachedEvent>(OnAttached);
        SubscribeLocalEvent<SeraphimOverdriveComponent, LocalPlayerDetachedEvent>(OnDetached);
    }

    private void OnInit(Entity<SeraphimOverdriveComponent> ent, ref ComponentInit args)
    {
        if (_player.LocalEntity == ent.Owner)
            _overlayMan.AddOverlay(_overlay);
    }

    private void OnShutdown(Entity<SeraphimOverdriveComponent> ent, ref ComponentShutdown args)
    {
        _lastPositions.Remove(ent.Owner);

        if (_player.LocalEntity == ent.Owner)
            _overlayMan.RemoveOverlay(_overlay);
    }

    private void OnAttached(Entity<SeraphimOverdriveComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        _overlayMan.AddOverlay(_overlay);
    }

    private void OnDetached(Entity<SeraphimOverdriveComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        _overlayMan.RemoveOverlay(_overlay);
    }

    // Послесвечения
    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var cur = _timing.CurTime;

        // Спавн новых следов
        if (cur >= _nextTrail)
        {
            _nextTrail = cur + TrailInterval;
            _trailFlip = !_trailFlip;

            var query = EntityQueryEnumerator<SeraphimOverdriveComponent>();
            while (query.MoveNext(out var uid, out _))
            {
                var pos = _transform.GetWorldPosition(uid);

                // Не двигается - без следа
                if (_lastPositions.TryGetValue(uid, out var last)
                    && (pos - last).LengthSquared() < 0.001f)
                {
                    continue;
                }

                _lastPositions[uid] = pos;
                SpawnTrail(uid, cur);
            }

            var stale = new List<EntityUid>();
            foreach (var key in _lastPositions.Keys)
            {
                if (!HasComp<SeraphimOverdriveComponent>(key))
                    stale.Add(key);
            }

            foreach (var key in stale)
            {
                _lastPositions.Remove(key);
            }
        }

        // Синхронизация с пешкой
        var lifetime = (float) TrailLifetime.TotalSeconds;
        for (var i = _trails.Count - 1; i >= 0; i--)
        {
            var trail = _trails[i];

            if (!Exists(trail.CloneUid) || !Exists(trail.SourceUid))
            {
                _trails.RemoveAt(i);
                continue;
            }

            // Затухание цветом
            if (TryComp<SpriteComponent>(trail.CloneUid, out var dst))
            {
                var age = (float) (cur - trail.SpawnTime).TotalSeconds;
                var k = Math.Clamp(1f - age / lifetime, 0f, 1f);
                dst.Color = trail.Tint.WithAlpha(trail.Tint.A * k);
            }

            // Поворот и поза
            _transform.SetWorldRotation(trail.CloneUid, _transform.GetWorldRotation(trail.SourceUid));

            if ((cur - trail.SpawnTime) >= TrailLifetime)
                _trails.RemoveAt(i);
        }
    }

    private void SpawnTrail(EntityUid uid, TimeSpan cur)
    {
        if (!TryComp<SpriteComponent>(uid, out var sourceSprite))
            return;

        var clone = SpawnAtPosition("ClientsideClone", Transform(uid).Coordinates);
        var sprite = Comp<SpriteComponent>(clone);
        _sprite.CopySprite((uid, sourceSprite), (clone, sprite));
        _sprite.SetVisible((clone, sprite), true);

        // Все слои unshaded
        for (var i = 0; i < sprite.AllLayers.Count(); i++)
            sprite.LayerSetShader(i, "unshaded");

        var despawn = EnsureComp<TimedDespawnComponent>(clone);
        despawn.Lifetime = (float) TrailLifetime.TotalSeconds;

        _trails.Add(new TrailRecord(clone, uid, _trailFlip ? TrailPurple : TrailWhite, cur));
    }
}
