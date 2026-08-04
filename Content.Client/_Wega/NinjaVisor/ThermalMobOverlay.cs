using Content.Shared.Mobs.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;

namespace Content.Client._Wega.NinjaVisor;

public sealed class ThermalMobOverlay : Overlay
{
    private readonly IEntityManager _entManager;
    private readonly SharedTransformSystem _transform;
    private readonly SpriteSystem _sprite;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public ThermalMobOverlay(IEntityManager entManager)
    {
        _entManager = entManager;
        _transform = _entManager.System<SharedTransformSystem>();
        _sprite = _entManager.System<SpriteSystem>();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;
        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();
        var spriteQuery = _entManager.GetEntityQuery<SpriteComponent>();
        var eyeRot = args.Viewport.Eye?.Rotation ?? Angle.Zero;
        var bounds = args.WorldAABB.Enlarged(1f);

        var query = _entManager.AllEntityQueryEnumerator<MobStateComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (!xformQuery.TryGetComponent(uid, out var xform) || xform.MapID != args.MapId)
                continue;

            if (!spriteQuery.TryGetComponent(uid, out var sprite) || !sprite.Visible)
                continue;

            var worldPos = _transform.GetWorldPosition(xform, xformQuery);
            if (!bounds.Contains(worldPos))
                continue;

            var worldRot = _transform.GetWorldRotation(xform, xformQuery);
            _sprite.RenderSprite((uid, sprite), handle, eyeRot, worldRot, worldPos);
        }

        handle.SetTransform(System.Numerics.Matrix3x2.Identity);
    }
}
