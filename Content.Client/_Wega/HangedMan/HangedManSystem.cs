using System.Numerics;
using Content.Shared.HangedMan;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Timing;

namespace Content.Client.HangedMan;

/// <summary>
/// Applies the gentle swaying of a hanged mob's sprite, pivoting from a point
/// above the head. The rope itself is drawn by <see cref="HangedManRopeOverlay"/>.
/// </summary>
public sealed partial class HangedManSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private IOverlayManager _overlay = default!;
    [Dependency] private IEyeManager _eye = default!;

    /// <summary>
    /// How far above the sprite origin the swing pivots, in tiles.
    /// </summary>
    public const float PivotHeight = 0.6f;

    // Remembers each victim's original sprite offset so it can be restored.
    private readonly Dictionary<EntityUid, Vector2> _baseOffsets = new();

    private HangedManRopeFrontOverlay? _frontOverlay;
    private HangedManRopeBehindOverlay? _behindOverlay;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HangedManVictimComponent, ComponentRemove>(OnRemove);

        _frontOverlay = new HangedManRopeFrontOverlay(EntityManager, _timing, _eye);
        _behindOverlay = new HangedManRopeBehindOverlay(EntityManager, _timing, _eye);
        _overlay.AddOverlay(_frontOverlay);
        _overlay.AddOverlay(_behindOverlay);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        if (_frontOverlay != null)
            _overlay.RemoveOverlay(_frontOverlay);
        if (_behindOverlay != null)
            _overlay.RemoveOverlay(_behindOverlay);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var time = (float)_timing.CurTime.TotalSeconds;
        var query = EntityQueryEnumerator<HangedManVictimComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var victim, out var sprite))
        {
            if (!_baseOffsets.TryGetValue(uid, out var baseOffset))
            {
                baseOffset = sprite.Offset;
                _baseOffsets[uid] = baseOffset;
            }

            var angle = Angle.FromDegrees(MathF.Sin(time * victim.SwaySpeed) * victim.SwayAngle);

            // Offset so the rotation appears to pivot from a point above the origin.
            var sin = (float)Math.Sin(angle.Theta);
            var cos = (float)Math.Cos(angle.Theta);
            var pivotOffset = new Vector2(PivotHeight * sin, PivotHeight * (1f - cos));

            _sprite.SetRotation((uid, sprite), angle);
            _sprite.SetOffset((uid, sprite), baseOffset + pivotOffset);
        }
    }

    private void OnRemove(Entity<HangedManVictimComponent> ent, ref ComponentRemove args)
    {
        if (TryComp<SpriteComponent>(ent, out var sprite))
        {
            _sprite.SetRotation((ent.Owner, sprite), Angle.Zero);

            if (_baseOffsets.TryGetValue(ent, out var baseOffset))
                _sprite.SetOffset((ent.Owner, sprite), baseOffset);
        }

        _baseOffsets.Remove(ent);
    }
}
