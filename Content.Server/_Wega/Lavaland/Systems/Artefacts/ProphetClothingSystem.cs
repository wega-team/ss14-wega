using System.Numerics;
using Content.Shared.Damage.Systems;
using Content.Shared.Inventory;
using Content.Shared.Lavaland.Artefacts.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Server.Lavaland.Artefacts.Systems;

public sealed partial class ProphetClothingSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedGunSystem _gun = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ProphetClothingComponent, InventoryRelayedEvent<DamageModifyEvent>>(OnDamage);
    }

    private void OnDamage(Entity<ProphetClothingComponent> ent, ref InventoryRelayedEvent<DamageModifyEvent> args)
    {
        if (args.Args.Origin == null)
            return;

        if (!_random.Prob(ent.Comp.ProbChance))
            return;

        var wearer = Transform(ent).ParentUid;
        if (!Exists(wearer))
            return;

        var coords = Transform(wearer).Coordinates;
        var mapUid = _transform.GetMap(wearer);
        if (mapUid == null)
            return;

        var wearerPos = coords.Position;

        var directions = new[]
        {
            new Vector2(0, 1), new Vector2(0, -1), new Vector2(-1, 0), new Vector2(1, 0),
            new Vector2(1, 1).Normalized(), new Vector2(1, -1).Normalized(),
            new Vector2(-1, 1).Normalized(), new Vector2(-1, -1).Normalized()
        };

        foreach (var dir in directions)
        {
            var spawnPos = wearerPos + dir * 0.5f;
            var spawnCoords = new EntityCoordinates(mapUid.Value, spawnPos);

            var projectile = Spawn(ent.Comp.BulletProto, spawnCoords);
            _gun.ShootProjectile(projectile, dir, Vector2.Zero, null, wearer, SharedGunSystem.ProjectileSpeed / 2);
        }
    }
}
