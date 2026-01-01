using Content.Server.Atmos.EntitySystems;
using Robust.Server.GameObjects;
using Content.Server.CoreTempChange.Components;

namespace Content.Server.CoreTempChange.Effects;

public sealed class CoreTempChangeSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly TransformSystem _xform = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CoreTempChangeComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out var comp, out var xform))
        {
            var grid = xform.GridUid;
            var map = xform.MapUid;
            var indices = _xform.GetGridTilePositionOrDefault((ent, xform));
            var mixture = _atmosphere.GetTileMixture(grid, map, indices, true);

            if (mixture is { })
            {
                mixture.Temperature += comp.TempChangePerSecond * frameTime;
            }
        }
    }
}