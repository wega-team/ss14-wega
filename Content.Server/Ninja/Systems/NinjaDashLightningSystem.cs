using Content.Server.Lightning;
using Content.Shared.Inventory;
using Content.Shared.Ninja.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Map;

namespace Content.Server.Ninja.Systems;

/// <summary>
/// Fires a revenant-style lightning bolt along the katana dash path.
/// </summary>
public sealed class NinjaDashLightningSystem : EntitySystem
{
    private static readonly string[] LightningProtos =
    [
        "LightningNinjaDashRed",
        "LightningNinjaDashBlue",
        "LightningNinjaDash", // green
    ];

    [Dependency] private readonly LightningSystem  _lightning = default!;
    [Dependency] private readonly TransformSystem  _transform  = default!;
    [Dependency] private readonly InventorySystem  _inventory  = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EnergyKatanaComponent, AfterDashEvent>(OnAfterDash);
    }

    private void OnAfterDash(Entity<EnergyKatanaComponent> ent, ref AfterDashEvent args)
    {
        var destMap = _transform.ToMapCoordinates(args.Destination);
        if (destMap.MapId != args.Origin.MapId)
            return;

        var proto = LightningProtos[2]; // default green
        if (_inventory.TryGetSlotEntity(args.User, "outerClothing", out var suit)
            && TryComp<SpiderOSComponent>(suit.Value, out var spiderOS))
        {
            proto = LightningProtos[Math.Clamp(spiderOS.SuitColor, 0, 2)];
        }

        var anchor = Spawn(null, new MapCoordinates(args.Origin.Position, args.Origin.MapId));
        _lightning.ShootLightning(anchor, args.User, proto, triggerLightningEvents: false);
        QueueDel(anchor);
    }
}
