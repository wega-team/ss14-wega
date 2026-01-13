using System.Linq;
using System.Numerics;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Physics;
using Content.Shared.Projectiles;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Marker;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Weapons.Misc.Upgrades;

public sealed class CrusherUpgradeEffectsSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly MobThresholdSystem _threshold = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    private static readonly ProtoId<TagPrototype> SlowImmune = "SlowImmune";
    private static readonly ProtoId<TagPrototype> StunImmune = "StunImmune";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CrusherLegionSkullUpgradeComponent, GunRefreshModifiersEvent>(OnLegionFireRateRefresh);
        SubscribeLocalEvent<CrusherGoliathTentacleUpgradeComponent, MarkerAttackAttemptEvent>(OnGoliathMarkerAttack);
        SubscribeLocalEvent<CrusherGoliathTentacleUpgradeComponent, MeleeHitEvent>(OnGoliathAttacked);
        SubscribeLocalEvent<CrusherWatcherWingUpgradeComponent, GunShotEvent>(OnWatcherWingGunShot);
        SubscribeLocalEvent<CrusherMagmaWingUpgradeComponent, AfterMarkerAttackedEvent>(OnMagmaWingAfterMarker);
        SubscribeLocalEvent<CrusherMagmaWingUpgradeComponent, GunShotEvent>(OnMagmaWingGunShot);
        SubscribeLocalEvent<CrusherEyeBloodDrunkMinerUpgradeComponent, AfterMarkerAttackedEvent>(OnEyeBDMAfterMarker);
        SubscribeLocalEvent<CrusherAshDrakeSpikeUpgradeComponent, AfterMarkerAttackedEvent>(OnAshDrakeSpikeAfterMarker);
        SubscribeLocalEvent<CrusherDemonClawsUpgradeComponent, MarkerAttackAttemptEvent>(OnDemonClawsMarkerAttack);
        SubscribeLocalEvent<CrusherDemonClawsUpgradeComponent, MeleeHitEvent>(OnDemonClawsAttacked);
        SubscribeLocalEvent<CrusherBlasterTubesUpgradeComponent, AfterMarkerAttackedEvent>(OnBlasterTubesAfterMarker);
        SubscribeLocalEvent<CrusherBlasterTubesUpgradeComponent, GunRefreshModifiersEvent>(OnBlasterTubesRefresh);
        SubscribeLocalEvent<CrusherBlasterTubesUpgradeComponent, GunShotEvent>(OnBlasterTubesGunShot);
        SubscribeLocalEvent<CrusherVortexTalismanUpgradeComponent, AfterMarkerAttackedEvent>(OnVortexTalismanAfterMarker);
    }

    // Legion
    private void OnLegionFireRateRefresh(Entity<CrusherLegionSkullUpgradeComponent> ent, ref GunRefreshModifiersEvent args)
        => args.FireRate *= ent.Comp.Coefficient;

    // Goliath
    private void OnGoliathMarkerAttack(Entity<CrusherGoliathTentacleUpgradeComponent> ent, ref MarkerAttackAttemptEvent args)
    {
        if (!TryComp<DamageableComponent>(args.User, out var damageable) || damageable.TotalDamage <= 0
            || !_threshold.TryGetThresholdForState(args.User, ent.Comp.TargetState, out var threshold))
            return;

        var currentDamage = damageable.TotalDamage.Float();
        var thresholdFloat = threshold.Value.Float();
        if (currentDamage >= thresholdFloat)
            return;

        var bonus = ent.Comp.MaxCoefficient * (currentDamage / thresholdFloat);

        bonus = Math.Min(bonus, ent.Comp.MaxCoefficient);
        args.DamageModifier += bonus;
    }

    private void OnGoliathAttacked(Entity<CrusherGoliathTentacleUpgradeComponent> ent, ref MeleeHitEvent args)
    {
        if (!TryComp<DamageableComponent>(args.User, out var damageable) || damageable.TotalDamage <= 0
            || !_threshold.TryGetThresholdForState(args.User, ent.Comp.TargetState, out var threshold))
            return;

        var currentDamage = damageable.TotalDamage.Float();
        var thresholdFloat = threshold.Value.Float();
        if (currentDamage >= thresholdFloat)
            return;

        var bonus = ent.Comp.MaxCoefficient * (currentDamage / thresholdFloat);

        bonus = Math.Min(bonus, ent.Comp.MaxCoefficient);
        args.BonusDamage += args.BaseDamage * bonus;
    }

    // Watcher
    private void OnWatcherWingGunShot(Entity<CrusherWatcherWingUpgradeComponent> ent, ref GunShotEvent args)
    {
        foreach (var (ammo, _) in args.Ammo)
        {
            if (ammo == null)
                return;

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
            if (!ent.Comp.Active)
                return;

            if (TryComp<ProjectileComponent>(ammo, out var projectile))
            {
                projectile.Damage += ent.Comp.Damage;
                ent.Comp.Active = false;
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
        var ents = _lookup.GetEntitiesInRange<DamageableComponent>(Transform(entity).Coordinates, entity.Comp.DamageRadius)
            .Where(e => e.Owner != target && e.Owner != user);

        foreach (var ent in ents)
        {
            _damage.TryChangeDamage(ent.Owner, args.Damage * entity.Comp.DamageMultiplier);

            var entityPos = _transform.GetWorldPosition(entity);
            var entPos = _transform.GetWorldPosition(ent.Owner);
            var direction = (entPos - entityPos).Normalized();

            _throwing.TryThrow(ent, direction);
        }
    }

    // Bubblegum
    private void OnDemonClawsMarkerAttack(Entity<CrusherDemonClawsUpgradeComponent> ent, ref MarkerAttackAttemptEvent args)
        => args.HealModifier += ent.Comp.DamageMultiplier * 4; // Allowance for the fact that the heal comes from the attack.

    private void OnDemonClawsAttacked(Entity<CrusherDemonClawsUpgradeComponent> ent, ref MeleeHitEvent args)
    {
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
            if (!ent.Comp.Active)
                return;

            if (TryComp<ProjectileComponent>(ammo, out var projectile))
            {
                projectile.Damage += ent.Comp.Damage;
                ent.Comp.Active = false;
            }
        }
    }

    private void OnVortexTalismanAfterMarker(Entity<CrusherVortexTalismanUpgradeComponent> ent, ref AfterMarkerAttackedEvent args)
    {
        if (!_net.IsServer)
            return;

        var user = args.User;

        var userTransform = Transform(user);
        var direction = userTransform.LocalRotation.ToWorldVec().Normalized();
        var perpendicularDirection = new Vector2(-direction.Y, direction.X);

        var spawnedCount = 0;
        for (int i = -1; i <= 1 && spawnedCount < ent.Comp.SpawnCount; i++)
        {
            var barrier = Spawn(ent.Comp.SpawnProto, userTransform.Coordinates);

            var barrierTransform = Transform(barrier);
            barrierTransform.LocalRotation = perpendicularDirection.ToAngle();
            if (i != 0)
            {
                var offset = perpendicularDirection * (i * 1.5f);
                _transform.SetLocalPositionNoLerp(barrier, userTransform.LocalPosition + offset);
            }

            EnsureComp<PreventCollideComponent>(barrier).Uid = user;
            spawnedCount++;
        }
    }
}
