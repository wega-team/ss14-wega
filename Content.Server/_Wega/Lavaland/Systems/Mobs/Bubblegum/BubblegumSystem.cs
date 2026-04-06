using System.Linq;
using System.Numerics;
using Content.Server.Lavaland.Mobs.Components;
using Content.Server.NPC.Components;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Shared.Actions.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Fluids.Components;
using Content.Shared.Ghost;
using Content.Shared.Gibbing;
using Content.Shared.Lavaland.Events;
using Content.Shared.Maps;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Physics;
using Content.Shared.Visuals;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Lavaland.Mobs;

public sealed partial class BubblegumSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly GibbingSystem _gibbing = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _threshold = default!;
    [Dependency] private readonly NPCUseActionsOnTargetSystem _npcActions = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly NPCSystem _npc = default!;

    private const float LowHealthThreshold = 0.5f;
    private const float PassiveHandRadius = 5f;
    private const float PassiveHandInterval = 2f;
    private const float PassiveHandChance = 0.5f;

    private Dictionary<EntityUid, List<EntityUid>> _activeIllusions = new();
    private HashSet<EntityUid> _dashDamagedTargets = new();
    private HashSet<EntityUid> _illusionDamagedTargets = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BubblegumBossComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<BubblegumBossComponent, MegafaunaKilledEvent>(OnBubblegumKilled);

        SubscribeLocalEvent<BubblegumBossComponent, BubblegumRageActionEvent>(OnRageAction);
        SubscribeLocalEvent<BubblegumBossComponent, BubblegumBloodDiveActionEvent>(OnBloodDiveAction);
        SubscribeLocalEvent<BubblegumBossComponent, BubblegumTripleDashActionEvent>(OnTripleDash);
        SubscribeLocalEvent<BubblegumBossComponent, BubblegumIllusionDashActionEvent>(OnIllusionDash);
        SubscribeLocalEvent<BubblegumBossComponent, BubblegumPentagramDashActionEvent>(OnPentagramDashAction);
        SubscribeLocalEvent<BubblegumBossComponent, BubblegumChaoticIllusionDashActionEvent>(OnChaoticIllusionDashAction);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateRageState();
        UpdatePassiveHandAttack();
    }

    #region Event Handlers

    private void OnDamageChanged(EntityUid uid, BubblegumBossComponent component, DamageChangedEvent args)
    {
        if (!args.DamageIncreased)
            return;

        var healthRatio = GetHealthRatio(uid);
        var newPhase = healthRatio > LowHealthThreshold
            ? BubblegumPhase.Normal : BubblegumPhase.Enraged;

        UpdateRageChance(uid, component);

        if (newPhase != component.CurrentPhase)
        {
            component.CurrentPhase = newPhase;
            UpdatePhaseActions(uid, component);
        }
    }

    private void UpdatePhaseActions(EntityUid uid, BubblegumBossComponent component)
    {
        if (!TryComp<NPCUseActionsOnTargetComponent>(uid, out var npcActions))
            return;

        if (component.CurrentPhase == BubblegumPhase.Normal)
        {
            _npcActions.SetActions(uid,
                component.Phase1Actions ?? new(),
                component.Phase1Chances ?? new(),
                npcActions);
        }
        else
        {
            _npcActions.SetActions(uid,
                component.Phase2Actions ?? new(),
                component.Phase2Chances ?? new(),
                npcActions);
        }

        UpdateRageChance(uid, component, npcActions);
    }

    private void UpdateRageChance(EntityUid uid, BubblegumBossComponent component, NPCUseActionsOnTargetComponent? npcActions = null)
    {
        if (!Resolve(uid, ref npcActions))
            return;

        var healthRatio = GetHealthRatio(uid);
        var rageChance = 0.1f + 0.3f * (1f - healthRatio);
        rageChance = Math.Clamp(rageChance, 0.1f, 0.4f);

        _npcActions.SetActionChance(uid,
            new EntProtoId<TargetActionComponent>("ActionBubblegumRage"),
            rageChance, npcActions);
    }

    private void OnBubblegumKilled(EntityUid uid, BubblegumBossComponent component, MegafaunaKilledEvent args)
    {
        var coords = Transform(uid).Coordinates;
        foreach (var reward in component.RewardsProto)
            Spawn(reward, coords);

        QueueDel(uid);
    }

    #endregion

    #region Rage System

    private void OnRageAction(Entity<BubblegumBossComponent> ent, ref BubblegumRageActionEvent args)
    {
        args.Handled = true;
        TriggerRage(ent, ent.Comp);
    }

    private void TriggerRage(EntityUid uid, BubblegumBossComponent component)
    {
        if (component.IsRaging)
            return;

        component.IsRaging = true;
        var duration = _random.NextFloat(component.RageDurationMin, component.RageDurationMax);
        component.RageEndTime = _timing.CurTime + TimeSpan.FromSeconds(duration);

        EnsureComp<GodmodeComponent>(uid);
        _appearance.SetData(uid, VisualLayers.Enabled, true);
        _npcActions.SetDelaySpeed(uid, component.RageDelayModifier);
    }

    private void UpdateRageState()
    {
        var query = EntityQueryEnumerator<BubblegumBossComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.IsRaging)
                continue;

            if (_timing.CurTime >= comp.RageEndTime)
            {
                comp.IsRaging = false;
                RemCompDeferred<GodmodeComponent>(uid);
                _appearance.SetData(uid, VisualLayers.Enabled, false);
                _npcActions.SetDelaySpeed(uid, 1f);
            }
        }
    }

    #endregion

    #region Triple Dash

    private void OnTripleDash(Entity<BubblegumBossComponent> ent, ref BubblegumTripleDashActionEvent args)
    {
        args.Handled = true;

        CleanupIllusions(ent.Owner);
        SpawnBloodPool(ent);

        var target = args.Target;
        if (!Exists(target))
            return;

        var mapUid = _transform.GetMap(ent.Owner);
        if (mapUid == null)
            return;

        PerformTripleDashStep(ent, target, mapUid.Value, args.DashDamage, args.DashDistance, args.MoveSpeed,
            args.UseSineWaveForLast, args.DashDelays, 0);
    }

    private void PerformTripleDashStep(Entity<BubblegumBossComponent> ent, EntityUid target, EntityUid mapUid,
        DamageSpecifier dashDamage, float dashDistance, float moveSpeed, bool useSineWaveForLast,
        List<float> dashDelays, int stepIndex)
    {
        if (!Exists(ent.Owner) || _mobState.IsDead(ent.Owner) || !Exists(target))
            return;

        var bossPos = _transform.GetWorldPosition(ent);
        var currentTargetPos = _transform.GetWorldPosition(target);
        Vector2 dashTarget;

        if (stepIndex == 2)
        {
            if (useSineWaveForLast && _random.Prob(0.5f))
            {
                var direction = (currentTargetPos - bossPos).Normalized();
                var sineOffset = MathF.Sin(_timing.CurTime.Seconds * 4) * 2f;
                var perpendicular = new Vector2(-direction.Y, direction.X);
                dashTarget = currentTargetPos + perpendicular * sineOffset;
            }
            else
            {
                var direction = (currentTargetPos - bossPos).Normalized();
                dashTarget = currentTargetPos + direction * 3.5f;
            }
        }
        else
        {
            var direction = (currentTargetPos - bossPos).Normalized();
            dashTarget = bossPos + direction * dashDistance;
        }

        var centerDashTarget = GetTileCenter(mapUid, dashTarget);
        var markerCoords = new EntityCoordinates(mapUid, centerDashTarget);

        if (!IsValidSpawnPosition(markerCoords))
        {
            var safeCoords = FindSafePositionNear(ent, markerCoords);
            if (safeCoords == null)
                return;
            markerCoords = safeCoords.Value;
        }

        Spawn(ent.Comp.DashMarker, markerCoords);

        PerformDash(ent, markerCoords, dashDamage, moveSpeed, stepIndex == 2,
            () => ScheduleNextDashStep(ent, target, mapUid, dashDamage, dashDistance, moveSpeed,
                useSineWaveForLast, dashDelays, stepIndex));
    }

    private void ScheduleNextDashStep(Entity<BubblegumBossComponent> ent, EntityUid target, EntityUid mapUid,
        DamageSpecifier dashDamage, float dashDistance, float moveSpeed, bool useSineWaveForLast,
        List<float> dashDelays, int currentStep)
    {
        if (currentStep >= dashDelays.Count - 1)
            return;

        var nextStep = currentStep + 1;
        var nextDelay = dashDelays[nextStep];

        Timer.Spawn(TimeSpan.FromSeconds(nextDelay), () =>
        {
            PerformTripleDashStep(ent, target, mapUid, dashDamage, dashDistance, moveSpeed,
                useSineWaveForLast, dashDelays, nextStep);
        });
    }

    #endregion

    #region Illusion Dash

    private void OnIllusionDash(Entity<BubblegumBossComponent> ent, ref BubblegumIllusionDashActionEvent args)
    {
        args.Handled = true;

        _npc.SleepNPC(ent.Owner);
        SpawnBloodPool(ent);

        var target = args.Target;
        if (!Exists(target))
        {
            _npc.WakeNPC(ent.Owner);
            return;
        }

        var mapUid = _transform.GetMap(ent.Owner);
        if (mapUid == null)
        {
            _npc.WakeNPC(ent.Owner);
            SetHTNTarget(ent, target);
            return;
        }

        PerformIllusionDashIteration(ent, target, mapUid.Value, args, 0);
    }

    private void PerformIllusionDashIteration(Entity<BubblegumBossComponent> ent, EntityUid target,
        EntityUid mapUid, BubblegumIllusionDashActionEvent args, int iteration)
    {
        if (!Exists(ent.Owner) || !Exists(target) || _mobState.IsDead(ent.Owner))
        {
            if (iteration == 0)
            {
                _npc.WakeNPC(ent.Owner);
                SetHTNTarget(ent, target);
            }
            return;
        }

        var targetCoords = Transform(target).Coordinates;

        var markerCoords = targetCoords;
        if (!IsValidSpawnPosition(markerCoords))
        {
            var safeCoords = FindSafePositionNear(ent, markerCoords);
            if (safeCoords == null)
            {
                ContinueToNextIteration(ent, target, mapUid, args, iteration);
                return;
            }
            markerCoords = safeCoords.Value;
        }

        Spawn(ent.Comp.DashMarker, markerCoords);

        var totalEntities = args.IllusionCount + 1;
        var positions = GetCircularPositions(targetCoords, mapUid, totalEntities, args.PlacementRadius);
        if (positions.Count < totalEntities)
        {
            ContinueToNextIteration(ent, target, mapUid, args, iteration);
            return;
        }

        var bossIndex = _random.Next(positions.Count);
        var illusions = SpawnIllusionCircle(ent, target, targetCoords, positions, bossIndex,
            args.IllusionPrototype, args.IllusionDamage);

        if (illusions.Count == 0)
        {
            ContinueToNextIteration(ent, target, mapUid, args, iteration);
            return;
        }

        _activeIllusions[ent.Owner] = illusions;

        var damage = args.IllusionDamage;

        Timer.Spawn(TimeSpan.FromSeconds(args.PreDashDelay), () =>
        {
            if (!Exists(ent.Owner) || !Exists(target))
            {
                CleanupIllusions(ent.Owner);
                ContinueToNextIteration(ent, target, mapUid, args, iteration);
                return;
            }

            StartIllusionDashForAll(illusions, targetCoords, damage);
            PerformDash(ent, targetCoords, damage, 0.1f, false);

            Timer.Spawn(TimeSpan.FromSeconds(1.5f), () =>
            {
                CleanupIllusions(ent.Owner);

                var nextIteration = iteration + 1;
                if (nextIteration < 3)
                {
                    PerformIllusionDashIteration(ent, target, mapUid, args, nextIteration);
                }
                else
                {
                    if (Exists(ent.Owner) && Exists(target))
                    {
                        TriggerTripleDashAfterIllusion(ent, target, args.IllusionDamage);
                    }
                    else
                    {
                        _npc.WakeNPC(ent.Owner);
                        SetHTNTarget(ent, target);
                    }
                }
            });
        });
    }

    private void ContinueToNextIteration(Entity<BubblegumBossComponent> ent, EntityUid target,
        EntityUid mapUid, BubblegumIllusionDashActionEvent args, int currentIteration)
    {
        var nextIteration = currentIteration + 1;

        if (nextIteration < 3)
        {
            PerformIllusionDashIteration(ent, target, mapUid, args, nextIteration);
        }
        else
        {
            if (Exists(ent.Owner) && Exists(target))
            {
                TriggerTripleDashAfterIllusion(ent, target, args.IllusionDamage);
            }
            else
            {
                _npc.WakeNPC(ent.Owner);
                SetHTNTarget(ent, target);
            }
        }
    }

    private void TriggerTripleDashAfterIllusion(Entity<BubblegumBossComponent> ent, EntityUid target,
        DamageSpecifier illusionDamage)
    {
        var tripleDashEvent = new BubblegumTripleDashActionEvent
        {
            Target = target,
            DashDamage = new DamageSpecifier(illusionDamage)
            {
                DamageDict = illusionDamage.DamageDict.ToDictionary(
                    x => x.Key,
                    x => x.Value * 2)
            },
            DashDistance = 5f,
            MoveSpeed = 0.05f,
            UseSineWaveForLast = true,
            DashDelays = new List<float> { 0.9f, 0.6f, 0.3f },
            Performer = ent.Owner
        };

        OnTripleDash(ent, ref tripleDashEvent);
    }

    #endregion

    #region Blood Dive

    private void OnBloodDiveAction(Entity<BubblegumBossComponent> ent, ref BubblegumBloodDiveActionEvent args)
    {
        args.Handled = true;
        if (_timing.CurTime < ent.Comp.NextBloodDiveTime)
            return;

        if (!Exists(args.Target))
            return;

        var target = args.Target;
        var targetCoords = Transform(target).Coordinates;
        var mapUid = _transform.GetMap(ent.Owner);
        if (mapUid == null)
            return;

        var diveCoords = FindBloodDiveCoordinates(ent, targetCoords, mapUid.Value, args);

        if (diveCoords == null)
            return;

        if (!IsValidSpawnPosition(diveCoords.Value))
        {
            var safeCoords = FindSafePositionNear(ent, diveCoords.Value);
            if (safeCoords == null)
                return;
            diveCoords = safeCoords;
        }

        Timer.Spawn(TimeSpan.FromSeconds(args.PreDiveDelay), () =>
        {
            if (!Exists(ent.Owner))
                return;

            _transform.SetCoordinates(ent.Owner, diveCoords.Value);
            SpawnBloodPool(ent.Owner);
        });

        ent.Comp.NextBloodDiveTime = _timing.CurTime + TimeSpan.FromSeconds(ent.Comp.BloodDiveCooldown);
    }

    private EntityCoordinates? FindBloodDiveCoordinates(Entity<BubblegumBossComponent> ent, EntityCoordinates targetCoords,
        EntityUid mapUid, BubblegumBloodDiveActionEvent args)
    {
        var bloodPuddles = _lookup
            .GetEntitiesInRange<PuddleComponent>(targetCoords, args.DiveRange)
            .Where(p => HasBloodPuddle(p.Owner))
            .ToList();

        if (bloodPuddles.Count > 0)
        {
            var selectedPuddle = _random.Pick(bloodPuddles);
            var puddleCoords = Transform(selectedPuddle.Owner).Coordinates;
            var puddlePos = puddleCoords.Position;
            var tileCenter = GetTileCenter(mapUid, puddlePos);
            return new EntityCoordinates(mapUid, tileCenter);
        }

        var spawnPos = FindValidPositionNear(targetCoords, args.DiveRange);
        if (spawnPos != null)
            Spawn(ent.Comp.BloodEffect, spawnPos.Value);

        return spawnPos;
    }

    #endregion

    #region Pentagram Dash

    private void OnPentagramDashAction(Entity<BubblegumBossComponent> ent, ref BubblegumPentagramDashActionEvent args)
    {
        args.Handled = true;
        if (ent.Comp.CurrentPhase != BubblegumPhase.Enraged)
            return;

        _npc.SleepNPC(ent.Owner);
        SpawnBloodPool(ent);

        var target = args.Target;
        if (!Exists(target))
        {
            _npc.WakeNPC(ent.Owner);
            return;
        }

        var targetCoords = Transform(target).Coordinates;
        var mapUid = _transform.GetMap(ent.Owner);
        if (mapUid == null)
        {
            _npc.WakeNPC(ent.Owner);
            SetHTNTarget(ent, target);
            return;
        }

        var markerCoords = targetCoords;
        if (!IsValidSpawnPosition(markerCoords))
        {
            var safeCoords = FindSafePositionNear(ent, markerCoords);
            if (safeCoords == null)
            {
                _npc.WakeNPC(ent.Owner);
                SetHTNTarget(ent, target);
                return;
            }
            markerCoords = safeCoords.Value;
        }

        Spawn(ent.Comp.DashMarker, markerCoords);

        const int totalEntities = 5;
        var positions = GetCircularPositions(targetCoords, mapUid.Value, totalEntities, args.PlacementRadius);
        if (positions.Count < totalEntities)
        {
            _npc.WakeNPC(ent.Owner);
            SetHTNTarget(ent, target);
            return;
        }

        var bossIndex = _random.Next(positions.Count);
        var illusions = SpawnIllusionCircle(ent, target, targetCoords, positions, bossIndex,
            args.IllusionPrototype, args.IllusionDamage);

        if (illusions.Count == 0)
        {
            _npc.WakeNPC(ent.Owner);
            SetHTNTarget(ent, target);
            return;
        }

        _activeIllusions[ent.Owner] = illusions;

        var damage = args.IllusionDamage;
        Timer.Spawn(TimeSpan.FromSeconds(args.PreDashDelay), () =>
        {
            if (!Exists(ent.Owner) || !Exists(target))
            {
                CleanupIllusions(ent.Owner);
                _npc.WakeNPC(ent.Owner);
                SetHTNTarget(ent, target);
                return;
            }

            StartIllusionDashForAll(illusions, targetCoords, damage);
            PerformDash(ent, targetCoords, damage, 0.1f, true);

            Timer.Spawn(TimeSpan.FromSeconds(1.5f), () =>
            {
                CleanupIllusions(ent.Owner);
                if (Exists(ent.Owner))
                {
                    _npc.WakeNPC(ent.Owner);
                    SetHTNTarget(ent, target);
                }
            });
        });
    }

    #endregion

    #region Chaotic Illusion Dash

    private void OnChaoticIllusionDashAction(Entity<BubblegumBossComponent> ent, ref BubblegumChaoticIllusionDashActionEvent args)
    {
        args.Handled = true;
        if (ent.Comp.CurrentPhase != BubblegumPhase.Enraged)
            return;

        var target = args.Target;
        if (!Exists(target))
            return;

        CleanupIllusions(ent.Owner);

        var action = args;
        for (int wave = 0; wave < 5; wave++)
        {
            var currentWave = wave;
            var waveDelay = wave * 2.3f;

            Timer.Spawn(TimeSpan.FromSeconds(waveDelay), () =>
            {
                if (!Exists(ent.Owner) || !Exists(target) || _mobState.IsDead(ent.Owner))
                    return;

                ExecuteChaoticWave(ent, target, action, currentWave);
            });
        }
    }

    private void ExecuteChaoticWave(Entity<BubblegumBossComponent> ent, EntityUid target,
        BubblegumChaoticIllusionDashActionEvent args, int waveIndex)
    {
        CleanupIllusions(ent.Owner);

        _npc.SleepNPC(ent.Owner);
        SpawnBloodPool(ent);

        var mapUid = _transform.GetMap(ent.Owner);
        if (mapUid == null)
        {
            _npc.WakeNPC(ent.Owner);
            SetHTNTarget(ent, target);
            return;
        }

        var bossMarker = GenerateRandomMarker(target, mapUid.Value, args.PlacementRadius);
        var illusionMarkers = new List<EntityCoordinates>();

        if (!IsValidSpawnPosition(bossMarker))
        {
            var safeCoords = FindSafePositionNear(ent, bossMarker);
            if (safeCoords == null)
            {
                _npc.WakeNPC(ent.Owner);
                SetHTNTarget(ent, target);
                return;
            }
            bossMarker = safeCoords.Value;
        }

        Spawn(ent.Comp.DashMarker, bossMarker);
        var illusions = SpawnChaoticIllusions(ent, target, mapUid.Value, args, illusionMarkers);

        if (illusions.Count == 0)
        {
            _npc.WakeNPC(ent.Owner);
            SetHTNTarget(ent, target);
            return;
        }

        _activeIllusions[ent.Owner] = illusions;

        Timer.Spawn(TimeSpan.FromSeconds(args.PreDashDelay), () =>
        {
            if (!Exists(ent.Owner) || !Exists(target))
            {
                CleanupIllusions(ent.Owner);
                _npc.WakeNPC(ent.Owner);
                SetHTNTarget(ent, target);
                return;
            }

            StartChaoticIllusionAttacks(illusions, illusionMarkers, args.IllusionDamage);
            PerformDash(ent, bossMarker, args.IllusionDamage, 0.1f, waveIndex == 4);

            Timer.Spawn(TimeSpan.FromSeconds(1f), () =>
            {
                CleanupIllusions(ent.Owner);
                if (Exists(ent.Owner) && waveIndex == 4)
                {
                    _npc.WakeNPC(ent.Owner);
                    SetHTNTarget(ent, target);
                }
            });
        });
    }

    private EntityCoordinates GenerateRandomMarker(EntityUid target, EntityUid mapUid, float placementRadius)
    {
        var angle = _random.NextFloat(0, MathF.PI * 2);
        var distance = _random.NextFloat(1f, placementRadius);
        var offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * distance;

        var targetPos = _transform.GetWorldPosition(target);
        var markerPos = targetPos + offset;
        var centerPos = GetTileCenter(mapUid, markerPos);

        return new EntityCoordinates(mapUid, centerPos);
    }

    private List<EntityUid> SpawnChaoticIllusions(Entity<BubblegumBossComponent> ent, EntityUid target,
        EntityUid mapUid, BubblegumChaoticIllusionDashActionEvent args, List<EntityCoordinates> illusionMarkers)
    {
        var illusions = new List<EntityUid>();
        var bossPos = _transform.GetWorldPosition(ent);
        var bossTile = GetTileCenter(mapUid, bossPos);

        for (int i = 0; i < args.IllusionCount; i++)
        {
            var marker = GenerateRandomMarker(target, mapUid, args.PlacementRadius);

            if (!IsValidSpawnPosition(marker))
            {
                var safeCoords = FindValidPositionNear(marker, args.PlacementRadius);
                if (safeCoords == null)
                    continue;
                marker = safeCoords.Value;
            }

            illusionMarkers.Add(marker);

            Spawn(ent.Comp.DashMarker, marker);
            for (int attempts = 0; attempts < 30; attempts++)
            {
                var angle = _random.NextFloat(0, MathF.PI * 2);
                var distance = _random.NextFloat(2f, args.PlacementRadius);
                var offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * distance;

                var targetPos = _transform.GetWorldPosition(target);
                var illusionPos = targetPos + offset;
                var illusionTile = GetTileCenter(mapUid, illusionPos);
                var illusionCoords = new EntityCoordinates(mapUid, illusionTile);

                if (!CanSpawnAt(illusionCoords) || illusionTile == bossTile)
                    continue;

                var direction = (marker.Position - illusionTile).Normalized();
                var illusion = SpawnAttachedTo(args.IllusionPrototype, illusionCoords, rotation: GetDirectionRotation(direction));

                if (TryComp<BubblegumIllusionComponent>(illusion, out var illusionComp))
                {
                    illusionComp.Master = ent.Owner;
                    illusionComp.Target = target;
                    illusionComp.TargetPosition = marker;
                    illusionComp.Damage = args.IllusionDamage;
                    illusions.Add(illusion);
                }
                break;
            }
        }

        return illusions;
    }

    private void StartChaoticIllusionAttacks(List<EntityUid> illusions, List<EntityCoordinates> markers, DamageSpecifier damage)
    {
        for (int i = 0; i < illusions.Count; i++)
        {
            var illusion = illusions[i];
            var marker = markers[i];

            if (!Exists(illusion))
                continue;

            var randomDelay = _random.NextFloat(0f, 0.3f);
            Timer.Spawn(TimeSpan.FromSeconds(randomDelay), () =>
            {
                if (Exists(illusion))
                    StartIllusionDash(illusion, marker, damage);
            });
        }
    }

    #endregion

    #region Dash Execution

    private void PerformDash(EntityUid uid, EntityCoordinates target, DamageSpecifier damage,
        float moveSpeed, bool isLastDash, Action? onComplete = null)
    {
        if (!IsValidSpawnPosition(target))
        {
            onComplete?.Invoke();
            return;
        }

        var startPos = _transform.GetWorldPosition(uid);
        var targetPos = target.Position;

        var map = _transform.GetMap(uid);
        if (map == null)
        {
            onComplete?.Invoke();
            return;
        }

        var startTile = GetTileCenter(map.Value, startPos);
        var targetTile = target.Position;

        var direction = (targetTile - startTile).Normalized();
        var distance = Vector2.Distance(startTile, targetTile);
        var steps = Math.Max(1, (int)Math.Ceiling(distance));

        var mapUid = _transform.GetMap(uid);
        if (mapUid == null)
        {
            onComplete?.Invoke();
            return;
        }

        _dashDamagedTargets.Clear();

        if (!TryComp<BubblegumBossComponent>(uid, out var bossComp))
        {
            onComplete?.Invoke();
            return;
        }

        InitializeDash(uid, bossComp, mapUid.Value, startTile, direction);

        var stepCounter = new StepCounter { CompletedSteps = 0, TotalSteps = steps };

        for (int step = 1; step <= steps; step++)
        {
            ScheduleDashStep(uid, bossComp, mapUid.Value, startTile, direction, step, moveSpeed,
                damage, stepCounter, isLastDash, onComplete);
        }
    }

    private void InitializeDash(EntityUid uid, BubblegumBossComponent bossComp, EntityUid mapUid,
        Vector2 startTile, Vector2 direction)
    {
        SpawnBloodPool(uid);
        SpawnAttachedTo(bossComp.DashTrail, new EntityCoordinates(mapUid, startTile),
            rotation: GetDirectionRotation(direction));
    }

    private void ScheduleDashStep(EntityUid uid, BubblegumBossComponent bossComp, EntityUid mapUid,
        Vector2 startTile, Vector2 direction, int step, float moveSpeed, DamageSpecifier damage,
        StepCounter stepCounter, bool isLastDash, Action? onComplete)
    {
        var currentStep = step;

        Timer.Spawn(TimeSpan.FromSeconds(currentStep * moveSpeed), () =>
        {
            if (!Exists(uid))
            {
                if (currentStep == stepCounter.TotalSteps)
                    onComplete?.Invoke();
                return;
            }

            var stepVector = direction * currentStep;
            var currentPos = startTile + stepVector;
            var tileCenter = GetTileCenter(mapUid, currentPos);
            var currentCoords = new EntityCoordinates(mapUid, tileCenter);

            if (!IsValidSpawnPosition(currentCoords))
            {
                if (currentStep == stepCounter.TotalSteps)
                    onComplete?.Invoke();
                return;
            }

            _transform.SetCoordinates(uid, currentCoords);
            SpawnBloodPool(uid);

            SpawnAttachedTo(bossComp.DashTrail, currentCoords,
                rotation: GetDirectionRotation(direction));

            CheckDashDamage(uid, currentCoords, damage);
            _audio.PlayPvs(bossComp.DashSound, uid);

            stepCounter.CompletedSteps++;

            if (stepCounter.CompletedSteps >= stepCounter.TotalSteps)
                HandleDashCompletion(uid, isLastDash, onComplete);
        });
    }

    private void HandleDashCompletion(EntityUid uid, bool isLastDash, Action? onComplete)
    {
        Timer.Spawn(TimeSpan.FromSeconds(0.1f), () =>
        {
            onComplete?.Invoke();

            if (isLastDash)
            {
                Timer.Spawn(TimeSpan.FromSeconds(0.3f), () =>
                {
                    if (Exists(uid))
                        _npc.WakeNPC(uid);
                });
            }
        });
    }

    private void CheckDashDamage(EntityUid uid, EntityCoordinates coords, DamageSpecifier damage)
    {
        var entities = _lookup.GetEntitiesInRange<MobStateComponent>(coords, 1f, LookupFlags.Uncontained);
        foreach (var entity in entities)
        {
            if (entity.Owner == uid || HasComp<BubblegumBossComponent>(entity.Owner))
                continue;

            if (_dashDamagedTargets.Contains(entity.Owner))
                continue;

            if (_mobState.IsIncapacitated(entity.Owner))
            {
                _gibbing.Gib(entity.Owner);
                _dashDamagedTargets.Add(entity.Owner);
                continue;
            }

            if (_damage.TryChangeDamage(entity.Owner, damage))
                _dashDamagedTargets.Add(entity.Owner);
        }
    }

    #endregion

    #region Illusion System

    private void StartIllusionDash(EntityUid uid, EntityCoordinates target, DamageSpecifier damage)
    {
        if (!TryComp<BubblegumIllusionComponent>(uid, out var illusion))
            return;

        if (!IsValidSpawnPosition(target))
            return;

        illusion.TargetPosition = target;

        var startPos = _transform.GetWorldPosition(uid);
        var targetPos = target.Position;

        var map = _transform.GetMap(uid);
        if (map == null)
            return;

        var startTile = GetTileCenter(map.Value, startPos);
        var targetTile = GetTileCenter(map.Value, targetPos);

        var direction = (targetTile - startTile).Normalized();
        var distance = Vector2.Distance(startTile, targetTile);
        var steps = Math.Max(1, (int)Math.Ceiling(distance));

        illusion.TotalSteps = steps;

        var mapUid = _transform.GetMap(uid);
        if (mapUid == null)
            return;

        _illusionDamagedTargets.Clear();

        for (int step = 1; step <= steps; step++)
        {
            ScheduleIllusionDashStep(uid, illusion, mapUid.Value, startTile, direction, step, damage);
        }
    }

    private void ScheduleIllusionDashStep(EntityUid uid, BubblegumIllusionComponent illusion, EntityUid mapUid,
        Vector2 startTile, Vector2 direction, int step, DamageSpecifier damage)
    {
        var currentStep = step;

        Timer.Spawn(TimeSpan.FromSeconds(currentStep * 0.1f), () =>
        {
            if (!Exists(uid))
                return;

            var stepVector = direction * currentStep;
            var currentPos = startTile + stepVector;
            var tileCenter = GetTileCenter(mapUid, currentPos);
            var currentCoords = new EntityCoordinates(mapUid, tileCenter);

            if (!IsValidSpawnPosition(currentCoords))
                return;

            _transform.SetCoordinates(uid, currentCoords);

            if (TryComp<BubblegumBossComponent>(illusion.Master, out var bossComp))
                SpawnAttachedTo(bossComp.DashTrail, currentCoords,
                    rotation: GetDirectionRotation(direction));

            CheckIllusionDashDamage(uid, illusion.Master, currentCoords, damage);

            illusion.CurrentStep = currentStep;
        });
    }

    private void CheckIllusionDashDamage(EntityUid uid, EntityUid? master, EntityCoordinates coords, DamageSpecifier damage)
    {
        var entities = _lookup.GetEntitiesInRange<MobStateComponent>(coords, 1f, LookupFlags.Uncontained);
        foreach (var entity in entities)
        {
            if (entity.Owner == uid || entity.Owner == master)
                continue;

            if (_illusionDamagedTargets.Contains(entity.Owner))
                continue;

            if (_damage.TryChangeDamage(entity.Owner, damage, origin: master))
                _illusionDamagedTargets.Add(entity.Owner);
        }
    }

    #endregion

    #region Passive Hand Attack

    private void UpdatePassiveHandAttack()
    {
        var query = EntityQueryEnumerator<BubblegumBossComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (_mobState.IsDead(uid))
                continue;

            if (_timing.CurTime.TotalSeconds % PassiveHandInterval > 0.1f)
                continue;

            var playersOnBlood = FindPlayersOnBlood(xform.Coordinates);

            foreach (var player in playersOnBlood)
            {
                if (_random.Prob(PassiveHandChance))
                    SpawnBloodHand(player, comp);
            }
        }
    }

    private HashSet<EntityUid> FindPlayersOnBlood(EntityCoordinates center)
    {
        var playersOnBlood = new HashSet<EntityUid>();
        var bloodPuddles = _lookup.GetEntitiesInRange<PuddleComponent>(center, PassiveHandRadius);

        foreach (var puddle in bloodPuddles)
        {
            if (!HasBloodPuddle(puddle.Owner))
                continue;

            var puddleCoords = Transform(puddle.Owner).Coordinates;
            var entitiesOnPuddle = _lookup.GetEntitiesInRange<ActorComponent>(puddleCoords, 0.5f, LookupFlags.Uncontained)
                .Where(a => HasComp<MobStateComponent>(a.Owner) && !HasComp<GhostComponent>(a.Owner));

            foreach (var entity in entitiesOnPuddle)
                playersOnBlood.Add(entity.Owner);
        }

        return playersOnBlood;
    }

    private void SpawnBloodHand(EntityUid target, BubblegumBossComponent comp)
    {
        var targetCoords = Transform(target).Coordinates;
        var bloodPuddles = _lookup.GetEntitiesInRange<PuddleComponent>(targetCoords, 0.5f)
            .Where(p => HasBloodPuddle(p.Owner))
            .ToList();

        foreach (var puddle in bloodPuddles)
        {
            Spawn(comp.HandEffect, Transform(puddle.Owner).Coordinates);
            if (_mobState.IsIncapacitated(target))
                _gibbing.Gib(target);

            break;
        }
    }

    private bool HasBloodPuddle(EntityUid uid)
    {
        if (!TryComp<PuddleComponent>(uid, out var puddle))
            return false;

        if (!TryComp(uid, out ContainerManagerComponent? containerManager))
            return false;

        if (!containerManager.Containers.TryGetValue("solution@puddle", out var container))
            return false;

        return container.ContainedEntities.Any(containedEntity =>
            TryComp(containedEntity, out SolutionComponent? solutionComponent) &&
            solutionComponent.Solution.Contents.Any(r =>
                r.Reagent.Prototype == "Blood" || r.Reagent.Prototype == "CopperBlood"));
    }

    #endregion

    #region Blood Pool

    private void SpawnBloodPool(EntityUid uid)
    {
        if (!TryComp<BubblegumBossComponent>(uid, out var comp))
            return;

        var mapUid = _transform.GetMap(uid);
        if (mapUid == null)
            return;

        var centerPos = _transform.GetWorldPosition(uid);
        var centerTile = GetTileCenter(mapUid.Value, centerPos);

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                TrySpawnBloodPoolAt(uid, comp, mapUid.Value, centerTile, x, y);
            }
        }
    }

    private void TrySpawnBloodPoolAt(EntityUid uid, BubblegumBossComponent comp, EntityUid mapUid,
        Vector2 centerTile, int x, int y)
    {
        var bloodPos = centerTile + new Vector2(x, y);
        var bloodCoords = new EntityCoordinates(mapUid, bloodPos);

        if (!IsValidMapPosition(mapUid, bloodPos))
            return;

        var existingPuddles = _lookup.GetEntitiesInRange<PuddleComponent>(bloodCoords, 0.1f);
        var hasBlood = existingPuddles.Any(p => HasBloodPuddle(p.Owner));

        if (!hasBlood)
            Spawn(comp.BloodEffect, bloodCoords);
    }

    #endregion

    #region Utility Methods

    private List<EntityCoordinates> GetCircularPositions(EntityCoordinates center, EntityUid mapUid,
        int count, float radius)
    {
        var positions = new List<EntityCoordinates>();

        for (int i = 0; i < count; i++)
        {
            var angle = i * (MathF.PI * 2 / count);
            var offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
            var pos = center.Offset(offset);

            if (!CanSpawnAt(pos))
            {
                var safePos = FindValidPositionNear(pos, radius);
                if (safePos == null)
                    continue;
                pos = safePos.Value;
            }

            positions.Add(pos);
        }

        return positions;
    }

    private List<EntityUid> SpawnIllusionCircle(Entity<BubblegumBossComponent> ent, EntityUid target,
        EntityCoordinates targetCoords, List<EntityCoordinates> positions, int bossIndex,
        EntProtoId illusionPrototype, DamageSpecifier damage)
    {
        var illusions = new List<EntityUid>();

        for (int i = 0; i < positions.Count; i++)
        {
            if (i == bossIndex)
            {
                PlaceBossAtPosition(ent, target, positions[i]);
            }
            else
            {
                var illusion = SpawnIllusion(ent, target, targetCoords, positions[i], illusionPrototype, damage);
                if (illusion != null)
                    illusions.Add(illusion.Value);
            }
        }

        return illusions;
    }

    private void PlaceBossAtPosition(Entity<BubblegumBossComponent> ent, EntityUid target, EntityCoordinates position)
    {
        if (!IsValidSpawnPosition(position))
        {
            var safePos = FindSafePositionNear(ent, position);
            if (safePos == null)
                return;
            position = safePos.Value;
        }

        _transform.SetCoordinates(ent.Owner, position);

        if (TryComp<BubblegumBossComponent>(ent.Owner, out var bossComp))
        {
            var direction = (_transform.GetWorldPosition(target) - _transform.GetWorldPosition(ent)).Normalized();
            SpawnAttachedTo(bossComp.DashTrail, position, rotation: GetDirectionRotation(direction));
            _audio.PlayPvs(bossComp.DashSound, ent.Owner);
        }
    }

    private EntityUid? SpawnIllusion(Entity<BubblegumBossComponent> ent, EntityUid target,
        EntityCoordinates targetCoords, EntityCoordinates position, EntProtoId prototype,
        DamageSpecifier damage)
    {
        if (!IsValidSpawnPosition(position))
            return null;

        var direction = (targetCoords.Position - position.Position).Normalized();
        var illusion = SpawnAttachedTo(prototype, position, rotation: GetDirectionRotation(direction));
        if (TryComp<BubblegumIllusionComponent>(illusion, out var illusionComp))
        {
            illusionComp.Master = ent.Owner;
            illusionComp.Target = target;
            illusionComp.TargetPosition = targetCoords;
            illusionComp.Damage = damage;
            return illusion;
        }

        return null;
    }

    private void StartIllusionDashForAll(List<EntityUid> illusions, EntityCoordinates targetCoords, DamageSpecifier damage)
    {
        foreach (var illusion in illusions)
        {
            if (Exists(illusion))
                StartIllusionDash(illusion, targetCoords, damage);
        }
    }

    private EntityCoordinates? FindValidPositionNear(EntityCoordinates center, float maxDistance)
    {
        for (int i = 0; i < 10; i++)
        {
            var angle = _random.NextFloat(0, MathF.PI * 2);
            var distance = _random.NextFloat(1f, maxDistance);
            var offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * distance;
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

    private Vector2 GetTileCenter(EntityUid mapUid, Vector2 position)
    {
        var coordinates = new EntityCoordinates(mapUid, position);
        var gridUid = _transform.GetGrid(coordinates);

        if (gridUid == null)
            return FindNearestValidPosition(mapUid, position);

        if (!TryComp<MapGridComponent>(gridUid, out var grid))
            return FindNearestValidPosition(mapUid, position);

        var tilePos = _map.CoordinatesToTile(gridUid.Value, grid, coordinates);
        return _map.GridTileToWorld(gridUid.Value, grid, tilePos).Position;
    }

    private float GetHealthRatio(EntityUid uid)
    {
        var totalDamage = _damage.GetTotalDamage(uid);
        if (!_threshold.TryGetThresholdForState(uid, MobState.Dead, out var threshold))
            return 1f;

        return 1f - (float)(totalDamage / threshold.Value.Double());
    }

    private bool IsValidMapPosition(EntityUid mapUid, Vector2 position)
    {
        var coordinates = new EntityCoordinates(mapUid, position);
        var gridUid = _transform.GetGrid(coordinates);

        if (gridUid == null)
            return false;

        if (!TryComp<MapGridComponent>(gridUid, out var grid))
            return false;

        var tilePos = _map.CoordinatesToTile(gridUid.Value, grid, coordinates);
        if (!_map.TryGetTileRef(gridUid.Value, grid, tilePos, out var tileRef))
            return false;

        return !_turf.IsTileBlocked(tileRef, CollisionGroup.Impassable);
    }

    private bool IsValidSpawnPosition(EntityCoordinates coords)
    {
        var mapUid = _transform.GetMap(coords);
        if (mapUid == null)
            return false;

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

    private EntityCoordinates? FindSafePositionNear(Entity<BubblegumBossComponent> ent, EntityCoordinates original)
    {
        var mapUid = _transform.GetMap(original);
        if (mapUid == null)
            return null;

        for (float radius = 0.5f; radius <= 10f; radius += 0.5f)
        {
            for (int angle = 0; angle < 360; angle += 45)
            {
                var rad = MathF.PI * angle / 180f;
                var offset = new Vector2(MathF.Cos(rad), MathF.Sin(rad)) * radius;
                var testPos = original.Position + offset;
                var testCoords = new EntityCoordinates(mapUid.Value, testPos);

                if (IsValidSpawnPosition(testCoords))
                    return testCoords;
            }
        }

        return FindNearestGridPosition(ent, mapUid.Value);
    }

    private EntityCoordinates? FindNearestGridPosition(Entity<BubblegumBossComponent> ent, EntityUid mapUid)
    {
        var gridQuery = EntityQueryEnumerator<MapGridComponent>();
        while (gridQuery.MoveNext(out var gridUid, out var grid))
        {
            if (Transform(gridUid).ParentUid != mapUid)
                continue;

            var gridCenter = _transform.GetWorldPosition(gridUid);
            var coords = new EntityCoordinates(mapUid, gridCenter);

            if (IsValidSpawnPosition(coords))
                return coords;
        }

        return null;
    }

    private Vector2 FindNearestValidPosition(EntityUid mapUid, Vector2 position)
    {
        var gridQuery = EntityQueryEnumerator<MapGridComponent>();
        while (gridQuery.MoveNext(out var gridUid, out _))
        {
            if (Transform(gridUid).ParentUid != mapUid)
                continue;

            var worldBounds = _transform.GetWorldPosition(gridUid);
            var gridRadius = 10f;

            for (float radius = 0.5f; radius <= gridRadius; radius += 0.5f)
            {
                for (int angle = 0; angle < 360; angle += 30)
                {
                    var rad = MathF.PI * angle / 180f;
                    var offset = new Vector2(MathF.Cos(rad), MathF.Sin(rad)) * radius;
                    var testPos = worldBounds + offset;

                    var testCoords = new EntityCoordinates(mapUid, testPos);
                    if (CanSpawnAt(testCoords))
                        return testPos;
                }
            }
        }

        return position;
    }

    private void CleanupIllusions(EntityUid boss)
    {
        if (_activeIllusions.TryGetValue(boss, out var illusions))
        {
            foreach (var illusion in illusions)
            {
                if (Exists(illusion))
                    QueueDel(illusion);
            }
            _activeIllusions.Remove(boss);
        }
    }

    private Angle GetDirectionRotation(Vector2 direction)
    {
        return direction == Vector2.Zero ? Angle.Zero
            : Angle.FromWorldVec(direction);
    }

    private void SetHTNTarget(Entity<BubblegumBossComponent> boss, EntityUid target)
    {
        if (!TryComp<HTNComponent>(boss, out var htn))
            return;

        if (htn.Blackboard.TryGetValue<EntityUid>(boss.Comp.TargetKey, out var targetEnt, EntityManager) && Exists(targetEnt))
            return;

        htn.Blackboard.SetValue(boss.Comp.TargetKey, target);
    }

    #endregion

    private sealed class StepCounter
    {
        public int CompletedSteps { get; set; }
        public int TotalSteps { get; set; }
    }
}
