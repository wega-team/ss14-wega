using Content.Server.Ninja.Components;
using Content.Shared.Ninja.Components;
using Content.Shared.Physics;
using Content.Shared.Throwing;
using Robust.Shared.Physics.Events;

namespace Content.Server.Ninja.Systems;

/// <summary>
/// Makes ninja throwing stars fly through windows and grilles (GlassLayer entities without Opaque).
/// Also increases their throw speed.
/// </summary>
public sealed class NinjaShurikenSystem : EntitySystem
{
    private const float ShurikenThrowSpeed = 22f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NinjaShurikenComponent, PreventCollideEvent>(OnPreventCollide);
        SubscribeLocalEvent<SpaceNinjaComponent, BeforeThrowEvent>(OnBeforeThrow);
    }

    private void OnPreventCollide(Entity<NinjaShurikenComponent> ent, ref PreventCollideEvent args)
    {
        var layer = args.OtherFixture.CollisionLayer;
        // Glass and grilles have GlassLayer bits (Impassable set) but NOT Opaque — walls have Opaque.
        var isGlass = (layer & (int) CollisionGroup.Impassable) != 0
                   && (layer & (int) CollisionGroup.Opaque)     == 0;
        if (isGlass)
            args.Cancelled = true;
    }

    private void OnBeforeThrow(Entity<SpaceNinjaComponent> ent, ref BeforeThrowEvent args)
    {
        if (HasComp<NinjaShurikenComponent>(args.ItemUid))
            args.ThrowSpeed = ShurikenThrowSpeed;
    }
}
