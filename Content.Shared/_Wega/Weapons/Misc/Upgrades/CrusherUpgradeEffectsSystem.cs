using System.Linq;
using System.Numerics;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Maps;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Physics;
using Content.Shared.Projectiles;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Marker;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Weapons.Misc.Upgrades;

public sealed class CrusherUpgradeEffectsSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobThresholdSystem _threshold = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    private static readonly ProtoId<TagPrototype> SlowImmune = "SlowImmune";
    private static readonly ProtoId<TagPrototype> StunImmune = "StunImmune";

    public override void Initialize()
    {
        base.Initialize();

        // Legion
        SubscribeLocalEvent<CrusherLegionSkullUpgradeComponent, GunRefreshModifiersEvent>(OnLegionFireRateRefresh);

        // Goliath
        SubscribeLocalEvent<CrusherGoliathTentacleUpgradeComponent, MarkerAttackAttemptEvent>(OnGoliathMarkerAttack);
        SubscribeLocalEvent<CrusherGoliathTentacleUpgradeComponent, MeleeHitEvent>(OnGoliathAttacked);

        // Ancient Goliath
        SubscribeLocalEvent<CrusherAncientGoliathTentacleUpgradeComponent, MarkerAttackAttemptEvent>(OnAncientGoliathMarkerAttack);
        SubscribeLocalEvent<CrusherAncientGoliathTentacleUpgradeComponent, MeleeHitEvent>(OnAncientGoliathAttacked);

        // Watcher
        SubscribeLocalEvent<CrusherWatcherWingUpgradeComponent, GunShotEvent>(OnWatcherWingGunShot);

        // Magma Watcher
        SubscribeLocalEvent<CrusherMagmaWingUpgradeComponent, AfterMarkerAttackedEvent>(OnMagmaWingAfterMarker);
        SubscribeLocalEvent<CrusherMagmaWingUpgradeComponent, GunShotEvent>(OnMagmaWingGunShot);

        // Marrow Weaver
        SubscribeLocalEvent<CrusherPoisonFangUpgradeComponent, AfterMarkerAttackedEvent>(OnPoisonFangAfterMarker);

        // Frostbite Weaver
        SubscribeLocalEvent<CrusherFrostGlandUpgradeComponent, GunShotEvent>(OnFrostGlandGunShot);

        // Blood Drunk Miner
        SubscribeLocalEvent<CrusherEyeBloodDrunkMinerUpgradeComponent, AfterMarkerAttackedEvent>(OnEyeBDMAfterMarker);

        // Ash Drake
        SubscribeLocalEvent<CrusherAshDrakeSpikeUpgradeComponent, AfterMarkerAttackedEvent>(OnAshDrakeSpikeAfterMarker);

        // Bubblegum
        SubscribeLocalEvent<CrusherDemonClawsUpgradeComponent, MarkerAttackAttemptEvent>(OnDemonClawsMarkerAttack);
        SubscribeLocalEvent<CrusherDemonClawsUpgradeComponent, MeleeHitEvent>(OnDemonClawsAttacked);

        // Colossus
        SubscribeLocalEvent<CrusherBlasterTubesUpgradeComponent, AfterMarkerAttackedEvent>(OnBlasterTubesAfterMarker);
        SubscribeLocalEvent<CrusherBlasterTubesUpgradeComponent, GunRefreshModifiersEvent>(OnBlasterTubesRefresh);
        SubscribeLocalEvent<CrusherBlasterTubesUpgradeComponent, GunShotEvent>(OnBlasterTubesGunShot);

        // Hierophant
        SubscribeLocalEvent<CrusherVortexTalismanUpgradeComponent, AfterMarkerAttackedEvent>(OnVortexTalismanAfterMarker);
    }

    // Legion
    private void OnLegionFireRateRefresh(Entity<CrusherLegionSkullUpgradeComponent> ent, ref GunRefreshModifiersEvent args)
        => args.FireRate *= ent.Comp.Coefficient;

    // Goliath
    private void OnGoliathMarkerAttack(Entity<CrusherGoliathTentacleUpgradeComponent> ent, ref MarkerAttackAttemptEvent args)
    {
        if (!HasComp<MobThresholdsComponent>(args.User))
            return;

        if (!TryComp<DamageableComponent>(args.User, out var damageable))
            return;

        var totalDamage = _damage.GetTotalDamage((args.User, damageable));
        if (totalDamage <= 0 || !_threshold.TryGetThresholdForState(args.User, ent.Comp.TargetState, out var threshold))
            return;

        var currentDamage = totalDamage.Float();
        var thresholdFloat = threshold.Value.Float();
        if (currentDamage >= thresholdFloat)
            return;

        var bonus = ent.Comp.MaxCoefficient * (currentDamage / thresholdFloat);

        bonus = Math.Min(bonus, ent.Comp.MaxCoefficient);
        args.DamageModifier += bonus;
    }

    private void OnGoliathAttacked(Entity<CrusherGoliathTentacleUpgradeComponent> ent, ref MeleeHitEvent args)
    {
        if (!HasComp<MobThresholdsComponent>(args.User))
            return;

        if (!TryComp<DamageableComponent>(args.User, out var damageable))
            return;

        var totalDamage = _damage.GetTotalDamage((args.User, damageable));
        if (totalDamage <= 0 || !_threshold.TryGetThresholdForState(args.User, ent.Comp.TargetState, out var threshold))
            return;

        var currentDamage = totalDamage.Float();
        var thresholdFloat = threshold.Value.Float();
        if (currentDamage >= thresholdFloat)
            return;

        var bonus = ent.Comp.MaxCoefficient * (currentDamage / thresholdFloat);

        bonus = Math.Min(bonus, ent.Comp.MaxCoefficient);
        args.BonusDamage += args.BaseDamage * bonus;
    }

    // Ancient Goliath
    private void OnAncientGoliathMarkerAttack(Entity<CrusherAncientGoliathTentacleUpgradeComponent> ent, ref MarkerAttackAttemptEvent args)
    {
        if (!HasComp<MobThresholdsComponent>(args.Target))
            return;

        if (!TryComp<DamageableComponent>(args.Target, out var damageable))
            return;

        var totalDamage = _damage.GetTotalDamage((args.Target, damageable));
        if (!_threshold.TryGetThresholdForState(args.Target, MobState.Dead, out var threshold))
            return;

        if (threshold - threshold * ent.Comp.HealModifier < totalDamage)
            return;

        args.DamageModifier += ent.Comp.Coefficient;
    }

    private void OnAncientGoliathAttacked(Entity<CrusherAncientGoliathTentacleUpgradeComponent> ent, ref MeleeHitEvent args)
    {
        if (args.HitEntities.Count == 0)
            return;

        bool correct = false;
        foreach (var hitEnt in args.HitEntities)
        {
            if (!HasComp<MobThresholdsComponent>(hitEnt))
                return;

            if (!TryComp<DamageableComponent>(hitEnt, out var damageable))
                continue;

            var totalDamage = _damage.GetTotalDamage((hitEnt, damageable));
            if (!_threshold.TryGetThresholdForState(hitEnt, MobState.Dead, out var threshold))
                continue;

            if (threshold - threshold * ent.Comp.HealModifier < totalDamage)
                continue;

            correct = true;
            break;
        }

        if (!correct)
            return;

        args.BonusDamage += args.BaseDamage * ent.Comp.Coefficient;
    }

    // Watcher
    private void OnWatcherWingGunShot(Entity<CrusherWatcherWingUpgradeComponent> ent, ref GunShotEvent args)
    {
        foreach (var (ammo, _) in args.Ammo)
        {
            if (ammo == null || !HasComp<ProjectileComponent>(ammo.Value))
                continue;

            var timer = EnsureComp<ProjectileTimerResetsComponent>(ammo.Value);
            timer.ResetsTime = ent.Comp.ResetsTime;
        }
    }

    // Magma Watcher
    private void OnMagmaWingAfterMarker(Entity<CrusherMagmaWingUpgradeComponent> ent, ref AfterMarkerAttackedEvent args)
        => ent.Comp.Active = true;

    private void OnMagmaWingGunShot(Entity<CrusherMagmaWingUpgradeComponent> ent, ref GunShotEvent args)
    {
        foreach (var (ammo, _) in args.Ammo)
        {
            if (ammo == null || !ent.Comp.Active)
                continue;

            if (TryComp<ProjectileComponent>(ammo, out var projectile))
            {
                projectile.Damage += ent.Comp.Damage;
                ent.Comp.Active = false;
            }
        }
    }

    // Marrow Weaver
    private void OnPoisonFangAfterMarker(Entity<CrusherPoisonFangUpgradeComponent> ent, ref AfterMarkerAttackedEvent args)
    {
        EnsureComp<IncreasedDamageComponent>(args.Target).ActiveInterval = TimeSpan.FromSeconds(ent.Comp.Interval);
        Comp<IncreasedDamageComponent>(args.Target).DamageModifier = ent.Comp.DamageModifier;
    }

    // Frostbite Weaver
    private void OnFrostGlandGunShot(Entity<CrusherFrostGlandUpgradeComponent> ent, ref GunShotEvent args)
    {
        foreach (var (ammo, _) in args.Ammo)
        {
            if (ammo == null || !HasComp<ProjectileComponent>(ammo.Value))
                continue;

            if (TryComp<DamageMarkerOnCollideComponent>(ammo, out var marker) && !marker.Weakening)
            {
                marker.Weakening = true;
                marker.WeakeningModifier = ent.Comp.DamageModifier;
            }
        }
    }

    // Blood Drunk Miner
    private void OnEyeBDMAfterMarker(Entity<CrusherEyeBloodDrunkMinerUpgradeComponent> ent, ref AfterMarkerAttackedEvent args)
    {
        var time = TimeSpan.FromSeconds(1);
        var user = args.User;

        bool isStunImmuned = false;
        bool isSlowImmuned = false;
        if (!_tag.HasTag(user, StunImmune))
        {
            isStunImmuned = _tag.TryAddTag(user, StunImmune);
        }

        if (!_tag.HasTag(user, SlowImmune))
        {
            isSlowImmuned = _tag.TryAddTag(user, SlowImmune);
        }

        Timer.Spawn(time, () =>
        {
            if (isStunImmuned) _tag.RemoveTag(user, StunImmune);
            if (isSlowImmuned) _tag.RemoveTag(user, SlowImmune);
        });
    }

    // Ash Drake
    private void OnAshDrakeSpikeAfterMarker(Entity<CrusherAshDrakeSpikeUpgradeComponent> entity, ref AfterMarkerAttackedEvent args)
    {
        var user = args.User;
        var target = args.Target;
        if (!Exists(target))
            return;

        var ents = _lookup.GetEntitiesInRange<DamageableComponent>(Transform(target).Coordinates, entity.Comp.DamageRadius)
            .Where(e => e.Owner != target && e.Owner != user).ToList();

        foreach (var ent in ents)
        {
            // Only for mobs.
            if (!HasComp<MobStateComponent>(ent))
                continue;

            _damage.TryChangeDamage(ent.Owner, args.Damage * entity.Comp.DamageMultiplier);

            var targetPos = _transform.GetWorldPosition(target);
            var entPos = _transform.GetWorldPosition(ent.Owner);
            var direction = (entPos - targetPos).Normalized();

            var randomAngle = new Angle(_random.NextFloat(-0.2f, 0.2f));
            direction = randomAngle.RotateVec(direction);

            _throwing.TryThrow(ent, direction);
        }
    }

    // Bubblegum
    private void OnDemonClawsMarkerAttack(Entity<CrusherDemonClawsUpgradeComponent> ent, ref MarkerAttackAttemptEvent args)
        => args.HealModifier += ent.Comp.DamageMultiplier * 4; // Allowance for the fact that the heal comes from the attack.

    private void OnDemonClawsAttacked(Entity<CrusherDemonClawsUpgradeComponent> ent, ref MeleeHitEvent args)
    {
        bool alive = false;
        foreach (var hitEnt in args.HitEntities)
        {
            if (HasComp<MobStateComponent>(hitEnt) && !_mobState.IsDead(hitEnt))
            {
                alive = true;
                break;
            }
        }

        if (!alive) return;

        args.BonusDamage = args.BaseDamage * ent.Comp.DamageMultiplier;
        if (TryComp<LeechOnMarkerComponent>(ent, out var leech))
        {
            var leechAmount = leech.Leech * ent.Comp.DamageMultiplier;
            _damage.TryChangeDamage(args.User, leechAmount, true, false, origin: ent.Owner);
        }
    }

    // Colossus
    private void OnBlasterTubesAfterMarker(Entity<CrusherBlasterTubesUpgradeComponent> ent, ref AfterMarkerAttackedEvent args)
        => ent.Comp.Active = true;

    private void OnBlasterTubesRefresh(Entity<CrusherBlasterTubesUpgradeComponent> ent, ref GunRefreshModifiersEvent args)
        => args.ProjectileSpeed *= ent.Comp.Coefficient;

    private void OnBlasterTubesGunShot(Entity<CrusherBlasterTubesUpgradeComponent> ent, ref GunShotEvent args)
    {
        foreach (var (ammo, _) in args.Ammo)
        {
            if (ammo == null || !ent.Comp.Active)
                return;

            if (TryComp<ProjectileComponent>(ammo, out var projectile))
            {
                projectile.Damage += ent.Comp.Damage;
                ent.Comp.Active = false;
            }
        }
    }

    // Hierophant
    private void OnVortexTalismanAfterMarker(Entity<CrusherVortexTalismanUpgradeComponent> ent, ref AfterMarkerAttackedEvent args)
    {
        if (!_net.IsServer)
            return;

        var user = args.User;
        var userTransform = Transform(user);
        var direction = userTransform.LocalRotation.ToWorldVec().Normalized();
        var perpendicularDirection = new Vector2(-direction.Y, direction.X);

        for (int i = -1; i <= 1; i++)
        {
            var offset = perpendicularDirection * i;
            var spawnCoords = userTransform.Coordinates.Offset(offset);

            if (!CanSpawnAt(spawnCoords))
                continue;

            var barrier = Spawn(ent.Comp.SpawnProto, spawnCoords);
            var barrierTransform = Transform(barrier);
            barrierTransform.LocalRotation = perpendicularDirection.ToAngle();

            EnsureComp<PreventCollideComponent>(barrier).Uid = user;
        }
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
}
