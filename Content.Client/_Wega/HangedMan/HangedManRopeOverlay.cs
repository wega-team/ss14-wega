using System.Numerics;
using Content.Shared.HangedMan;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Timing;

namespace Content.Client.HangedMan;

/// <summary>
/// Draws the noose rope above each hanged victim in code (no sprite), centred
/// straight above the character. Two instances are used: one drawn just above
/// mobs (for victims facing away/north) and one just below mobs but above the
/// floor/carpet (for the other facings), so the rope sits behind the body for
/// front/side facings without sinking under floor decals.
/// </summary>
public abstract class HangedManRopeOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;

    private readonly IEntityManager _entManager;
    private readonly IGameTiming _timing;
    private readonly IEyeManager _eye;
    private readonly SharedTransformSystem _xform;
    private readonly bool _front;

    private static readonly Color RopeColor = Color.FromHex("#2E8B2E");

    /// <summary>Top of the rope above the head, in tiles.</summary>
    private const float RopeTop = 0.95f;

    /// <summary>Where the rope attaches to the body (the neck), in tiles.</summary>
    private const float RopeBottom = 0.18f;

    /// <summary>Thickness of the rope, in tiles.</summary>
    private const float Width = 0.08f;

    protected HangedManRopeOverlay(IEntityManager entManager, IGameTiming timing, IEyeManager eye, bool front)
    {
        _entManager = entManager;
        _timing = timing;
        _eye = eye;
        _front = front;
        _xform = entManager.System<SharedTransformSystem>();

        // Sort just above mobs when in front, just below mobs (but above carpets) when behind.
        ZIndex = (int)(front
            ? Content.Shared.DrawDepth.DrawDepth.OverMobs
            : Content.Shared.DrawDepth.DrawDepth.BelowMobs);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var time = (float)_timing.CurTime.TotalSeconds;
        var eyeRot = _eye.CurrentEye.Rotation;
        var handle = args.WorldHandle;
        handle.SetTransform(Matrix3x2.Identity);

        var query = _entManager.EntityQueryEnumerator<HangedManVictimComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var victim, out var xform))
        {
            if (xform.MapID != args.MapId)
                continue;

            // Facing the viewer (north/away) -> rope on top; otherwise rope behind.
            if (IsFacingNorth(uid, eyeRot) != _front)
                continue;

            var pos = _xform.GetWorldPosition(uid);
            var screen = _eye.WorldToScreen(pos);

            // Pixels per tile on screen (magnitude, so zoom is handled).
            var scale = (_eye.WorldToScreen(pos + new Vector2(0f, 1f)) - screen).Length();
            if (scale <= 0f)
                continue;

            var theta = MathF.Sin(time * victim.SwaySpeed) * victim.SwayAngle * (MathF.PI / 180f);
            var d = RopeTop - RopeBottom;

            // Compute in screen space (guarantees centred + straight up), then back to world.
            var topScreen = screen + new Vector2(0f, -RopeTop * scale);
            var bottomScreen = screen + new Vector2(d * MathF.Sin(theta) * scale, -RopeBottom * scale);

            var top = _eye.ScreenToMap(topScreen).Position;
            var bottom = _eye.ScreenToMap(bottomScreen).Position;

            var diff = bottom - top;
            var length = diff.Length();
            if (length <= 0f)
                continue;

            var mid = top + diff / 2f;
            var angle = diff.ToWorldAngle();
            var box = new Box2(-Width / 2f, -length / 2f, Width / 2f, length / 2f);
            var rotated = new Box2Rotated(box.Translated(mid), angle, mid);

            handle.DrawRect(rotated, RopeColor);
        }
    }

    private bool IsFacingNorth(EntityUid uid, Angle eyeRot)
    {
        var angle = (_xform.GetWorldRotation(uid) + eyeRot).Reduced().FlipPositive();
        var deg = angle.Degrees;
        return deg is > 135 and < 225;
    }
}

/// <summary>Rope drawn above the body, for victims facing away (north).</summary>
public sealed class HangedManRopeFrontOverlay : HangedManRopeOverlay
{
    public HangedManRopeFrontOverlay(IEntityManager entManager, IGameTiming timing, IEyeManager eye)
        : base(entManager, timing, eye, front: true)
    {
    }
}

/// <summary>Rope drawn behind the body (but above the floor), for the other facings.</summary>
public sealed class HangedManRopeBehindOverlay : HangedManRopeOverlay
{
    public HangedManRopeBehindOverlay(IEntityManager entManager, IGameTiming timing, IEyeManager eye)
        : base(entManager, timing, eye, front: false)
    {
    }
}
