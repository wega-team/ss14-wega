using System.Linq;
using System.Numerics;
using Content.Server.Administration;
using Content.Server.Antag;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Bible.Components;
using Content.Server.Destructible;
using Content.Server.Hallucinations;
using Content.Server.Prayer;
using Content.Server.Pinpointer;
using Content.Shared.Actions.Components;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Clothing;
using Content.Shared.CombatMode;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.DoAfter;
using Content.Shared.Ensnaring.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Content.Shared.Humanoid;
using Content.Shared.Maps;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Prying.Components;
using Content.Shared.Roles;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;
using Content.Shared.Standing;
using Content.Shared.Vampire;
using Content.Shared.Vampire.Components;
using Content.Shared.Weapons.Melee;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Shared.Damage.Systems;
using Content.Shared.Flash.Components;
using Content.Shared.NullRod.Components;
using Content.Shared.Surgery.Components;
using Content.Shared.StatusEffectNew;

namespace Content.Server.Vampire;

public sealed partial class VampireSystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly LoadoutSystem _loadout = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly SharedStealthSystem _stealth = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speed = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly TileSystem _tile = default!;
    [Dependency] private readonly PrayerSystem _prayerSystem = default!;
    [Dependency] private readonly QuickDialogSystem _quickDialog = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly HallucinationsSystem _hallucinations = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffect = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly MovementModStatusSystem _movementMod = default!;

    private static readonly EntProtoId RejuvenateAdvanced = "ActionVampireRejuvenateAdvanced";

    private void InitializePowers()
    {
        //Select Class
        SubscribeLocalEvent<VampireComponent, VampireSelectClassActionEvent>(SelectClass);
        SubscribeNetworkEvent<VampireSelectClassMenuClosedEvent>(OnClassSelected);

        //Abilities
        SubscribeLocalEvent<VampireComponent, VampireRejuvenateActionEvent>(OnRejuvenate);
        SubscribeLocalEvent<VampireComponent, VampireGlareActionEvent>(OnVampireGlare);

        // Hemomancer
        SubscribeLocalEvent<VampireComponent, VampireClawsActionEvent>(GiveVampireClaws);
        SubscribeLocalEvent<VampireComponent, VampireBloodTentacleAction>(OnBloodTendrils);
        SubscribeLocalEvent<VampireComponent, VampireBloodBarrierActionEvent>(OnBloodBarrierAction);
        SubscribeLocalEvent<VampireComponent, VampireSanguinePoolActionEvent>(OnSanguinePoolAction);
        SubscribeLocalEvent<VampireComponent, VampirePredatorSensesActionEvent>(OnVampirePredatorSensesAction);
        SubscribeLocalEvent<VampireComponent, VampireBloodEruptionActionEvent>(OnVampireBloodEruptionAction);
        SubscribeLocalEvent<VampireComponent, VampireBloodBringersRiteActionEvent>(OnBloodBringersRite);

        // Umbrae
        SubscribeLocalEvent<VampireComponent, VampireCloakOfDarknessActionEvent>(OnCloakOfDarkness);
        SubscribeLocalEvent<VampireComponent, VampireShadowSnareActionEvent>(OnShadowSnare);
        SubscribeLocalEvent<VampireComponent, VampireSoulAnchorActionEvent>(OnAfterSoulAnchor);
        SubscribeLocalEvent<VampireComponent, SoulAnchorDoAfterEvent>(OnSoulAnchorDoAfter);
        SubscribeLocalEvent<VampireComponent, VampireDarkPassageActionEvent>(OnVampireDarkPassage);
        SubscribeLocalEvent<VampireComponent, VampireExtinguishActionEvent>(OnExtinguish);
        SubscribeLocalEvent<VampireComponent, VampireShadowBoxingActionEvent>(OnShadowBoxing);
        SubscribeLocalEvent<VampireComponent, VampireEternalDarknessActionEvent>(OnEternalDarkness);

        // Gargantua
        SubscribeLocalEvent<VampireComponent, VampireRejuvenateAdvancedActionEvent>(OnRejuvenateAdvanced);
        SubscribeLocalEvent<VampireComponent, VampireBloodSwellActionEvent>(OnBloodSwell);
        SubscribeLocalEvent<VampireComponent, VampireBloodRushActionEvent>(OnBloodRush);
        SubscribeLocalEvent<VampireComponent, VampireSeismicStompActionEvent>(OnSeismicStomp);
        SubscribeLocalEvent<VampireComponent, VampireBloodSwellAdvancedActionEvent>(OnBloodSwellAdvanced);
        SubscribeLocalEvent<VampireComponent, VampireOverwhelmingForceActionEvent>(OnOverwhelmingForce);
        SubscribeLocalEvent<VampireComponent, VampireDemonicGraspActionEvent>(OnDemonicGrasp);
        SubscribeLocalEvent<VampireComponent, VampireChargeActionEvent>(OnCharge);

        // Dantalion
        SubscribeLocalEvent<VampireComponent, MaxThrallCountUpdateEvent>(MaxThrallCountUpdate);
        SubscribeLocalEvent<VampireComponent, VampireEnthrallActionEvent>(OnAfterEnthrall);
        SubscribeLocalEvent<VampireComponent, EnthrallDoAfterEvent>(OnEnthrallDoAfter);
        SubscribeLocalEvent<VampireComponent, VampireCommuneActionEvent>(OnCommune);
        SubscribeLocalEvent<VampireComponent, VampirePacifyActionEvent>(OnPacify);
        SubscribeLocalEvent<VampireComponent, VampireSubspaceSwapActionEvent>(OnSubspaceSwap);
        //SubscribeLocalEvent<VampireComponent, VampireDeployDecoyActionEvent>(OnDeployDecoy);
        SubscribeLocalEvent<VampireComponent, VampireRallyThrallsActionEvent>(OnRallyThralls);
        SubscribeLocalEvent<VampireComponent, VampireBloodBondActionEvent>(OnBloodBond);
        SubscribeLocalEvent<VampireComponent, VampireMassHysteriaActionEvent>(OnMassHysteria);
    }

    #region Select Class
    private void SelectClass(EntityUid uid, VampireComponent component, VampireSelectClassActionEvent args)
    {
        if (component.CurrentBlood >= 150)
        {
            var netEntity = _entityManager.GetNetEntity(uid);
            RaiseNetworkEvent(new SelectClassPressedEvent(netEntity));
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("vampire-hungry"), uid, uid, PopupType.SmallCaution);
            return;
        }
        args.Handled = true;
    }

    private void OnClassSelected(VampireSelectClassMenuClosedEvent args, EntitySessionEventArgs eventArgs)
    {
        var uid = _entityManager.GetEntity(args.Uid);
        if (!TryComp<ActionsContainerComponent>(uid, out var cont) || !TryComp<VampireComponent>(uid, out var vampire))
            return;

        var container = cont?.Container;
        if (container != null)
        {
            foreach (var actionId in container.ContainedEntities)
            {
                if (TryComp<InstantActionComponent>(actionId, out var instantActionComponent))
                {
                    if (instantActionComponent.Event is VampireSelectClassActionEvent)
                    {
                        _action.RemoveAction(uid, actionId);
                        _container.Remove(actionId, container);
                        break;
                    }
                }
            }
        }

        vampire.CurrentEvolution = args.SelectedClass;
        UpdatePowers(uid, vampire);

        if (args.SelectedClass is "Gargantua")
        {
            ReplaceVampireRejuvenateAction(uid);
        }
    }

    private void ReplaceVampireRejuvenateAction(EntityUid uid)
    {
        if (TryComp<ActionsContainerComponent>(uid, out var actionsContainer))
        {
            var container = actionsContainer?.Container;
            if (container != null)
            {
                foreach (var actionId in container.ContainedEntities)
                {
                    if (TryComp<InstantActionComponent>(actionId, out var instantActionComponent))
                    {
                        if (instantActionComponent.Event is VampireRejuvenateActionEvent)
                        {
                            _action.RemoveAction(uid, actionId);
                            _container.Remove(actionId, container);

                            _action.AddAction(uid, RejuvenateAdvanced);
                            break;
                        }
                    }
                }
            }
        }
    }
    #endregion

    #region Basic Abilities
    private void OnRejuvenate(EntityUid uid, VampireComponent component, VampireRejuvenateActionEvent args)
    {
        if (!_mobThreshold.TryGetThresholdForState(uid, MobState.Dead, out _))
        {
            _popup.PopupEntity(Loc.GetString("vampire-heal-dead"), uid, uid, PopupType.SmallCaution);
            return;
        }

        if (HasComp<StaminaComponent>(uid))
        {
            RemoveKnockdown(uid);
            _stamina.RemoveStaminaDamage(uid);
        }

        if (component.CurrentBlood >= 200)
        {
            var damageTypes = new[] { "Asphyxiation", "Blunt", "Slash", "Piercing", "Heat", "Poison" };

            var healingSpec = new DamageSpecifier();
            foreach (var type in damageTypes)
            {
                healingSpec.DamageDict[type] = type is "Asphyxiation" ? -5 : -2;
            }

            int count = 0;
            void Heal()
            {
                _damage.TryChangeDamage(uid, healingSpec, false, origin: uid);
                if (++count < 5)
                {
                    Timer.Spawn(3500, Heal);
                }
            }

            Heal();
        }

        args.Handled = true;
    }

    private void OnVampireGlare(EntityUid vampire, VampireComponent component, VampireGlareActionEvent ev)
    {
        var target = ev.Target;
        if (HasComp<VampireComponent>(target) || HasComp<FlashImmunityComponent>(target))
            return;

        if (HasComp<BibleUserComponent>(target) && !component.TruePowerActive)
        {
            _stun.TryUpdateParalyzeDuration(vampire, TimeSpan.FromSeconds(5f));
            _chat.TryEmoteWithoutChat(vampire, _prototypeManager.Index(Scream), true);
            _damage.TryChangeDamage(vampire, VampireComponent.HolyDamage);
            return;
        }

        _stun.TryUpdateParalyzeDuration(target, TimeSpan.FromSeconds(5f));
        ev.Handled = true;
    }
    #endregion

    #region Hemomancer Abilities
    private void GiveVampireClaws(EntityUid uid, VampireComponent component, VampireClawsActionEvent args)
    {
        if (!CheckBloodEssence(uid, 20))
        {
            _popup.PopupEntity(Loc.GetString("vampire-blood-sacrifice-insufficient-blood"), uid, uid, PopupType.SmallCaution);
            return;
        }

        var vampireClawsGear = new ProtoId<StartingGearPrototype>("VampireClawsGear");

        var dropEvent = new DropHandItemsEvent();
        RaiseLocalEvent(uid, ref dropEvent);
        List<ProtoId<StartingGearPrototype>> gear = new() { vampireClawsGear };
        _loadout.Equip(uid, gear, null);

        SubtractBloodEssence(uid, 20);
        args.Handled = true;
    }

    private void OnBloodTendrils(EntityUid uid, VampireComponent component, VampireBloodTentacleAction args)
    {
        if (args.Handled)
            return;

        if (!CheckBloodEssence(uid, 10))
        {
            _popup.PopupEntity(Loc.GetString("vampire-blood-sacrifice-insufficient-blood"), uid, uid, PopupType.SmallCaution);
            return;
        }

        var coords = args.Target;
        List<EntityCoordinates> spawnPos = new();
        spawnPos.Add(coords);

        var dirs = new List<Direction>();
        dirs.AddRange(args.OffsetDirections);

        for (var i = 0; i < args.ExtraSpawns; i++)
        {
            var dir = _random.PickAndTake(dirs);
            var vector = DirectionToVector2(dir);
            spawnPos.Add(coords.Offset(vector));
        }

        if (_transform.GetGrid(coords) is not { } grid || !TryComp<MapGridComponent>(grid, out var gridComp))
            return;

        foreach (var pos in spawnPos)
        {
            if (!_map.TryGetTileRef(grid, gridComp, pos, out var tileRef) ||
                _turf.IsTileBlocked(tileRef, CollisionGroup.Impassable))
                continue;

            if (_net.IsServer)
                Spawn(args.EntityId, pos);
        }

        SubtractBloodEssence(uid, 10);
        args.Handled = true;
    }

    private void OnBloodBarrierAction(EntityUid uid, VampireComponent component, VampireBloodBarrierActionEvent args)
    {
        if (!CheckBloodEssence(uid, 20))
        {
            _popup.PopupEntity(Loc.GetString("vampire-blood-sacrifice-insufficient-blood"), uid, uid, PopupType.SmallCaution);
            return;
        }

        var targetCoords = args.Target;
        if (args.UseCasterDirection)
        {
            var transform = Transform(uid);
            var direction = transform.LocalRotation.ToWorldVec().Normalized();

            var perpendicularDirection = new Vector2(-direction.Y, direction.X);

            var objectCount = 0;

            for (int i = -1; i <= 1 && objectCount < 3; i++)
            {
                var spawnPosition = targetCoords.Offset(perpendicularDirection * (1f * i));

                if (TrySpawnObjectAtPosition(spawnPosition, args.EntityId, uid))
                    objectCount++;
            }

            SubtractBloodEssence(uid, 20);
            args.Handled = true;
        }
    }

    public void OnSanguinePoolAction(EntityUid uid, VampireComponent component, VampireSanguinePoolActionEvent args)
    {
        if (!CheckBloodEssence(uid, 30))
        {
            _popup.PopupEntity(Loc.GetString("vampire-blood-sacrifice-insufficient-blood"), uid, uid, PopupType.SmallCaution);
            return;
        }

        var polymorphedEntity = _polymorph.PolymorphEntity(uid, args.PolymorphProto);
        if (polymorphedEntity is null)
            return;

        if (args.Sound != null)
            _audio.PlayPvs(args.Sound, uid);

        SubtractBloodEssence(uid, 30);
        args.Handled = true;
    }

    private void OnVampirePredatorSensesAction(EntityUid uid, VampireComponent component, VampirePredatorSensesActionEvent args)
    {
        var mapCoords = _transform.GetMapCoordinates(uid);
        var nearbyHumanoids = _entityLookup.GetEntitiesInRange<HumanoidAppearanceComponent>(mapCoords, 6f);

        foreach (var humanoidEntity in nearbyHumanoids)
        {
            var humanoid = humanoidEntity.Owner;
            if (humanoid == uid) continue;

            if (TryComp(humanoid, out TransformComponent? transform))
            {
                Spawn(args.Proto, transform.Coordinates);
                _audio.PlayPvs(args.Sound, humanoid);
                _popup.PopupEntity(Loc.GetString("vampire-predator-senses-puddle"), humanoid, uid, PopupType.SmallCaution);
                _stun.TryUpdateParalyzeDuration(humanoid, TimeSpan.FromSeconds(4));
                break;
            }
        }

        /// TODO почнить это говно -> Реально говно
        /*if (nearbyHumanoids is null)
        {
            var mapId = _transform.GetMapId(uid);
            var allHumanoidsOnMap = new HashSet<Entity<HumanoidAppearanceComponent>>();
            _entityLookup.GetEntitiesOnMap<HumanoidAppearanceComponent>(mapId, allHumanoidsOnMap);

            EntityUid? closestEntity = null;
            float closestDistance = 300f;
            foreach (var humanoidEntity in allHumanoidsOnMap)
            {
                var humanoidUid = humanoidEntity.Owner;
                var humanoidCoords = _transform.GetMapCoordinates(humanoidUid);

                Vector2 humanoidPosition = humanoidCoords.Position;
                Vector2 currentPosition = mapCoords.Position;

                float distance = Vector2.Distance(currentPosition, humanoidPosition);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEntity = humanoidUid;
                }
            }

            if (closestEntity != null)
            {
                if (TryComp(closestEntity.Value, out TransformComponent? closestTransform))
                {
                    var closestEntityTransform = new Entity<TransformComponent?>(closestEntity.Value, closestTransform);
                    var msg = Loc.GetString("vampire-predator-senses-warning",
                        ("location", FormattedMessage.RemoveMarkupOrThrow(_navMap.GetNearestBeaconString(closestEntityTransform))));
                    _popup.PopupEntity(msg, closestEntity.Value, uid, PopupType.MediumCaution);
                }
            }
            else
            {
                _popup.PopupEntity("vampire-predator-senses-nobody", uid, uid, PopupType.SmallCaution);
            }
        }
        else
        {
            foreach (var humanoidEntity in nearbyHumanoids)
            {
                var humanoid = humanoidEntity.Owner;
                if (humanoid == uid) continue;

                if (TryComp(humanoid, out TransformComponent? transform))
                {
                    Spawn(args.Proto, transform.Coordinates);
                    _audio.PlayPvs(args.Sound, humanoid);
                    _popup.PopupEntity(Loc.GetString("vampire-predator-senses-puddle"), humanoid, uid, PopupType.SmallCaution);
                    _stun.TryParalyze(humanoid, TimeSpan.FromSeconds(4), true);
                    break;
                }
            }
        }*/

        args.Handled = true;
    }

    private void OnVampireBloodEruptionAction(EntityUid uid, VampireComponent component, VampireBloodEruptionActionEvent args)
    {
        if (!TryComp(uid, out TransformComponent? vampireTransform))
            return;

        if (!CheckBloodEssence(uid, 50))
        {
            _popup.PopupEntity(Loc.GetString("vampire-blood-sacrifice-insufficient-blood"), uid, uid, PopupType.SmallCaution);
            return;
        }

        var puddlesInRange = _entityLookup
            .GetEntitiesInRange<PuddleComponent>(Transform(uid).Coordinates, 4f)
            .Where(puddle => TryComp(puddle.Owner, out ContainerManagerComponent? containerManager) &&
                            containerManager.Containers.TryGetValue("solution@puddle", out var container) &&
                            container.ContainedEntities.Any(containedEntity =>
                                TryComp(containedEntity, out SolutionComponent? solutionComponent) &&
                                solutionComponent.Solution.Contents.Any(r =>
                                    r.Reagent.Prototype == "Blood" || r.Reagent.Prototype == "CopperBlood")))
            .ToList();

        foreach (var puddleEntity in puddlesInRange)
        {
            if (!TryComp(puddleEntity.Owner, out TransformComponent? puddleTransform))
                continue;

            var entitiesOnPuddle = _entityLookup
                .GetEntitiesInRange<TransformComponent>(puddleTransform.Coordinates, 0.1f)
                .Where(entity => entity.Owner != uid)
                .ToList();

            foreach (var targetEntity in entitiesOnPuddle)
            {
                if (TryComp(targetEntity.Owner, out DamageableComponent? damageable))
                {
                    if (HasComp<NullRodOwnerComponent>(targetEntity.Owner) && !component.TruePowerActive)
                        continue;

                    var damage = new DamageSpecifier { DamageDict = { { "Blunt", 50 } } };

                    _damage.TryChangeDamage(targetEntity.Owner, damage, ignoreResistances: false, origin: uid);
                    _stun.TryUpdateParalyzeDuration(targetEntity.Owner, TimeSpan.FromSeconds(3));
                    _popup.PopupEntity(Loc.GetString("vampire-blood-eruption-effect-message"), targetEntity.Owner, uid, PopupType.SmallCaution);
                }
            }
        }

        SubtractBloodEssence(uid, 50);
        args.Handled = true;
    }

    private void OnBloodBringersRite(EntityUid uid, VampireComponent component, VampireBloodBringersRiteActionEvent args)
    {
        if (component.PowerActive)
        {
            component.PowerActive = false;
            args.Handled = true;
            return;
        }

        component.PowerActive = true;
        _popup.PopupEntity(Loc.GetString("vampire-blood-true-power-started"), uid, uid, PopupType.SmallCaution);

        bool bloodSpawned = false;

        var damageToEnemies = new DamageSpecifier { DamageDict = { { "Bloodloss", 5 } } };
        var baseHealingSpec = new DamageSpecifier { DamageDict = { { "Blunt", -8 }, { "Slash", -8 }, { "Piercing", -8 }, { "Heat", -1.5f } } };

        void ExecuteTick()
        {
            if (Deleted(uid) || !component.PowerActive)
            {
                component.PowerActive = false;
                return;
            }

            if (component.CurrentBlood < 10)
            {
                _popup.PopupEntity(Loc.GetString("vampire-blood-sacrifice-insufficient-blood"), uid, uid, PopupType.SmallCaution);
                component.PowerActive = false;
                return;
            }

            SubtractBloodEssence(uid, 10);

            var mapCoords = _transform.GetMapCoordinates(uid);
            var nearbyEntities = _entityLookup.GetEntitiesInRange<BodyComponent>(mapCoords, 7f)
                .Where(entity => entity.Owner != uid && !Deleted(entity.Owner))
                .Where(entity => TryComp(entity.Owner, out MobStateComponent? mobState) && mobState.CurrentState != MobState.Dead)
                .ToList();

            int victimCount = nearbyEntities.Count;

            if (victimCount > 0)
            {
                var scaledHealingSpec = baseHealingSpec * victimCount;

                _damage.TryChangeDamage(uid, scaledHealingSpec, ignoreResistances: false, origin: uid);

                if (TryComp(uid, out StaminaComponent? staminaComponent))
                    staminaComponent.StaminaDamage = Math.Max(0, staminaComponent.StaminaDamage - 15f * victimCount);

                if (!bloodSpawned)
                {
                    foreach (var entity in nearbyEntities)
                    {
                        if (HasComp<NullRodOwnerComponent>(entity.Owner) && !component.TruePowerActive)
                            continue;

                        _damage.TryChangeDamage(entity.Owner, damageToEnemies, ignoreResistances: false, origin: uid);

                        if (TryComp(entity.Owner, out TransformComponent? transform))
                        {
                            Spawn(args.Proto, transform.Coordinates);
                        }
                    }

                    _audio.PlayPvs(args.Sound, uid);
                    bloodSpawned = true;
                }
            }

            Timer.Spawn((int)(1f * 1000), ExecuteTick);
        }

        ExecuteTick();
        SubtractBloodEssence(uid, 10);
        args.Handled = true;
    }
    #endregion

    #region Umbrae Abilities
    private void OnCloakOfDarkness(EntityUid uid, VampireComponent component, VampireCloakOfDarknessActionEvent args)
    {
        if (!HasComp<StealthComponent>(uid))
        {
            var newStealth = EnsureComp<StealthComponent>(uid);
            _stealth.SetVisibility(uid, 0.3f, newStealth);
            _stealth.SetEnabled(uid, false, newStealth);
        }

        if (TryComp(uid, out MovementSpeedModifierComponent? speedmodComponent))
        {
            var originalWalkSpeed = speedmodComponent.BaseWalkSpeed;
            var originalSprintSpeed = speedmodComponent.BaseSprintSpeed;
            var stealthComponent = _entityManager.GetComponent<StealthComponent>(uid);
            if (TryComp(uid, out MobStateComponent? mobState) && mobState.CurrentState == MobState.PreCritical)
            {
                args.Handled = true;
                return;
            }

            if (stealthComponent.Enabled)
            {
                _stealth.SetEnabled(uid, false, stealthComponent);
                _speed.ChangeBaseSpeed(uid, originalWalkSpeed / 1.3f, originalSprintSpeed / 1.3f, speedmodComponent.Acceleration, speedmodComponent);
                _popup.PopupEntity(Loc.GetString("vampire-stealth-disabled"), uid, uid, PopupType.Small);
            }
            else
            {
                if (!CheckBloodEssence(uid, 10))
                {
                    _popup.PopupEntity(Loc.GetString("vampire-blood-sacrifice-insufficient-blood"), uid, uid, PopupType.SmallCaution);
                    return;
                }

                _stealth.SetEnabled(uid, true, stealthComponent);
                _speed.ChangeBaseSpeed(uid, originalWalkSpeed * 1.3f, originalSprintSpeed * 1.3f, speedmodComponent.Acceleration, speedmodComponent);
                _popup.PopupEntity(Loc.GetString("vampire-stealth-enabled"), uid, uid, PopupType.Small);
                SubtractBloodEssence(uid, 10);
            }
        }

        args.Handled = true;
    }

    private void OnShadowSnare(EntityUid uid, VampireComponent component, VampireShadowSnareActionEvent args)
    {
        if (!CheckBloodEssence(uid, 20))
        {
            _popup.PopupEntity(Loc.GetString("vampire-blood-sacrifice-insufficient-blood"), uid, uid, PopupType.SmallCaution);
            return;
        }

        var targetCoords = args.Target;
        if (TrySpawnObjectAtPosition(targetCoords, args.EntityId, uid))
        {
            SubtractBloodEssence(uid, 20);
            args.Handled = true;
        }
    }

    private void OnAfterSoulAnchor(EntityUid uid, VampireComponent component, VampireSoulAnchorActionEvent args)
    {
        if (!CheckBloodEssence(uid, 30))
        {
            _popup.PopupEntity(Loc.GetString("vampire-blood-sacrifice-insufficient-blood"), uid, uid, PopupType.SmallCaution);
            return;
        }

        var allEntities = _entityManager.GetEntities();
        EntityUid? beaconEntity = null;

        foreach (var entity in allEntities)
        {
            if (_entityManager.HasComponent<BeaconSoulComponent>(entity))
            {
                var beaconComponent = _entityManager.GetComponent<BeaconSoulComponent>(entity);
                if (beaconComponent.VampireOwner == uid)
                {
                    beaconEntity = entity;
                    break;
                }
            }
        }

        if (beaconEntity.HasValue)
        {
            RaiseLocalEvent(uid, new SoulAnchorDoAfterEvent());
            args.Handled = true;
            return;
        }

        args.Handled = true;
        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, uid, TimeSpan.FromSeconds(15f), new SoulAnchorDoAfterEvent(), uid)
        {
            BreakOnMove = false,
            NeedHand = false,
        });
    }


    private void OnSoulAnchorDoAfter(EntityUid uid, VampireComponent component, DoAfterEvent args)
    {
        var allEntities = _entityManager.GetEntities();
        EntityUid? beaconEntity = null;

        foreach (var entity in allEntities)
        {
            if (_entityManager.HasComponent<BeaconSoulComponent>(entity))
            {
                var beaconComponent = _entityManager.GetComponent<BeaconSoulComponent>(entity);
                if (beaconComponent.VampireOwner == uid)
                {
                    beaconEntity = entity;
                    break;
                }
            }
        }

        if (beaconEntity.HasValue)
        {
            var beaconTransform = _entityManager.GetComponent<TransformComponent>(beaconEntity.Value);
            var beaconCoords = beaconTransform.Coordinates;

            _transform.SetCoordinates(uid, beaconCoords);
            _entityManager.DeleteEntity(beaconEntity.Value);

            SubtractBloodEssence(uid, 30);
        }
        else
        {
            var playerTransform = _entityManager.GetComponent<TransformComponent>(uid);
            var coords = playerTransform.Coordinates;

            var beaconEntityNew = _entityManager.SpawnEntity("BeaconSoul", coords);
            var beaconComponent = _entityManager.EnsureComponent<BeaconSoulComponent>(beaconEntityNew);
            beaconComponent.VampireOwner = uid;
        }
    }

    private void OnVampireDarkPassage(EntityUid uid, VampireComponent component, VampireDarkPassageActionEvent args)
    {
        var targetCoords = args.Target;
        if (!_interaction.InRangeUnobstructed(uid, targetCoords, range: 1000F, collisionMask: CollisionGroup.Impassable, popup: false))
        {
            _popup.PopupEntity(Loc.GetString("vampire-teleport-failed"), uid, uid, PopupType.Small);
            return;
        }

        if (!CheckBloodEssence(uid, 30))
        {
            _popup.PopupEntity(Loc.GetString("vampire-blood-sacrifice-insufficient-blood"), uid, uid, PopupType.SmallCaution);
            return;
        }

        if (HasComp<NullRodOwnerComponent>(targetCoords.EntityId) && !component.TruePowerActive)
            return;

        var currentCoords = Transform(uid).Coordinates;
        _transform.SetCoordinates(uid, targetCoords);
        _entityManager.SpawnEntity("VampireMistEffect", currentCoords);
        _entityManager.SpawnEntity("VampireMistReappearEffect", targetCoords);

        SubtractBloodEssence(uid, 30);
        args.Handled = true;
    }

    private void OnExtinguish(EntityUid uid, VampireComponent component, VampireExtinguishActionEvent args)
    {
        if (!TryComp(uid, out TransformComponent? vampireTransform))
            return;

        var originCoords = _transform.GetMapCoordinates(uid);
        var lightsInRange = _entityLookup
            .GetEntitiesInRange<PointLightComponent>(originCoords, 15f)
            .Where(entity => TryComp(entity.Owner, out DamageableComponent? _))
            .ToList();

        foreach (var lightEntity in lightsInRange)
        {
            if (!TryComp(lightEntity.Owner, out DamageableComponent? damageable))
                continue;

            var damage = new DamageSpecifier { DamageDict = { { "Blunt", 5 } } };

            _damage.TryChangeDamage(lightEntity.Owner, damage, ignoreResistances: true, origin: uid);
        }

        args.Handled = true;
    }

    private void OnShadowBoxing(EntityUid uid, VampireComponent component, VampireShadowBoxingActionEvent args)
    {
        if (!CheckBloodEssence(uid, 50))
        {
            _popup.PopupEntity(Loc.GetString("vampire-blood-sacrifice-insufficient-blood"), uid, uid, PopupType.SmallCaution);
            return;
        }

        int currentTick = 0;

        void ExecuteTick()
        {
            if (Deleted(uid) || currentTick >= 10)
                return;

            currentTick++;

            _entityManager.SpawnEntity("MobFollowerShadow", Transform(uid).Coordinates);

            Timer.Spawn((int)(1f * 1000), ExecuteTick);
        }

        ExecuteTick();
        SubtractBloodEssence(uid, 50);
        args.Handled = true;
    }

    private void OnEternalDarkness(EntityUid uid, VampireComponent component, VampireEternalDarknessActionEvent args)
    {
        var netEntity = _entityManager.GetNetEntity(uid);
        if (component.PowerActive)
        {
            component.PowerActive = false;
            RaiseNetworkEvent(new VampireToggleFovEvent(netEntity));
            args.Handled = true;
            return;
        }

        component.PowerActive = true;
        RaiseNetworkEvent(new VampireToggleFovEvent(netEntity));
        _popup.PopupEntity(Loc.GetString("vampire-blood-true-power-started"), uid, uid, PopupType.SmallCaution);

        void ExecuteTick()
        {
            if (Deleted(uid) || !component.PowerActive)
            {
                component.PowerActive = false;
                args.Handled = true;
                return;
            }

            if (component.CurrentBlood < 5)
            {
                _popup.PopupEntity(Loc.GetString("vampire-blood-sacrifice-insufficient-blood"), uid, uid, PopupType.SmallCaution);
                component.PowerActive = false;
                RaiseNetworkEvent(new VampireToggleFovEvent(netEntity));
                return;
            }

            CoolSurroundingAtmosphere(uid);
            SubtractBloodEssence(uid, 5);
            Timer.Spawn((int)(1f * 1000), ExecuteTick);
        }

        ExecuteTick();
        SubtractBloodEssence(uid, 5);
        args.Handled = true;
    }
    #endregion

    #region Gargantua Abilities
    private void OnRejuvenateAdvanced(EntityUid uid, VampireComponent component, VampireRejuvenateAdvancedActionEvent args)
    {
        if (!_mobThreshold.TryGetThresholdForState(uid, MobState.Dead, out var health))
        {
            _popup.PopupEntity(Loc.GetString("vampire-heal-dead"), uid, uid, PopupType.SmallCaution);
            return;
        }

        if (HasComp<StaminaComponent>(uid))
        {
            RemoveKnockdown(uid);
            _stamina.RemoveStaminaDamage(uid);
        }

        if (component.CurrentBlood >= 200 && TryComp<DamageableComponent>(uid, out var damageableComponent))
        {
            var totalDamage = damageableComponent.Damage.DamageDict.Values.Sum(d => d.Float());

            float CalculateHealing(float damage, float minHeal, float maxHeal)
            {
                return damage <= 100 ? MathHelper.Lerp(minHeal, maxHeal, damage / 100f) : maxHeal;
            }

            var healingSpec = new DamageSpecifier
            {
                DamageDict = new Dictionary<string, FixedPoint2> { { "Asphyxiation", FixedPoint2.New(CalculateHealing(totalDamage, -5f, -25f)) } }
            };

            var otherDamageTypes = new[] { "Blunt", "Slash", "Piercing", "Heat", "Poison" };
            foreach (var damageType in otherDamageTypes)
            {
                healingSpec.DamageDict[damageType] = FixedPoint2.New(CalculateHealing(totalDamage, -2f, -10f));
            }

            int count = 0;
            void Heal()
            {
                _damage.TryChangeDamage(uid, healingSpec, false, origin: uid);
                if (++count < 5)
                {
                    Timer.Spawn(3500, Heal);
                }
            }

            Heal();
        }

        args.Handled = true;
    }

    private void OnBloodSwell(EntityUid uid, VampireComponent component, VampireBloodSwellActionEvent args)
    {
        if (!CheckBloodEssence(uid, 30))
        {
            _popup.PopupEntity(Loc.GetString("vampire-blood-sacrifice-insufficient-blood"), uid, uid, PopupType.SmallCaution);
            return;
        }

        _damage.SetDamageModifierSetId(uid, "VampireBloodSwell");

        Timer.Spawn(30000, () => { _damage.SetDamageModifierSetId(uid, "Vampire"); });

        SubtractBloodEssence(uid, 30);
        args.Handled = true;
    }

    private void OnBloodRush(EntityUid uid, VampireComponent component, VampireBloodRushActionEvent args)
    {
        if (!CheckBloodEssence(uid, 15))
        {
            _popup.PopupEntity(Loc.GetString("vampire-blood-sacrifice-insufficient-blood"), uid, uid, PopupType.SmallCaution);
            return;
        }

        if (TryComp(uid, out MovementSpeedModifierComponent? speedmodComponent))
        {
            var originalWalkSpeed = speedmodComponent.BaseWalkSpeed;
            var originalSprintSpeed = speedmodComponent.BaseSprintSpeed;

            _speed.ChangeBaseSpeed(uid, originalWalkSpeed * 2, originalSprintSpeed * 2, speedmodComponent.Acceleration, speedmodComponent);

            Timer.Spawn(10000, () =>
            {
                if (TryComp(uid, out MovementSpeedModifierComponent? speedmodComponentAfter))
                {
                    _speed.ChangeBaseSpeed(uid, originalWalkSpeed, originalSprintSpeed, speedmodComponent.Acceleration, speedmodComponentAfter);
                }
            });
        }

        SubtractBloodEssence(uid, 15);
        args.Handled = true;
    }

    private void OnSeismicStomp(EntityUid uid, VampireComponent component, VampireSeismicStompActionEvent args)
    {
        if (TryComp(uid, out EnsnareableComponent? ensnareable) && ensnareable.IsEnsnared)
        {
            _popup.PopupEntity(Loc.GetString("vampire-legs-ensnared"), uid, uid, PopupType.Medium);
            return;
        }

        if (!CheckBloodEssence(uid, 25))
        {
            _popup.PopupEntity(Loc.GetString("vampire-blood-sacrifice-insufficient-blood"), uid, uid, PopupType.SmallCaution);
            return;
        }

        var vampirePosition = _transform.GetWorldPosition(uid);
        var gridUid = _transform.GetGrid(uid);
        if (gridUid != null && TryComp<MapGridComponent>(gridUid.Value, out var grid))
        {
            var tiles = _map.GetTilesIntersecting(gridUid.Value, grid,
                Box2.CenteredAround(vampirePosition, new Vector2(6, 6)), ignoreEmpty: true);

            foreach (var tile in tiles)
            {
                if (!_random.Prob(0.5f))
                    continue;

                _tile.PryTile(tile);
            }
        }

        var transform = Transform(uid);
        var nearbyHumanoids = _entityLookup.GetEntitiesInRange<BodyComponent>(transform.Coordinates, 3f);
        foreach (var humanoid in nearbyHumanoids)
        {
            var humanoidUid = humanoid.Owner;
            if (humanoidUid == uid) continue;

            if (HasComp<NullRodOwnerComponent>(humanoidUid) && !component.TruePowerActive)
                continue;

            if (!_entityManager.TryGetComponent(humanoid, out PhysicsComponent? physics)
                || !_entityManager.TryGetComponent(humanoid, out TransformComponent? humanoidTransform))
                continue;

            var humanoidPosition = _transform.GetWorldPosition(humanoid);
            var direction = (humanoidPosition - vampirePosition).Normalized();
            var force = 10000f;
            if (physics.Mass < 80f)
            {
                force *= 2;
            }

            _physics.ApplyLinearImpulse(humanoid, direction * force, body: physics);
        }

        _audio.PlayPvs(args.Sound, uid);
        SubtractBloodEssence(uid, 25);
        args.Handled = true;
    }

    private void OnBloodSwellAdvanced(EntityUid uid, VampireComponent component, VampireBloodSwellAdvancedActionEvent args)
    {
        if (!CheckBloodEssence(uid, 30))
        {
            _popup.PopupEntity(Loc.GetString("vampire-blood-sacrifice-insufficient-blood"), uid, uid, PopupType.SmallCaution);
            return;
        }

        if (TryComp(uid, out MeleeWeaponComponent? meleeWeapon))
        {
            FixedPoint2? oldBluntDamage = null;

            _damage.SetDamageModifierSetId(uid, "VampireBloodSwell");

            if (meleeWeapon.Damage.DamageDict.ContainsKey("Blunt"))
            {
                oldBluntDamage = meleeWeapon.Damage.DamageDict["Blunt"];
                meleeWeapon.Damage.DamageDict["Blunt"] += FixedPoint2.New(14);
            }
            else
            {
                meleeWeapon.Damage.DamageDict["Blunt"] = FixedPoint2.New(14);
            }

            Timer.Spawn(30000, () =>
            {
                if (oldBluntDamage.HasValue && meleeWeapon.Damage.DamageDict.ContainsKey("Blunt"))
                {
                    meleeWeapon.Damage.DamageDict["Blunt"] = oldBluntDamage.Value;
                }
                _damage.SetDamageModifierSetId(uid, "Vampire");
            });
        }

        SubtractBloodEssence(uid, 30);
        args.Handled = true;
    }

    private void OnOverwhelmingForce(EntityUid uid, VampireComponent component, VampireOverwhelmingForceActionEvent args)
    {
        if (TryComp(uid, out EnsnareableComponent? ensnareable) && ensnareable.IsEnsnared)
        {
            _popup.PopupEntity(Loc.GetString("vampire-legs-ensnared"), uid, uid, PopupType.Medium);
            return;
        }

        if (!CheckBloodEssence(uid, 10))
        {
            _popup.PopupEntity(Loc.GetString("vampire-blood-sacrifice-insufficient-blood"), uid, uid, PopupType.SmallCaution);
            return;
        }

        if (_entityManager.HasComponent<PryingComponent>(uid))
        {
            _entityManager.RemoveComponent<PryingComponent>(uid);
            _entityManager.AddComponent<EnsnareableComponent>(uid);
        }
        else
        {
            var pryComponent = _entityManager.AddComponent<PryingComponent>(uid);
            pryComponent.PryPowered = true;
            pryComponent.Force = true;
            pryComponent.SpeedModifier = 1.5f;
            pryComponent.UseSound = new SoundPathSpecifier("/Audio/Items/crowbar.ogg");

            if (_entityManager.HasComponent<EnsnareableComponent>(uid))
            {
                _entityManager.RemoveComponent<EnsnareableComponent>(uid);
            }
        }

        SubtractBloodEssence(uid, 10);
        args.Handled = true;
    }

    private void OnDemonicGrasp(EntityUid uid, VampireComponent component, VampireDemonicGraspActionEvent args)
    {
        if (!TryComp<CombatModeComponent>(uid, out var combatMode))
            return;

        if (!CheckBloodEssence(uid, 10))
        {
            _popup.PopupEntity(Loc.GetString("vampire-blood-sacrifice-insufficient-blood"), uid, uid, PopupType.SmallCaution);
            return;
        }

        var target = args.Target;
        if (HasComp<NullRodOwnerComponent>(target) && !component.TruePowerActive)
            return;

        var vampirePosition = _transform.GetWorldPosition(uid);
        var targetPosition = _transform.GetWorldPosition(target);
        var direction = (vampirePosition - targetPosition).Normalized();

        if (TryComp<PhysicsComponent>(target, out var physics))
        {
            if (!combatMode.IsInCombatMode)
            {
                _physics.ApplyLinearImpulse(target, -direction * 5000f, body: physics);
                _stun.TryUpdateStunDuration(target, TimeSpan.FromSeconds(3f));
            }
            else
            {
                _physics.ApplyLinearImpulse(args.Target, direction * 5000f, body: physics);
                _stun.TryUpdateStunDuration(target, TimeSpan.FromSeconds(3f));
            }
        }

        SubtractBloodEssence(uid, 10);
        args.Handled = true;
    }

    private void OnCharge(EntityUid uid, VampireComponent component, VampireChargeActionEvent args)
    {
        if (!TryComp(uid, out TransformComponent? vampireTransform))
            return;

        if (TryComp(uid, out EnsnareableComponent? ensnareable) && ensnareable.IsEnsnared)
        {
            _popup.PopupEntity(Loc.GetString("vampire-legs-ensnared"), uid, uid, PopupType.Medium);
            return;
        }

        if (!CheckBloodEssence(uid, 30))
        {
            _popup.PopupEntity(Loc.GetString("vampire-blood-sacrifice-insufficient-blood"), uid, uid, PopupType.SmallCaution);
            return;
        }

        var coords = args.Target;
        var transformSystem = _entityManager.System<SharedTransformSystem>();
        var vampirePosition = _transform.GetWorldPosition(uid);
        var targetPosition = transformSystem.ToMapCoordinates(coords, true).Position;
        var direction = (targetPosition - vampirePosition).Normalized();

        if (TryComp(uid, out PhysicsComponent? vampirePhysics))
            _physics.ApplyLinearImpulse(uid, direction * 20000f, body: vampirePhysics);

        if (args.Entity is not { } targetEntity)
        {
            _audio.PlayPvs(args.Sound, uid);
            SubtractBloodEssence(uid, 30);
            args.Handled = true;
            return;
        }

        if (HasComp<NullRodOwnerComponent>(targetEntity) && !component.TruePowerActive)
            return;

        if (_entityManager.TryGetComponent(targetEntity, out DestructibleComponent? _))
        {
            var damage = new DamageSpecifier { DamageDict = { { "Structural", 150 } } };
            _damage.TryChangeDamage(targetEntity, damage, origin: uid);
        }

        if (_entityManager.TryGetComponent(targetEntity, out BodyComponent? _))
        {
            var damage = new DamageSpecifier { DamageDict = { { "Blunt", 60 } } };
            _damage.TryChangeDamage(targetEntity, damage, ignoreResistances: false, origin: uid);

            if (_entityManager.TryGetComponent(targetEntity, out PhysicsComponent? physics))
                _physics.ApplyLinearImpulse(targetEntity, direction * 1000f, body: physics);

            _stun.TryUpdateParalyzeDuration(targetEntity, TimeSpan.FromSeconds(10f));
        }

        _audio.PlayPvs(args.Sound, uid);
        SubtractBloodEssence(uid, 30);
        args.Handled = true;
    }
    #endregion

    #region Dantalion Abilities
    private void MaxThrallCountUpdate(EntityUid uid, VampireComponent component, MaxThrallCountUpdateEvent args)
    {
        component.MaxThrallCount++;

        _action.RemoveAction(uid, args.Action!);
    }

    private void OnAfterEnthrall(EntityUid uid, VampireComponent component, VampireEnthrallActionEvent args)
    {
        var target = args.Target;
        if (component.ThrallCount >= component.MaxThrallCount)
        {
            _popup.PopupEntity(Loc.GetString("vampire-max-trall-reached"), uid, uid, PopupType.Medium);
            return;
        }

        if (HasComp<VampireComponent>(target) || HasComp<MindShieldComponent>(target) || HasComp<BibleUserComponent>(target)
            || HasComp<SyntheticOperatedComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("vampire-enthall-failed", ("target", target)), uid, uid);
            return;
        }

        if (TryComp<ThrallComponent>(target, out var trallComponent))
        {
            if (trallComponent.VampireOwner == uid)
            {
                _popup.PopupEntity(Loc.GetString("vampire-enthall-already", ("target", target)), uid, uid);
                return;
            }
            else
            {
                _popup.PopupEntity(Loc.GetString("vampire-enthall-failed", ("target", target)), uid, uid);
                return;
            }
        }

        if (!CheckBloodEssence(uid, 150))
        {
            _popup.PopupEntity(Loc.GetString("vampire-blood-sacrifice-insufficient-blood"), uid, uid, PopupType.SmallCaution);
            return;
        }

        args.Handled = true;
        var netTarget = _entityManager.GetNetEntity(target);
        _popup.PopupEntity(Loc.GetString("vampire-blooddrink-countion"), uid, args.Target, PopupType.MediumCaution);
        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, uid, TimeSpan.FromSeconds(15f), new EnthrallDoAfterEvent(netTarget), uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 0.01f,
            DistanceThreshold = 0.5f,
            NeedHand = true
        });
    }

    private void OnEnthrallDoAfter(EntityUid uid, VampireComponent component, EnthrallDoAfterEvent args)
    {
        if (args.Cancelled) return;

        var target = _entityManager.GetEntity(args.Target);
        if (!TryComp<ActorComponent>(target, out _))
            return;

        EnsureComp<UnholyComponent>(target);
        var newTrallComponent = EnsureComp<ThrallComponent>(target);
        newTrallComponent.VampireOwner = uid;

        if (!component.ThrallOwned.Contains(target))
        {
            component.ThrallOwned.Add(target);
            component.ThrallCount++;
        }

        _popup.PopupEntity(Loc.GetString("vampire-enthall-success", ("target", target)), uid, uid);
        _antag.SendBriefing(target, Loc.GetString("thrall-greeting"), Color.Red, new SoundPathSpecifier("/Audio/_Wega/Ambience/Antag/vampare_start.ogg"));
        SubtractBloodEssence(uid, 150);
    }

    private void OnCommune(EntityUid uid, VampireComponent component, VampireCommuneActionEvent args)
    {
        if (component.ThrallOwned.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("vampire-no-thrall"), uid, uid, PopupType.Medium);
            return;
        }

        if (!TryComp<ActorComponent>(uid, out var playerActor))
            return;

        // Админ логика, зато как просто
        var playerSession = playerActor.PlayerSession;
        _quickDialog.OpenDialog(playerSession, Loc.GetString("vampire-commune-title"), Loc.GetString("vampire-commune-prompt"),
            (string message) =>
            {
                var finalMessage = string.IsNullOrWhiteSpace(message)
                    ? Loc.GetString("vampire-commune-default-message")
                    : message;

                foreach (var thrallUid in component.ThrallOwned)
                {
                    if (!TryComp<ActorComponent>(thrallUid, out var thrallActor))
                        continue;

                    _prayerSystem.SendSubtleMessage(thrallActor.PlayerSession, thrallActor.PlayerSession, finalMessage, Loc.GetString("vampire-commune-default-message"));
                }

                _popup.PopupEntity(Loc.GetString("vampire-commune-sent"), uid, args.Performer, PopupType.Medium);
            });
        args.Handled = true;
    }

    private void OnPacify(EntityUid uid, VampireComponent component, VampirePacifyActionEvent args)
    {
        if (!CheckBloodEssence(uid, 30))
        {
            _popup.PopupEntity(Loc.GetString("vampire-blood-sacrifice-insufficient-blood"), uid, uid, PopupType.SmallCaution);
            return;
        }

        var target = args.Target;
        if (TryComp<HumanoidAppearanceComponent>(target, out var humanoid))
        {
            if (HasComp<NullRodOwnerComponent>(target) && !component.TruePowerActive)
            {
                _popup.PopupEntity(Loc.GetString("vampire-pacify-failed", ("target", target)), uid, uid);
                return;
            }

            if (!TryComp<PacifiedComponent>(target, out var pacified))
            {
                EnsureComp<PacifiedComponent>(target);
                Timer.Spawn(40000, () => { RemComp<PacifiedComponent>(target); });

                SubtractBloodEssence(uid, 30);
                args.Handled = true;
            }
            else
            {
                _popup.PopupEntity(Loc.GetString("vampire-pacify-failed", ("target", target)), uid, uid);
                args.Handled = true;
            }
        }
    }

    private void OnSubspaceSwap(EntityUid uid, VampireComponent component, VampireSubspaceSwapActionEvent args)
    {
        if (!CheckBloodEssence(uid, 15))
        {
            _popup.PopupEntity(Loc.GetString("vampire-blood-sacrifice-insufficient-blood"), uid, uid, PopupType.SmallCaution);
            return;
        }

        var target = args.Target;
        if (!TryComp(target, out HumanoidAppearanceComponent? humanoid))
        {
            _popup.PopupEntity(Loc.GetString("vampire-teleport-failed"), uid, uid, PopupType.Small);
            return;
        }

        if (HasComp<NullRodOwnerComponent>(target) && !component.TruePowerActive)
            return;

        var currentCoords = Transform(uid).Coordinates;
        var targetCoords = Transform(target).Coordinates;
        _transform.SetCoordinates(uid, targetCoords);
        _transform.SetCoordinates(target, currentCoords);
        _movementMod.TryUpdateMovementSpeedModDuration(target, MovementModStatusSystem.Slowdown, TimeSpan.FromSeconds(4f), 0.5f);
        _hallucinations.StartHallucinations(target, "Hallucinations", TimeSpan.FromSeconds(15f), true, "MindBreaker");

        SubtractBloodEssence(uid, 15);
        args.Handled = true;
    }

    /*private void OnDeployDecoy(EntityUid uid, VampireComponent component, VampireDeployDecoyActionEvent args)
    {
        if (!TryComp(uid, out VampireComponent? vampireComponent))
            return;

        if (!CheckBloodEssence(uid, 30))
        {
            _popup.PopupEntity(Loc.GetString("vampire-blood-sacrifice-insufficient-blood"), uid, uid, PopupType.SmallCaution);
            return;
        }

        SubtractBloodEssence(uid, 30);
        args.Handled = true;
    }*/

    private void OnRallyThralls(EntityUid uid, VampireComponent component, VampireRallyThrallsActionEvent args)
    {
        if (!CheckBloodEssence(uid, 40))
        {
            _popup.PopupEntity(Loc.GetString("vampire-blood-sacrifice-insufficient-blood"), uid, uid, PopupType.SmallCaution);
            return;
        }

        var thrallsInRange = _entityLookup.GetEntitiesInRange<ThrallComponent>(Transform(uid).Coordinates, 7f);
        foreach (var thrallEntity in thrallsInRange)
        {
            if (HasComp<StaminaComponent>(thrallEntity))
            {
                RemoveKnockdown(thrallEntity);
                _stamina.RemoveStaminaDamage(thrallEntity.Owner);
            }
        }

        SubtractBloodEssence(uid, 40);
        args.Handled = true;
    }

    private void OnBloodBond(EntityUid uid, VampireComponent component, VampireBloodBondActionEvent args)
    {
        if (component.ThrallOwned.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("vampire-no-thrall"), uid, uid, PopupType.Medium);
            return;
        }

        if (component.PowerActive)
        {
            component.PowerActive = false;
            component.IsDamageSharingActive = false;

            args.Handled = true;
            return;
        }

        component.PowerActive = true;
        component.IsDamageSharingActive = true;

        void ExecuteTick()
        {
            if (Deleted(uid) || !component.PowerActive)
            {
                component.PowerActive = false;
                component.IsDamageSharingActive = false;
                return;
            }

            if (component.CurrentBlood < 5)
            {
                _popup.PopupEntity(Loc.GetString("vampire-blood-sacrifice-insufficient-blood"), uid, uid, PopupType.SmallCaution);
                component.PowerActive = false;
                component.IsDamageSharingActive = false;
                return;
            }

            SubtractBloodEssence(uid, 5);

            Timer.Spawn((int)(1f * 1000), ExecuteTick);
        }

        ExecuteTick();
        SubtractBloodEssence(uid, 5);
        args.Handled = true;
    }

    private void OnMassHysteria(EntityUid uid, VampireComponent component, VampireMassHysteriaActionEvent args)
    {
        if (!CheckBloodEssence(uid, 40))
        {
            _popup.PopupEntity(Loc.GetString("vampire-blood-sacrifice-insufficient-blood"), uid, uid, PopupType.SmallCaution);
            return;
        }

        var victimInRange = _entityLookup
            .GetEntitiesInRange<BodyComponent>(Transform(uid).Coordinates, 8f)
            .Where(entity => entity.Owner != uid)
            .ToList();
        foreach (var victimEntity in victimInRange)
        {
            if (HasComp<NullRodOwnerComponent>(victimEntity) && !component.TruePowerActive)
                continue;

            _movementMod.TryUpdateMovementSpeedModDuration(victimEntity, MovementModStatusSystem.Slowdown, TimeSpan.FromSeconds(4f), 0.5f);
            _hallucinations.StartHallucinations(victimEntity, "Hallucinations", TimeSpan.FromSeconds(30f), true, "MindBreaker");
        }

        SubtractBloodEssence(uid, 40);
        args.Handled = true;
    }
    #endregion

    #region Other Methods
    public void RemoveKnockdown(EntityUid uid)
    {
        _statusEffect.TryRemoveStatusEffect(uid, "KnockedDown");
        _statusEffect.TryRemoveStatusEffect(uid, "Stun");
    }

    private void CoolSurroundingAtmosphere(EntityUid uid)
    {
        if (_atmosphereSystem.GetContainingMixture(uid, excite: true) is { } atmosphere)
        {
            const float targetTemperature = 233.15f;
            const float coolingRate = 10000f;

            var deltaT = targetTemperature - atmosphere.Temperature;
            if (deltaT < 0)
            {
                var heatCapacity = _atmosphereSystem.GetHeatCapacity(atmosphere, true);
                var energyToRemove = Math.Min(Math.Abs(deltaT) * heatCapacity, coolingRate);

                _atmosphereSystem.AddHeat(atmosphere, -energyToRemove);
            }
        }
    }

    private Vector2 DirectionToVector2(Direction direction)
    {
        return direction switch
        {
            Direction.North => new Vector2(0, 1),
            Direction.South => new Vector2(0, -1),
            Direction.East => new Vector2(1, 0),
            Direction.West => new Vector2(-1, 0),
            Direction.NorthEast => new Vector2(1, 1).Normalized(),
            Direction.NorthWest => new Vector2(-1, 1).Normalized(),
            Direction.SouthEast => new Vector2(1, -1).Normalized(),
            Direction.SouthWest => new Vector2(-1, -1).Normalized(),
            _ => Vector2.Zero,
        };
    }

    private bool TrySpawnObjectAtPosition(EntityCoordinates coords, EntProtoId entityId, EntityUid uid)
    {
        var grid = _transform.GetGrid(coords);
        if (grid is null) return false;

        var gridEntityUid = grid.Value;
        if (!TryComp<MapGridComponent>(gridEntityUid, out var gridComp))
            return false;

        var position = coords.Position;
        var gridPosition = new Vector2i((int)position.X, (int)position.Y);
        if (!_map.TryGetTileRef(gridEntityUid, gridComp, gridPosition, out var tileRef)
            || _turf.IsTileBlocked(tileRef, CollisionGroup.Impassable))
            return false;

        if (_net.IsServer)
        {
            var ent = Spawn(entityId, coords);

            if (uid.IsValid())
            {
                var comp = EnsureComp<PreventCollideComponent>(ent);
                comp.Uid = uid;
            }
        }

        return true;
    }
    #endregion
}
