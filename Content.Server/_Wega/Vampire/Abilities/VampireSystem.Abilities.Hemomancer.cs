using System.Linq;
using System.Numerics;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Clothing;
using Content.Shared.Damage.Components;
using Content.Shared.Fluids.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Components;
using Content.Shared.NullRod.Components;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Roles;
using Content.Shared.Standing;
using Content.Shared.Vampire;
using Content.Shared.Vampire.Components;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Vampire;

public sealed partial class VampireSystem
{
    [Dependency] private readonly LoadoutSystem _loadout = default!;

    private static readonly ProtoId<ReagentPrototype>[] BloodProto = new ProtoId<ReagentPrototype>[]
    {
        "Blood", "CopperBlood", "InsectBlood", "SulfurBlood", "ResomiBlood", "AriralBlood"
    };

    private void InitializeHemomancer()
    {
        SubscribeLocalEvent<VampireComponent, VampireClawsActionEvent>(GiveVampireClaws);
        SubscribeLocalEvent<VampireComponent, VampireBloodTentacleAction>(OnBloodTendrils);
        SubscribeLocalEvent<VampireComponent, VampireBloodBarrierActionEvent>(OnBloodBarrierAction);
        SubscribeLocalEvent<VampireComponent, VampireSanguinePoolActionEvent>(OnSanguinePoolAction);
        SubscribeLocalEvent<VampireComponent, VampirePredatorSensesActionEvent>(OnVampirePredatorSensesAction);
        SubscribeLocalEvent<VampireComponent, VampireBloodEruptionActionEvent>(OnVampireBloodEruptionAction);
        SubscribeLocalEvent<VampireComponent, VampireBloodBringersRiteActionEvent>(OnBloodBringersRite);
    }

    private void GiveVampireClaws(Entity<VampireComponent> ent, ref VampireClawsActionEvent args)
    {
        if (!CheckBloodEssence(ent.Owner, args.BloodCost))
        {
            SendFailedPopup(ent);
            return;
        }

        var dropEvent = new DropHandItemsEvent();
        RaiseLocalEvent(ent, ref dropEvent);

        List<ProtoId<StartingGearPrototype>> gear = new() { args.ProtoId };
        _loadout.Equip(ent, gear, null);

        SubtractBloodEssence(ent.Owner, args.BloodCost);
        args.Handled = true;
    }

    private void OnBloodTendrils(Entity<VampireComponent> ent, ref VampireBloodTentacleAction args)
    {
        if (!CheckBloodEssence(ent.Owner, args.BloodCost))
        {
            SendFailedPopup(ent);
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
            if (!_map.TryGetTileRef(grid, gridComp, pos, out var tileRef)
                || _turf.IsTileBlocked(tileRef, CollisionGroup.Impassable))
                continue;

            Spawn(args.EntityId, pos);
        }

        SubtractBloodEssence(ent.Owner, args.BloodCost);
        args.Handled = true;
    }

    private void OnBloodBarrierAction(Entity<VampireComponent> ent, ref VampireBloodBarrierActionEvent args)
    {
        if (!CheckBloodEssence(ent.Owner, args.BloodCost))
        {
            SendFailedPopup(ent);
            return;
        }

        var targetCoords = args.Target;
        if (args.UseCasterDirection)
        {
            var transform = Transform(ent);
            var direction = transform.LocalRotation.ToWorldVec().Normalized();

            var perpendicularDirection = new Vector2(-direction.Y, direction.X);

            var objectCount = 0;
            for (int i = -1; i <= 1 && objectCount < 3; i++)
            {
                var spawnPosition = targetCoords.Offset(perpendicularDirection * (1f * i));

                if (TrySpawnObjectAtPosition(spawnPosition, args.EntityId, ent))
                    objectCount++;
            }

            SubtractBloodEssence(ent.Owner, args.BloodCost);
            args.Handled = true;
        }
    }

    private void OnSanguinePoolAction(Entity<VampireComponent> ent, ref VampireSanguinePoolActionEvent args)
    {
        if (!CheckBloodEssence(ent.Owner, args.BloodCost))
        {
            SendFailedPopup(ent);
            return;
        }

        var polymorphedEntity = _polymorph.PolymorphEntity(ent, args.PolymorphProto);
        if (polymorphedEntity == null)
            return;

        SubtractBloodEssence(ent.Owner, args.BloodCost);
        args.Handled = true;
    }

    private void OnVampirePredatorSensesAction(Entity<VampireComponent> ent, ref VampirePredatorSensesActionEvent args)
    {
        var nearbyHumanoids = _entityLookup.GetEntitiesInRange<HumanoidProfileComponent>(Transform(ent).Coordinates, 6f);

        foreach (var humanoidEntity in nearbyHumanoids)
        {
            var humanoid = humanoidEntity.Owner;
            if (humanoid == ent.Owner)
                continue;

            if (_mobState.IsIncapacitated(humanoid))
                continue;

            Spawn(args.EntityId, Transform(humanoid).Coordinates);

            _audio.PlayPvs(args.Sound, humanoid);
            _popup.PopupEntity(Loc.GetString("vampire-predator-senses-puddle"), humanoid, ent, PopupType.SmallCaution);
            _stun.TryUpdateParalyzeDuration(humanoid, TimeSpan.FromSeconds(4));
            break;
        }

        args.Handled = true;
    }

