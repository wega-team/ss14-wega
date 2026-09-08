using System.Linq;
using System.Numerics;
using Content.Server.Cargo.Components;
using Content.Server.Humanoid.Components;
using Content.Server.Lavaland.Mobs.Components;
using Content.Server.Polymorph.Systems;
using Content.Server.Surgery;
using Content.Shared.Achievements;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Destructible;
using Content.Shared.FixedPoint;
using Content.Shared.Height;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Lavaland.Components;
using Content.Shared.Lavaland.Events;
using Content.Shared.Maps;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Content.Shared.Visuals;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Lavaland.Mobs;

public sealed partial class LegionSystem : EntitySystem
{
    [Dependency] private SharedAchievementsSystem _achievement = default!;
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private BossMusicSystem _bossMusic = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private PolymorphSystem _polymorph = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SurgerySystem _surgery = default!;
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private TurfSystem _turf = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LegionCoreComponent, MapInitEvent>(OnLegionCoreMapInit);
        SubscribeLocalEvent<LegionCoreComponent, UseInHandEvent>(OnLegionCoreUse);
        SubscribeLocalEvent<LegionCoreComponent, AfterInteractEvent>(OnLegionCoreInteract);

        SubscribeLocalEvent<LegionFaunaComponent, LegionSummonSkullAction>(OnLegionSummon);
        SubscribeLocalEvent<LegionReversibleComponent, MobStateChangedEvent>(OnReversible);

        SubscribeLocalEvent<LegionnaireComponent, MobStateChangedEvent>(OnLegionnaireMobStateChanged);
        SubscribeLocalEvent<LegionnaireComponent, LegionnaireChargeActionEvent>(OnLegionnaireCharge);
        SubscribeLocalEvent<LegionnaireComponent, LegionnaireDetachHeadActionEvent>(OnLegionnaireDetachHead);
        SubscribeLocalEvent<LegionnaireComponent, LegionnaireBoneFireActionEvent>(OnLegionnaireBoneFire);
        SubscribeLocalEvent<LegionnaireComponent, LegionnaireSmokeActionEvent>(OnLegionnaireSmoke);

        SubscribeLocalEvent<LegionnaireHeadComponent, DestructionEventArgs>(OnHeadDeath);

        SubscribeLocalEvent<LegionBossComponent, MapInitEvent>(OnMegaLegionMapInit);
        SubscribeLocalEvent<LegionBossComponent, MegaLegionAction>(OnMegaLegionAction);
        SubscribeLocalEvent<LegionBossComponent, MegafaunaKilledEvent>(OnMegaLegionKilled);
        SubscribeLocalEvent<LegionSplitComponent, MegafaunaKilledEvent>(OnSplitKilled);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<LegionBossComponent>();
        while (query.MoveNext(out _, out var component))
        {
            if (_timing.CurTime >= component.NextStateSwitchTime)
            {
                component.CurrentState = component.CurrentState == LegionState.Summoning
                    ? LegionState.Charging : LegionState.Summoning;

                component.NextStateSwitchTime = _timing.CurTime + TimeSpan.FromSeconds(component.StateSwitchInterval);
            }
        }

