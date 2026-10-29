using System.Linq;
using System.Numerics;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Maps;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
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

public sealed partial class CrusherUpgradeEffectsSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private MobThresholdSystem _threshold = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private MovementModStatusSystem _movementMod = default!;
    [Dependency] private NpcFactionSystem _npcFaction = default!;
    [Dependency] private TurfSystem _turf = default!;

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

        // Broodmother
        SubscribeLocalEvent<CrusherBroodmotherTongueUpgradeComponent, AfterMarkerAttackedEvent>(OnBroodmotherTongueAfterMarker);

        // Legionnaire
        SubscribeLocalEvent<CrusherLegionnaireSpineUpgradeComponent, AfterMarkerAttackedEvent>(OnLegionnaireSpineAfterMarker);

        // White Wolf
        SubscribeLocalEvent<CrusherWhiteWolfEarUpgradeComponent, AfterMarkerAttackedEvent>(OnWhiteWolfEarAfterMarker);

        // Ice Demon
        SubscribeLocalEvent<CrusherIceDemonCubeUpgradeComponent, AfterMarkerAttackedEvent>(OnIceDemonCubeAfterMarker);

        // Polar Bear
        SubscribeLocalEvent<CrusherPolarBearPawUpgradeComponent, AfterMarkerAttackedEvent>(OnPolarBearPawAfterMarker);

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

        // Wendigo
        SubscribeLocalEvent<CrusherWendigoHornUpgradeComponent, MeleeHitEvent>(OnWendigoHornAttacked);

        // Frost Miner
        SubscribeLocalEvent<CrusherFrostMinerBlockUpgradeComponent, AfterMarkerAttackedEvent>(OnFrostMinerBlockAfterMarker);

        // The Thing
        SubscribeLocalEvent<CrusherTheThingGlobUpgradeComponent, MeleeHitEvent>(OnTheThingGlobAttacked);
        SubscribeLocalEvent<CrusherTheThingGlobUpgradeComponent, AfterMarkerAttackedEvent>(OnTheThingGlobAfterMarker);
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

    // Broodmother
    private void OnBroodmotherTongueAfterMarker(Entity<CrusherBroodmotherTongueUpgradeComponent> ent, ref AfterMarkerAttackedEvent args)
    {
        if (!_net.IsServer)
            return;

        if (!_random.Prob(ent.Comp.SpawnProb))
            return;

        var target = args.Target;
        var user = args.User;

        if (!Exists(target) || !Exists(user))
            return;

        var targetCoords = Transform(target).Coordinates;
        var userCoords = Transform(user).Coordinates;

        var dirs = new List<Direction>(ent.Comp.OffsetDirections);
        if (dirs.Count == 0)
            return;

        var spawnPositions = new List<EntityCoordinates>();
        if (CanSpawnAt(targetCoords) && targetCoords != userCoords)
            spawnPositions.Add(targetCoords);

        var needed = ent.Comp.SpawnCount - spawnPositions.Count;
        var availableDirs = new List<Direction>(dirs);

        while (needed > 0 && availableDirs.Count > 0)
        {
            var dir = _random.PickAndTake(availableDirs);

            var dirVector = dir.ToVec();
            var offsetPos = targetCoords.Offset(dirVector);

            if (CanSpawnAt(offsetPos) && offsetPos != userCoords)
            {
                spawnPositions.Add(offsetPos);
                needed--;
            }
        }

        foreach (var pos in spawnPositions)
        {
            Spawn(ent.Comp.SpawnProto, pos);
        }
    }

    // Legionnaire
    private void OnLegionnaireSpineAfterMarker(Entity<CrusherLegionnaireSpineUpgradeComponent> ent, ref AfterMarkerAttackedEvent args)
    {
        if (!_net.IsServer)
            return;

        if (!_random.Prob(ent.Comp.SpawnProb))
            return;

        var user = args.User;
        var target = args.Target;

        var spawnPos = FindSpawnPositionNear(Transform(user).Coordinates, 2f);
        if (spawnPos == null)
            return;

        var skull = Spawn(ent.Comp.SpawnProto, spawnPos.Value);
        if (TryComp<NpcFactionMemberComponent>(user, out var npcFaction))
        {
            _npcFaction.ClearFactions(skull, false);
            _npcFaction.AddFactions(skull, npcFaction.Factions);
        }
    }

    // White Wolf
    private void OnWhiteWolfEarAfterMarker(Entity<CrusherWhiteWolfEarUpgradeComponent> ent, ref AfterMarkerAttackedEvent args)
    {
        _movementMod.TryUpdateMovementSpeedModDuration(args.User, ent.Comp.EffectProto, ent.Comp.Duration, ent.Comp.Modifier);
    }

    // Ice Demon
    private void OnIceDemonCubeAfterMarker(Entity<CrusherIceDemonCubeUpgradeComponent> ent, ref AfterMarkerAttackedEvent args)
    {
        if (!_net.IsServer)
            return;

        if (!_random.Prob(ent.Comp.SpawnProb))
            return;

        var target = args.Target;
        var user = args.User;

        for (int i = 0; i < ent.Comp.SpawnCount; i++)
        {
            var spawnPos = FindSpawnPositionNear(Transform(target).Coordinates, 2f);
            if (spawnPos == null)
                continue;

            var afterimage = Spawn(ent.Comp.SpawnProto, spawnPos.Value);
            if (TryComp<NpcFactionMemberComponent>(user, out var npcFaction))
            {
                _npcFaction.ClearFactions(afterimage, false);
                _npcFaction.AddFactions(afterimage, npcFaction.Factions);
            }
        }
    }

    // Polar Bear
    private void OnPolarBearPawAfterMarker(Entity<CrusherPolarBearPawUpgradeComponent> ent, ref AfterMarkerAttackedEvent args)
    {
        if (args.Damage.GetTotal() <= 0)
            return;

        var totalDamage = _damage.GetTotalDamage(args.User);
        if (!_threshold.TryGetThresholdForState(args.User, ent.Comp.TargetState, out var threshold))
            return;

        var currentDamage = totalDamage.Float();
        var thresholdFloat = threshold.Value.Float();

        if (currentDamage >= thresholdFloat * ent.Comp.Threshold)
        {
            _damage.TryChangeDamage(args.Target, args.Damage, origin: args.User);
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

            _damage.TryChangeDamage(ent.Owner, args.Damage * entity.Comp.DamageMultiplier, origin: user);

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

            _transform.SetLocalRotation(barrier, perpendicularDirection.ToAngle(), barrierTransform);
            EnsureComp<PreventCollideComponent>(barrier).Uid = user;
        }
    }

    // Wendigo
    private void OnWendigoHornAttacked(Entity<CrusherWendigoHornUpgradeComponent> ent, ref MeleeHitEvent args)
    {
        bool works = false;
        foreach (var hitEnt in args.HitEntities)
        {
            if (HasComp<DamageMarkerComponent>(hitEnt))
            {
                works = true;
                break;
            }
        }

        if (!works) return;

        args.BonusDamage += args.BaseDamage - (args.BaseDamage * ent.Comp.DamageModifier);
    }

    // Frost Miner
    private void OnFrostMinerBlockAfterMarker(Entity<CrusherFrostMinerBlockUpgradeComponent> ent, ref AfterMarkerAttackedEvent args)
    {
        _movementMod.TryUpdateMovementSpeedModDuration(args.Target, ent.Comp.EffectProto, ent.Comp.Duration, ent.Comp.SpeedModifdier);
    }

    // The Thing
    private void OnTheThingGlobAttacked(Entity<CrusherTheThingGlobUpgradeComponent> ent, ref MeleeHitEvent args)
    {
        if (args.HitEntities.Count == 0)
            return;

        bool hasLivingTarget = false;
        foreach (var hitEnt in args.HitEntities)
        {
            if (HasComp<MobStateComponent>(hitEnt) && !_mobState.IsDead(hitEnt))
            {
                hasLivingTarget = true;
                break;
            }
        }

        if (!hasLivingTarget)
            return;

        HealDamageEvenly(args.User, ent.Comp.BaseHealAmount);
    }

    private void OnTheThingGlobAfterMarker(Entity<CrusherTheThingGlobUpgradeComponent> ent, ref AfterMarkerAttackedEvent args)
    {
        if (args.Damage.GetTotal() <= 0)
            return;

        if (!HasComp<MobStateComponent>(args.Target) || _mobState.IsDead(args.Target))
            return;

        HealDamageEvenly(args.User, ent.Comp.MarkHealAmount);
    }

    #region Utility Methods

    private EntityCoordinates? FindSpawnPositionNear(EntityCoordinates center, float maxDistance)
    {
        for (int i = 0; i < 10; i++)
        {
            var angle = _random.NextDouble() * Math.PI * 2;
            var distance = _random.NextFloat(1f, maxDistance);
            var offset = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * distance;
            var testCoords = center.Offset(offset);
            if (CanSpawnAt(testCoords))
                return testCoords;
        }
        return null;
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

    private void HealDamageEvenly(EntityUid entity, FixedPoint2 healAmount)
    {
        if (healAmount <= 0)
            return;

        if (!TryComp<DamageableComponent>(entity, out var damageable))
            return;

        var currentDamage = _damage.GetAllDamage((entity, damageable));
        if (currentDamage.Empty || currentDamage.GetTotal() <= 0)
            return;

        var remainingHeal = healAmount;
        var healSpec = new DamageSpecifier();

        var damageTypes = currentDamage.DamageDict
            .Where(x => x.Value > 0)
            .Select(x => x.Key)
            .ToList();

        if (damageTypes.Count == 0)
            return;

        var damageClone = currentDamage.Clone();
        while (remainingHeal > 0 && damageTypes.Count > 0)
        {
            damageTypes.RemoveAll(type =>
            {
                if (!damageClone.DamageDict.TryGetValue(type, out var current))
                    return true;

                return current <= 0;
            });

            if (damageTypes.Count == 0)
                break;

            var type = _random.Pick(damageTypes);
            var currentValue = damageClone.DamageDict[type];

            var healThis = FixedPoint2.Min(FixedPoint2.New(1), currentValue);
            healThis = FixedPoint2.Min(healThis, remainingHeal);

            if (healThis > 0)
            {
                if (healSpec.DamageDict.ContainsKey(type))
                    healSpec.DamageDict[type] -= healThis;
                else
                    healSpec.DamageDict.Add(type, -healThis);

                damageClone.DamageDict[type] -= healThis;
                remainingHeal -= healThis;

                if (damageClone.DamageDict[type] <= 0)
                    damageTypes.Remove(type);
            }
        }

        if (!healSpec.Empty) _damage.TryChangeDamage(entity, healSpec, true, false);
    }

    #endregion
}