    private void OnVampireBloodEruptionAction(Entity<VampireComponent> ent, ref VampireBloodEruptionActionEvent args)
    {
        if (!CheckBloodEssence(ent.Owner, args.BloodCost))
        {
            SendFailedPopup(ent);
            return;
        }

        var puddlesInRange = _entityLookup.GetEntitiesInRange<PuddleComponent>(Transform(ent).Coordinates, 4f)
            .Where(puddle => TryComp(puddle.Owner, out ContainerManagerComponent? containerManager)
                && containerManager.Containers.TryGetValue("solution@puddle", out var container)
                && container.ContainedEntities.Any(containedEntity =>
                    TryComp(containedEntity, out SolutionComponent? solutionComponent)
                    && solutionComponent.Solution.Contents.Any(r =>
                        BloodProto.Contains(r.Reagent.Prototype))))
            .ToList();

        foreach (var puddleEntity in puddlesInRange)
        {
            var entitiesOnPuddle = _entityLookup.GetEntitiesInRange<DamageableComponent>(Transform(puddleEntity.Owner).Coordinates, 0.1f)
                .Where(entity => entity.Owner != ent.Owner).ToList();

            foreach (var targetEntity in entitiesOnPuddle)
            {
                if (HasComp<NullRodOwnerComponent>(targetEntity.Owner) && !HasTruePower(ent))
                    continue;

                _damage.TryChangeDamage(targetEntity.Owner, args.Damage, origin: ent);
                _stun.TryUpdateParalyzeDuration(targetEntity.Owner, TimeSpan.FromSeconds(3));
                _popup.PopupEntity(Loc.GetString("vampire-blood-eruption-effect-message"), targetEntity.Owner, ent, PopupType.SmallCaution);
            }
        }

        SubtractBloodEssence(ent.Owner, args.BloodCost);
        args.Handled = true;
    }

    private void OnBloodBringersRite(Entity<VampireComponent> ent, ref VampireBloodBringersRiteActionEvent args)
    {
        var supreme = GetTruePower(ent);
        if (supreme == null)
            return;

        if (!CheckBloodEssence(ent.Owner, args.BloodCost))
        {
            SendFailedPopup(ent);
            return;
        }

        if (supreme.Active)
        {
            supreme.Active = false;
            _alerts.ShowAlert(ent.Owner, args.Alert, 0);
            Dirty(ent.Owner, supreme);
            args.Handled = true;
            return;
        }

        supreme.Active = true;
        _alerts.ShowAlert(ent.Owner, args.Alert, 1);
        Dirty(ent.Owner, supreme);

        _popup.PopupEntity(Loc.GetString("vampire-blood-true-power-started"), ent, ent, PopupType.SmallCaution);

        ExecuteBloodBringersRiteTick(ent, supreme, args, false);
        SubtractBloodEssence(ent.Owner, args.BloodCost);
    }

    #region Utility Methods

    private void ExecuteBloodBringersRiteTick(Entity<VampireComponent> ent, SupremeVampireComponent supreme, VampireBloodBringersRiteActionEvent args, bool bloodSpawned)
    {
        if (!Exists(ent) || !supreme.Active)
        {
            supreme.Active = false;
            _alerts.ShowAlert(ent.Owner, args.Alert, 0);
            Dirty(ent.Owner, supreme);
            return;
        }

        if (!CheckBloodEssence(ent.Owner, args.BloodCost))
        {
            SendFailedPopup(ent);

            supreme.Active = false;
            _alerts.ShowAlert(ent.Owner, args.Alert, 0);
            Dirty(ent.Owner, supreme);
            return;
        }

        SubtractBloodEssence(ent.Owner, args.BloodCost);

        var nearbyEntities = _entityLookup.GetEntitiesInRange<MobStateComponent>(Transform(ent).Coordinates, 7f)
            .Where(entity => !_mobState.IsDead(entity.Owner)).ToList();

        if (nearbyEntities.Count > 0)
        {
            var scaledHealingSpec = args.Heal * nearbyEntities.Count;
            _damage.TryChangeDamage(ent.Owner, scaledHealingSpec, true, false, origin: ent);
            _stamina.TakeStaminaDamage(ent, args.StaminaMod * nearbyEntities.Count, visual: false);

            if (!bloodSpawned)
            {
                foreach (var entity in nearbyEntities)
                {
                    if (HasComp<NullRodOwnerComponent>(entity.Owner) && !HasTruePower(ent))
                        continue;

                    _damage.TryChangeDamage(entity.Owner, args.Damage, origin: ent);
                    Spawn(args.EntityId, Transform(entity.Owner).Coordinates);
                }

                _audio.PlayPvs(args.Sound, ent);
                bloodSpawned = true;
            }
        }

        Timer.Spawn(args.TimeInterval, () => ExecuteBloodBringersRiteTick(ent, supreme, args, bloodSpawned));
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

    #endregion
}