        var coreQuery = EntityQueryEnumerator<LegionCoreComponent>();
        while (coreQuery.MoveNext(out var uid, out var component))
        {
            if (component.AlwaysActive || !component.Active)
                continue;

            if (_timing.CurTime >= component.ActiveEndTime)
            {
                if (TryComp<StaticPriceComponent>(uid, out var price))
                    price.Price = 0; // He's useless now.

                _appearance.SetData(uid, VisualLayers.Enabled, false);
                component.Active = false;
            }
        }
    }

    #region Legion Core

    private void OnLegionCoreMapInit(EntityUid uid, LegionCoreComponent component, MapInitEvent args)
    {
        component.ActiveEndTime = _timing.CurTime + component.ActiveInterval;
        _appearance.SetData(uid, VisualLayers.Enabled, true);
    }

    private void OnLegionCoreUse(Entity<LegionCoreComponent> ent, ref UseInHandEvent args)
    {
        if (!ent.Comp.Active || !HasComp<DamageableComponent>(args.User))
        {
            _popup.PopupEntity(Loc.GetString("legion-core-interact-failed", ("name", Name(ent))), args.User, args.User);
            return;
        }

        PerformCoreHeal(args.User, ent);
    }

    private void OnLegionCoreInteract(Entity<LegionCoreComponent> ent, ref AfterInteractEvent args)
    {
        if (!ent.Comp.Active || !HasComp<DamageableComponent>(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("legion-core-interact-failed", ("name", Name(ent))), args.User, args.User);
            return;
        }

        PerformCoreHeal(args.Target.Value, ent);
    }

    public void PerformCoreHeal(EntityUid target, Entity<LegionCoreComponent> ent)
    {
        _damage.TryChangeDamage(target, ent.Comp.HealAmount, true, false);
        foreach (var internalDam in ent.Comp.HealInternals)
            _surgery.TryRemoveInternalDamage(target, internalDam);

        _popup.PopupEntity(Loc.GetString("legion-core-interact-healed"), target, target);
        QueueDel(ent);
    }

    #endregion

    #region Basic Legion

    private void OnLegionSummon(Entity<LegionFaunaComponent> entity, ref LegionSummonSkullAction args)
    {
        args.Handled = true;
        // To prevent NPS from spamming skulls without stopping.
        if (_mobState.IsIncapacitated(entity) || _mobState.IsIncapacitated(args.Target))
            return;

        var coords = Transform(entity).Coordinates;
        for (var i = 0; i < args.MaxSpawns; i++)
            Spawn(args.EntityId, coords);
    }

    private void OnReversible(Entity<LegionReversibleComponent> entity, ref MobStateChangedEvent args)
    {
        if (!entity.Comp.CanReversible)
            return;

        if (args.NewMobState != MobState.Critical && args.NewMobState != MobState.Dead)
            return;

        // For skipping corpse on spawn.
        if (TryComp<RandomHumanoidAppearanceComponent>(entity, out var random) && !random.RandomizeName)
            return;

        var legions = _lookup.GetEntitiesInRange<LegionFaunaComponent>(Transform(entity).Coordinates, 6f, LookupFlags.Uncontained).ToList();
        if (legions.Count == 0)
            return;

        var legion = legions.First();
        // lol                                                                                                 ↓ ur so big bozo
        var polymorphProto = TryComp<HumanoidProfileComponent>(entity, out var humanoid) && humanoid.Height < 160 || HasComp<SmallHeightComponent>(entity)
            ? legion.Comp.DwarfPolymorph : legion.Comp.BasePolymorph;

        _bossMusic.StopTracker(entity); // <- If you lost to Legion.
        _polymorph.PolymorphEntity(entity, polymorphProto);
    }

    #endregion

    #region Legionnaire

    private void OnLegionnaireMobStateChanged(Entity<LegionnaireComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
        {
            DeleteHead(ent);
            _appearance.SetData(ent, VisualLayers.Enabled, false);
        }
        else
        {
            _appearance.SetData(ent, VisualLayers.Enabled, true);
        }
    }

    private void OnLegionnaireCharge(Entity<LegionnaireComponent> ent, ref LegionnaireChargeActionEvent args)
    {
        args.Handled = true;
        if (_mobState.IsIncapacitated(ent) || !Exists(args.Target))
            return;

        var target = args.Target;
        var xform = Transform(ent);
        var targetCoords = Transform(target).Coordinates;

        var direction = (targetCoords.Position - xform.Coordinates.Position).Normalized();
        var throwTarget = xform.Coordinates.Offset(direction * args.ChargeDistance);

        _throwing.TryThrow(ent, throwTarget, args.ChargeForce);
    }

    private void OnLegionnaireDetachHead(Entity<LegionnaireComponent> ent, ref LegionnaireDetachHeadActionEvent args)
    {
        args.Handled = true;
        if (_mobState.IsIncapacitated(ent))
            return;

        if (ent.Comp.HeadEntity != null && Exists(ent.Comp.HeadEntity.Value))
        {
            ReturnHead(ent);
            return;
        }

        var head = Spawn(args.HeadPrototype, Transform(ent).Coordinates);
        var headComp = EnsureComp<LegionnaireHeadComponent>(head);
        headComp.OwnerLegionnaire = ent.Owner;

        ent.Comp.HeadEntity = head;

        _appearance.SetData(ent, VisualLayers.Enabled, false);
    }

    private void ReturnHead(Entity<LegionnaireComponent> ent)
    {
        if (ent.Comp.HeadEntity == null)
            return;

        DeleteHead(ent);
        _appearance.SetData(ent, VisualLayers.Enabled, true);
    }

    private void DeleteHead(Entity<LegionnaireComponent> ent)
    {
        if (ent.Comp.HeadEntity == null)
            return;

        if (Exists(ent.Comp.HeadEntity.Value))
            QueueDel(ent.Comp.HeadEntity.Value);

        ent.Comp.HeadEntity = null;
    }

    private void OnLegionnaireBoneFire(Entity<LegionnaireComponent> ent, ref LegionnaireBoneFireActionEvent args)
    {
        args.Handled = true;
        if (_mobState.IsIncapacitated(ent))
            return;

        if (ent.Comp.BoneCampfire != null && Exists(ent.Comp.BoneCampfire))
        {
            var selfCoords = Transform(ent).Coordinates;
            var fireCoords = Transform(ent.Comp.BoneCampfire.Value).Coordinates;

            _transform.SetCoordinates(ent, fireCoords);
            _transform.SetCoordinates(ent.Comp.BoneCampfire.Value, selfCoords);

            return;
        }

        var boneFire = Spawn(args.BoneFirePrototype, Transform(ent).Coordinates);
        ent.Comp.BoneCampfire = boneFire;
    }

    private void OnLegionnaireSmoke(Entity<LegionnaireComponent> ent, ref LegionnaireSmokeActionEvent args)
    {
        args.Handled = true;
        if (_mobState.IsIncapacitated(ent))
            return;

        if (ent.Comp.HeadEntity != null && Exists(ent.Comp.HeadEntity.Value))
        {
            var headCoords = Transform(ent.Comp.HeadEntity.Value).Coordinates;
            Spawn(args.SmokePrototype, headCoords);
        }
        else
        {
            Spawn(args.SmokePrototype, Transform(ent).Coordinates);
        }
    }

    private void OnHeadDeath(Entity<LegionnaireHeadComponent> ent, ref DestructionEventArgs args)
    {
        if (!TryComp<LegionnaireComponent>(ent.Comp.OwnerLegionnaire, out var legionnaire))
            return;

        ReturnHead((ent.Comp.OwnerLegionnaire, legionnaire));
    }

    #endregion

    private void OnMegaLegionMapInit(EntityUid uid, LegionBossComponent component, MapInitEvent args)
    {
        component.NextStateSwitchTime = _timing.CurTime + TimeSpan.FromSeconds(component.StateSwitchInterval);
        component.NextSummonTime = _timing.CurTime;
        component.NextChargeTime = _timing.CurTime;
    }

    private void OnMegaLegionAction(EntityUid uid, LegionBossComponent component, ref MegaLegionAction args)
    {
        // To prevent NPS from spamming without stopping.
        if (_mobState.IsIncapacitated(uid) || _mobState.IsIncapacitated(args.Target))
            return;

        switch (component.CurrentState)
        {
            case LegionState.Summoning:
                UpdateSummoningState(uid, component);
                break;
            case LegionState.Charging:
                UpdateChargingState(uid, component, args.Target);
                break;
        }
    }

    #region Summoning State

    private void UpdateSummoningState(EntityUid uid, LegionBossComponent component)
    {
        if (_timing.CurTime < component.NextSummonTime)
            return;

        SummonMinions(uid, component);
        component.NextSummonTime = _timing.CurTime + TimeSpan.FromSeconds(component.SummonInterval);
    }

    private void SummonMinions(EntityUid uid, LegionBossComponent component)
    {
        var selfCoords = Transform(uid).Coordinates;
        for (int i = 0; i < component.SummonCount; i++)
        {
            var spawnPos = FindSpawnPositionNear(selfCoords, 3f);
            if (spawnPos != null)
            {
                Spawn(component.MinionPrototype, spawnPos.Value);
            }
        }
    }

    #endregion

    #region Charging State

    private void UpdateChargingState(EntityUid uid, LegionBossComponent component, EntityUid target)
    {
        if (target == uid || !Exists(target) || _timing.CurTime < component.NextChargeTime)
            return;

        ChargeAtTarget(uid, component, target);
        component.NextChargeTime = _timing.CurTime + TimeSpan.FromSeconds(component.ChargeInterval);
    }

    private void ChargeAtTarget(EntityUid uid, LegionBossComponent component, EntityUid target)
    {
        var xform = Transform(uid);
        var targetCoords = Transform(target).Coordinates;

        var direction = (targetCoords.Position - xform.Coordinates.Position).Normalized();
        var throwing = direction * 6f;
        var throwTarget = xform.Coordinates.Offset(throwing);

        _throwing.TryThrow(uid, throwTarget, 15f);
    }

    #endregion

    #region Splitting System

    private void OnMegaLegionKilled(EntityUid uid, LegionBossComponent component, MegafaunaKilledEvent args)
    {
        var coords = Transform(uid).Coordinates;
        SpawnLootWithChance(component, coords);

        if (HasComp<LegionSplitComponent>(uid)) // Skip when is splited
            return;

        foreach (var prototype in component.SplitPrototypes)
        {
            var spawnPos = FindSpawnPositionNear(coords, 2f);
            if (spawnPos != null)
            {
                var splitEntity = Spawn(prototype, spawnPos.Value);
                TransferDamageContributors(uid, splitEntity);
            }
        }

        QueueDel(uid);
    }

    private void OnSplitKilled(EntityUid uid, LegionSplitComponent component, MegafaunaKilledEvent args)
    {
        RedistributeDamageToAllSplits(uid);

        if (!string.IsNullOrEmpty(component.NextSplitPrototype))
        {
            SplitToNextLevel(uid, component);
        }
        else
        {
            var allSplits = EntityQuery<LegionSplitComponent>().ToList();
            if (allSplits.Count == 1)
            {
                if (!TryComp<LegionBossComponent>(uid, out var legion))
                    return;

                var coords = Transform(uid).Coordinates;
                foreach (var reward in legion.RewardsProto)
                    Spawn(reward, coords);

                GrantAchievementsForLegion(uid);
            }
        }

        QueueDel(uid);
    }

    private void SplitToNextLevel(EntityUid uid, LegionSplitComponent component)
    {
        var coords = Transform(uid).Coordinates;
        for (int i = 0; i < 2; i++)
        {
            var spawnPos = FindSpawnPositionNear(coords, 2f);
            if (spawnPos != null)
            {
                var nextSplit = Spawn(component.NextSplitPrototype, spawnPos.Value);
                TransferDamageContributors(uid, nextSplit);
            }
        }
    }

    private void SpawnLootWithChance(LegionBossComponent component, EntityCoordinates coords)
    {
        foreach (var (prototype, chance) in component.LootPrototypes)
        {
            if (_random.Prob(chance))
            {
                var spawnPos = FindSpawnPositionNear(coords, 1.5f);
                if (spawnPos != null)
                {
                    Spawn(prototype, spawnPos.Value);
                }
            }
        }
    }

    private void GrantAchievementsForLegion(EntityUid lastSplit)
    {
        if (!TryComp<MegafaunaDamageContributorComponent>(lastSplit, out var contributor))
            return;

        if (contributor.AchievementsGranted)
            return;

        contributor.AchievementsGranted = true;

        FixedPoint2 totalDamage = 0f;
        foreach (var damage in contributor.Contributors.Values)
            totalDamage += damage;

        if (totalDamage <= 0)
            return;

        var threshold = contributor.Threshold;
        foreach (var (player, damage) in contributor.Contributors)
        {
            var percentage = damage / totalDamage;
            if (percentage >= threshold)
            {
                _achievement.QueueAchievement(player, AchievementsEnum.FirstBoss);
                _achievement.QueueAchievement(player, AchievementsEnum.LegionBoss);
            }
        }

        contributor.Contributors.Clear();
    }

    private void RedistributeDamageToAllSplits(EntityUid dyingSplit)
    {
        if (!TryComp<MegafaunaDamageContributorComponent>(dyingSplit, out var dyingContrib))
            return;

        var livingSplits = new List<EntityUid>();
        var allSplits = EntityQueryEnumerator<LegionSplitComponent>();
        while (allSplits.MoveNext(out var uid, out _))
        {
            if (uid == dyingSplit || Exists(uid))
                continue;

            livingSplits.Add(uid);
        }

        if (livingSplits.Count == 0)
            return;

        foreach (var living in livingSplits)
        {
            var livingContrib = EnsureComp<MegafaunaDamageContributorComponent>(living);
            foreach (var (player, damage) in dyingContrib.Contributors)
            {
                livingContrib.Contributors.TryGetValue(player, out var current);
                livingContrib.Contributors[player] = current + damage;
            }

            livingContrib.Threshold = dyingContrib.Threshold;
        }
    }

    private void TransferDamageContributors(EntityUid from, EntityUid to)
    {
        if (!TryComp<MegafaunaDamageContributorComponent>(from, out var fromContrib))
            return;

        var toContrib = EnsureComp<MegafaunaDamageContributorComponent>(to);
        foreach (var (player, damage) in fromContrib.Contributors)
        {
            toContrib.Contributors.TryGetValue(player, out var current);
            toContrib.Contributors[player] = current + damage;
        }

        toContrib.Threshold = fromContrib.Threshold;
        toContrib.AchievementId = fromContrib.AchievementId;
    }

    #endregion

    #region Utility Methods

    private EntityCoordinates? FindSpawnPositionNear(EntityCoordinates center, float maxDistance)
    {
        for (int i = 0; i < 5; i++)
        {
            var angle = _random.NextDouble() * Math.PI * 2;
            var distance = _random.NextFloat(1f, maxDistance);
            var offset = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * distance;

            var testCoords = center.Offset(offset);

            if (CanMoveTo(testCoords))
                return testCoords;
        }
        return null;
    }

    private bool CanMoveTo(EntityCoordinates coords)
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
