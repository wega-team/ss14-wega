using System.Numerics;
using Content.Server.Lavaland.Mobs.Components;
using Content.Server.NPC.Systems;
using Content.Shared.Damage.Systems;
using Content.Shared.Lavaland.Events;
using Content.Shared.Maps;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Physics;
using Content.Shared.Visuals;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Lavaland.Mobs;

public sealed partial class WendigoSystem : EntitySystem
{
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private TurfSystem _turf = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private MobThresholdSystem _threshold = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private NPCUseActionsOnTargetSystem _npcActions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WendigoBossComponent, DamageChangedEvent>(OnWendigoDamage);

        SubscribeLocalEvent<WendigoBossComponent, WendigoStompActionEvent>(OnStomp);
        SubscribeLocalEvent<WendigoBossComponent, WendigoTeleportActionEvent>(OnTeleport);
        SubscribeLocalEvent<WendigoBossComponent, WendigoScreamActionEvent>(OnScream);
    }

    private void OnWendigoDamage(Entity<WendigoBossComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased)
            return;

        if (ent.Comp.IsEnraged)
            return;

        var healthRatio = GetHealthRatio(ent);
        if (healthRatio <= ent.Comp.EnrageThreshold)
            EnterEnrage(ent);
    }

    private void EnterEnrage(Entity<WendigoBossComponent> ent)
    {
        ent.Comp.IsEnraged = true;
        if (TryComp<MovementSpeedModifierComponent>(ent, out var speedMod))
        {
            var multiplier = ent.Comp.EnrageSpeedMultiplier;
            _movementSpeed.ChangeBaseSpeed(ent,
                speedMod.BaseWalkSpeed * multiplier,
                speedMod.BaseSprintSpeed * multiplier,
                speedMod.BaseAcceleration);
        }

        _appearance.SetData(ent.Owner, VisualLayers.Enabled, true);
        _npcActions.SetDelaySpeed(ent, ent.Comp.EnrageDelayModifier);
    }

    private float GetHealthRatio(EntityUid uid)
    {
        var totalDamage = _damage.GetTotalDamage(uid);
        if (!_threshold.TryGetThresholdForState(uid, MobState.Dead, out var maxHealth))
            return 1f;

        return 1f - (float)(totalDamage / maxHealth.Value.Double());
    }

    private void OnStomp(Entity<WendigoBossComponent> ent, ref WendigoStompActionEvent args)
    {
        args.Handled = true;
        if (_mobState.IsIncapacitated(ent))
            return;

        var range = ent.Comp.IsEnraged ? args.Range * 2 : args.Range;
        PerformStomp(ent, range);
    }

    private void PerformStomp(Entity<WendigoBossComponent> ent, float range)
    {
        var center = Transform(ent).Coordinates;
        var mapUid = _transform.GetMap(ent.Owner);
        if (mapUid == null)
            return;

        var maxWave = (int)Math.Ceiling(range);
        var waveDelay = 0.5f;

        for (int wave = 1; wave <= maxWave; wave++)
        {
            var currentWave = wave;
            Timer.Spawn(TimeSpan.FromSeconds((wave - 1) * waveDelay), () =>
            {
                if (!Exists(ent) || _mobState.IsDead(ent))
                    return;

                SpawnStompRing(ent, center, currentWave);
            });
        }
    }

    private void SpawnStompRing(Entity<WendigoBossComponent> ent, EntityCoordinates center, int wave)
    {
        int half = wave;
        for (int x = -half; x <= half; x++)
        {
            for (int y = -half; y <= half; y++)
            {
                if (Math.Abs(x) == half || Math.Abs(y) == half)
                {
                    var offset = new Vector2(x, y);
                    var spawnCoords = center.Offset(offset);

                    if (CanSpawnAt(spawnCoords)) Spawn(ent.Comp.SmokePrototype, spawnCoords);
                }
            }
        }
    }

    private void OnTeleport(Entity<WendigoBossComponent> ent, ref WendigoTeleportActionEvent args)
    {
        args.Handled = true;
        if (_mobState.IsIncapacitated(ent) || !Exists(args.Target))
            return;

        var target = args.Target;
        var targetCoords = Transform(target).Coordinates;
        var selfCoords = Transform(ent).Coordinates;

        Spawn(args.TeleportEffect, selfCoords);

        var teleportPos = FindTeleportPosition(targetCoords, args.TeleportDistance);
        if (teleportPos == null)
            return;

        _transform.SetCoordinates(ent, teleportPos.Value);
        Spawn(args.TeleportEffect, teleportPos.Value);
    }

    private EntityCoordinates? FindTeleportPosition(EntityCoordinates targetCoords, float distance)
    {
        for (int i = 0; i < 10; i++)
        {
            var angle = _random.NextFloat(0, MathF.PI * 2);
            var offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * distance;
            var testCoords = targetCoords.Offset(offset);
            var corrected = GetTileCenter(testCoords);

            if (CanSpawnAt(corrected))
                return corrected;
        }
        return null;
    }

    private void OnScream(Entity<WendigoBossComponent> ent, ref WendigoScreamActionEvent args)
    {
        args.Handled = true;
        if (_mobState.IsIncapacitated(ent))
            return;

        var pattern = _random.Next(0, 3);
        if (!Exists(ent) || _mobState.IsDead(ent))
            return;

        switch (pattern)
        {
            case 0:
                ShootSpread(ent.Owner, ent.Comp, args.SpreadCount);
                break;

            case 1:
                ShootAlternating(ent.Owner, ent.Comp, args.AlternatingCount);
                break;

            case 2:
                ShootStar(ent.Owner, ent.Comp, args.StarCount, args.StarRotationSpeed);
                break;
        }
    }

    private void ShootSpread(EntityUid shooter, WendigoBossComponent component, int count)
    {
        var mapUid = _transform.GetMap(shooter);
        if (mapUid == null)
            return;

        for (int i = 0; i < count; i++)
        {
            var angle = (i * MathF.PI * 2) / count;
            var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));

            var projectile = Spawn(component.DeathBoltPrototype, Transform(shooter).Coordinates);
            _gun.ShootProjectile(projectile, direction, Vector2.Zero, null, shooter,
                SharedGunSystem.ProjectileSpeed * 0.75f);
        }
    }

    private void ShootAlternating(EntityUid shooter, WendigoBossComponent component, int count)
    {
        var mapUid = _transform.GetMap(shooter);
        if (mapUid == null)
            return;

        var groups = new[] { 0f, MathF.PI / 8 };

        foreach (var offset in groups)
        {
            for (int i = 0; i < count; i++)
            {
                var angle = offset + (i * MathF.PI * 2) / count;
                var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));

                var projectile = Spawn(component.DeathBoltPrototype, Transform(shooter).Coordinates);
                _gun.ShootProjectile(projectile, direction, Vector2.Zero, null, shooter,
                    SharedGunSystem.ProjectileSpeed * 0.75f);
            }
        }
    }

    private void ShootStar(EntityUid shooter, WendigoBossComponent component, int points, float rotationSpeed)
    {
        var mapUid = _transform.GetMap(shooter);
        if (mapUid == null)
            return;

        var time = _timing.CurTime.Seconds;
        var rotation = time * rotationSpeed;

        for (int i = 0; i < points; i++)
        {
            var angle = rotation + (i * MathF.PI * 2) / points;
            var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));

            var projectile = Spawn(component.DeathBoltPrototype, Transform(shooter).Coordinates);
            _gun.ShootProjectile(projectile, direction, Vector2.Zero, null, shooter,
                SharedGunSystem.ProjectileSpeed * 0.75f);
        }
    }

    #region Utility Methods

    private EntityCoordinates GetTileCenter(EntityCoordinates coords)
    {
        var gridUid = _transform.GetGrid(coords);
        if (gridUid == null)
            return coords;

        if (!TryComp<MapGridComponent>(gridUid, out var grid))
            return coords;

        var tilePos = _map.CoordinatesToTile(gridUid.Value, grid, coords);
        return _map.GridTileToLocal(gridUid.Value, grid, tilePos);
    }

    private bool CanSpawnAt(EntityCoordinates coords)
    {
        var gridUid = _transform.GetGrid(coords);
        if (gridUid == null)
            return false;

        if (!TryComp<MapGridComponent>(gridUid, out var grid))
            return false;

        var tilePos = _map.CoordinatesToTile(gridUid.Value, grid, coords);
        if (!_map.TryGetTileRef(gridUid.Value, grid, tilePos, out var tileRef))
            return false;

        return !_turf.IsTileBlocked(tileRef, CollisionGroup.Impassable);
    }

    #endregion
}
