using Content.Shared.Ninja.Components;
using Robust.Client.Graphics;
using Robust.Shared.Enums;

namespace Content.Client.Ninja.Systems;

public sealed class ChainKunaiChainOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    private static readonly Color ShadowColor  = Color.FromHex("#1d1d1d");
    private static readonly Color CoreColor    = Color.FromHex("#0c0c0c");

    private readonly IEntityManager _entManager;

    public ChainKunaiChainOverlay(IEntityManager entManager)
    {
        _entManager = entManager;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var query      = _entManager.EntityQueryEnumerator<ChainKunaiProjectileComponent>();
        var xformQuery = _entManager.GetEntityQuery<TransformComponent>();
        var xformSystem = _entManager.System<SharedTransformSystem>();
        var worldHandle = args.WorldHandle;

        while (query.MoveNext(out var kunaiUid, out var comp))
        {
            var ninja = comp.Ninja;
            if (!ninja.IsValid())
                continue;

            if (!xformQuery.TryGetComponent(ninja, out var ninjaXform) ||
                !xformQuery.TryGetComponent(kunaiUid, out var kunaiXform))
                continue;

            if (ninjaXform.MapID != kunaiXform.MapID)
                continue;

            var ninjaPos = xformSystem.GetWorldPosition(ninjaXform, xformQuery);
            var kunaiPos = xformSystem.GetWorldPosition(kunaiXform, xformQuery);

            var diff   = kunaiPos - ninjaPos;
            var length = diff.Length() / 2f;

            if (length < 0.01f)
                continue;

            var angle    = diff.ToWorldAngle();
            var midPoint = ninjaPos + diff / 2f;

            // Outer shadow haze — wide, very transparent
            var shadowBox = new Box2(-0.09f, -length, 0.09f, length);
            worldHandle.DrawRect(new Box2Rotated(shadowBox.Translated(midPoint), angle, midPoint),
                ShadowColor.WithAlpha(0.18f));

            // Thin dark core
            var coreBox = new Box2(-0.04f, -length, 0.04f, length);
            worldHandle.DrawRect(new Box2Rotated(coreBox.Translated(midPoint), angle, midPoint),
                CoreColor.WithAlpha(0.75f));
        }
    }
}
