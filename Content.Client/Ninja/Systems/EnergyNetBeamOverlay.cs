using Content.Shared.Ninja.Components;
using Robust.Client.Graphics;
using Robust.Shared.Enums;

namespace Content.Client.Ninja.Systems;

/// <summary>
/// Draws a programmatic green beam between an energy-net victim and the caster ninja, for as long as
/// the victim has <see cref="EnergyNetBeamComponent"/> (the server removes it after a short time).
/// </summary>
public sealed class EnergyNetBeamOverlay : Overlay
{
    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    private static readonly Color BeamColor = Color.FromHex("#07c921");

    private readonly IEntityManager _entManager;

    public EnergyNetBeamOverlay(IEntityManager entManager)
    {
        _entManager = entManager;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var query       = _entManager.EntityQueryEnumerator<EnergyNetBeamComponent>();
        var xformQuery  = _entManager.GetEntityQuery<TransformComponent>();
        var xformSystem = _entManager.System<SharedTransformSystem>();
        var worldHandle = args.WorldHandle;

        while (query.MoveNext(out var target, out var comp))
        {
            var caster = comp.Caster;
            if (!caster.IsValid())
                continue;

            if (!xformQuery.TryGetComponent(caster, out var casterXform) ||
                !xformQuery.TryGetComponent(target, out var targetXform))
                continue;

            if (casterXform.MapID != targetXform.MapID)
                continue;

            var casterPos = xformSystem.GetWorldPosition(casterXform, xformQuery);
            var targetPos = xformSystem.GetWorldPosition(targetXform, xformQuery);

            var diff   = targetPos - casterPos;
            var length = diff.Length() / 2f;

            if (length < 0.01f)
                continue;

            var angle    = diff.ToWorldAngle();
            var midPoint = casterPos + diff / 2f;

            // Outer glow — wide, transparent
            var glowBox = new Box2(-0.1f, -length, 0.1f, length);
            worldHandle.DrawRect(new Box2Rotated(glowBox.Translated(midPoint), angle, midPoint),
                BeamColor.WithAlpha(0.25f));

            // Bright core
            var coreBox = new Box2(-0.04f, -length, 0.04f, length);
            worldHandle.DrawRect(new Box2Rotated(coreBox.Translated(midPoint), angle, midPoint),
                BeamColor.WithAlpha(0.85f));
        }
    }
}
