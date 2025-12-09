using Content.Server.Objectives.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Objectives.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Stacks;
using Content.Shared.Interaction;
using Content.Shared.CartridgeLoader;

namespace Content.Server.Objectives.Systems;

public sealed class BloodBrotherSharedStealConditionSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedObjectivesSystem _objectives = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly BloodBrotherSharedConditionSystem _sharedCondition = default!;

    private EntityQuery<ContainerManagerComponent> _containerQuery;
    private HashSet<Entity<TransformComponent>> _nearestEnts = new();
    private HashSet<EntityUid> _countedItems = new();

    public override void Initialize()
    {
        base.Initialize();

        _containerQuery = GetEntityQuery<ContainerManagerComponent>();

        SubscribeLocalEvent<BloodBrotherSharedStealConditionComponent, ObjectiveAssignedEvent>(OnAssigned);
        SubscribeLocalEvent<BloodBrotherSharedStealConditionComponent, ObjectiveAfterAssignEvent>(OnAfterAssign);
        SubscribeLocalEvent<BloodBrotherSharedStealConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnAssigned(Entity<BloodBrotherSharedStealConditionComponent> condition, ref ObjectiveAssignedEvent args)
    {
        List<StealTargetComponent?> targetList = new();

        var query = AllEntityQuery<StealTargetComponent>();
        while (query.MoveNext(out var target))
        {
            if (condition.Comp.StealGroup != target.StealGroup)
                continue;

            targetList.Add(target);
        }

        if (targetList.Count == 0 && condition.Comp.VerifyMapExistence)
        {
            args.Cancelled = true;
            return;
        }

        var maxSize = condition.Comp.VerifyMapExistence
            ? Math.Min(targetList.Count, condition.Comp.MaxCollectionSize)
            : condition.Comp.MaxCollectionSize;
        var minSize = condition.Comp.VerifyMapExistence
            ? Math.Min(targetList.Count, condition.Comp.MinCollectionSize)
            : condition.Comp.MinCollectionSize;

        condition.Comp.CollectionSize = _random.Next(minSize, maxSize);
    }

    private void OnAfterAssign(Entity<BloodBrotherSharedStealConditionComponent> condition, ref ObjectiveAfterAssignEvent args)
    {
        var group = _proto.Index(condition.Comp.StealGroup);
        string localizedName = Loc.GetString(group.Name);

        var title = condition.Comp.OwnerText == null
            ? Loc.GetString(condition.Comp.ObjectiveNoOwnerText, ("itemName", localizedName))
            : Loc.GetString(condition.Comp.ObjectiveText, ("owner", Loc.GetString(condition.Comp.OwnerText)), ("itemName", localizedName));

        var description = condition.Comp.CollectionSize > 1
            ? Loc.GetString(condition.Comp.DescriptionMultiplyText, ("itemName", localizedName), ("count", condition.Comp.CollectionSize))
            : Loc.GetString(condition.Comp.DescriptionText, ("itemName", localizedName));

        _metaData.SetEntityName(condition.Owner, title, args.Meta);
        _metaData.SetEntityDescription(condition.Owner, description, args.Meta);
        _objectives.SetIcon(condition.Owner, group.Sprite, args.Objective);
    }

    private void OnGetProgress(Entity<BloodBrotherSharedStealConditionComponent> condition, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetProgress(condition.Owner, (args.MindId, args.Mind), condition);
    }

    private float GetProgress(EntityUid objectiveUid, Entity<MindComponent> mind, BloodBrotherSharedStealConditionComponent condition)
    {
        if (_sharedCondition.TryGetSharedCondition(objectiveUid, mind.Owner, out var sharedCondition)
            && !_sharedCondition.CheckBaseConditions(mind.Owner, sharedCondition, mind.Comp))
            return 0f;

        var currentCount = CountStolenItems(mind, condition);
        var brotherCount = 0;

        if (sharedCondition?.BrotherMind != null && TryComp<MindComponent>(sharedCondition.BrotherMind.Value, out var brotherMind))
            brotherCount = CountStolenItems((sharedCondition.BrotherMind.Value, brotherMind), condition);

        var totalCount = Math.Max(currentCount, brotherCount);
        var result = totalCount / (float)condition.CollectionSize;
        return Math.Clamp(result, 0, 1);
    }

    private int CountStolenItems(Entity<MindComponent> mind, BloodBrotherSharedStealConditionComponent condition)
    {
        if (mind.Comp.OwnedEntity == null || !_containerQuery.TryGetComponent(mind.Comp.OwnedEntity.Value, out var currentManager))
            return 0;

        var containerStack = new Stack<ContainerManagerComponent>();
        var count = 0;

        _countedItems.Clear();

        if (condition.CheckStealAreas)
        {
            var areasQuery = AllEntityQuery<StealAreaComponent, TransformComponent>();
            while (areasQuery.MoveNext(out var uid, out var area, out var xform))
            {
                if (!IsOwnerOfStealArea(uid, mind.Owner, area))
                    continue;

                _nearestEnts.Clear();
                _lookup.GetEntitiesInRange(xform.Coordinates, area.Range, _nearestEnts);
                foreach (var ent in _nearestEnts)
                {
                    if (!_interaction.InRangeUnobstructed((uid, xform), (ent, ent.Comp), range: area.Range))
                        continue;

                    CheckEntity(ent, condition, ref containerStack, ref count);
                }
            }
        }

        if (TryComp<PullerComponent>(mind.Comp.OwnedEntity, out var pull))
        {
            var pulledEntity = pull.Pulling;
            if (pulledEntity != null)
            {
                CheckEntity(pulledEntity.Value, condition, ref containerStack, ref count);
            }
        }

        do
        {
            foreach (var container in currentManager.Containers.Values)
            {
                foreach (var entity in container.ContainedEntities)
                {
                    count += CheckStealTarget(entity, condition);
                    if (_containerQuery.TryGetComponent(entity, out var containerManager))
                        containerStack.Push(containerManager);
                }
            }
        } while (containerStack.TryPop(out currentManager));

        return count;
    }

    private bool IsOwnerOfStealArea(EntityUid areaUid, EntityUid mindId, StealAreaComponent area)
    {
        var owners = new HashSet<EntityUid>();
        foreach (var owner in area.Owners)
        {
            owners.Add(owner);
        }
        return owners.Contains(mindId);
    }

    private void CheckEntity(EntityUid entity, BloodBrotherSharedStealConditionComponent condition, ref Stack<ContainerManagerComponent> containerStack, ref int counter)
    {
        counter += CheckStealTarget(entity, condition);
        if (!TryComp<MindContainerComponent>(entity, out var pullMind))
        {
            if (_containerQuery.TryGetComponent(entity, out var containerManager))
                containerStack.Push(containerManager);
        }
    }

    private int CheckStealTarget(EntityUid entity, BloodBrotherSharedStealConditionComponent condition)
    {
        if (_countedItems.Contains(entity))
            return 0;

        if (!TryComp<StealTargetComponent>(entity, out var target))
            return 0;

        if (target.StealGroup != condition.StealGroup)
            return 0;

        if (TryComp<CartridgeComponent>(entity, out var cartridge) &&
            cartridge.InstallationStatus is not InstallationStatus.Cartridge)
            return 0;

        if (condition.CheckAlive)
        {
            if (TryComp<MobStateComponent>(entity, out var state))
            {
                if (!_mobState.IsAlive(entity, state))
                    return 0;
            }
        }

        _countedItems.Add(entity);
        return TryComp<StackComponent>(entity, out var stack) ? stack.Count : 1;
    }

    public void CopySharedStealConditionData(EntityUid sourceObjective, EntityUid targetObjective)
    {
        if (TryComp<BloodBrotherSharedStealConditionComponent>(sourceObjective, out var sourceCondition)
            && TryComp<BloodBrotherSharedStealConditionComponent>(targetObjective, out var targetCondition))
        {
            targetCondition.StealGroup = sourceCondition.StealGroup;
            targetCondition.VerifyMapExistence = sourceCondition.VerifyMapExistence;
            targetCondition.CheckStealAreas = sourceCondition.CheckStealAreas;
            targetCondition.CheckAlive = sourceCondition.CheckAlive;
            targetCondition.MinCollectionSize = sourceCondition.MinCollectionSize;
            targetCondition.MaxCollectionSize = sourceCondition.MaxCollectionSize;
            targetCondition.CollectionSize = sourceCondition.CollectionSize;
            targetCondition.OwnerText = sourceCondition.OwnerText;
            targetCondition.ObjectiveText = sourceCondition.ObjectiveText;
            targetCondition.ObjectiveNoOwnerText = sourceCondition.ObjectiveNoOwnerText;
            targetCondition.DescriptionText = sourceCondition.DescriptionText;
            targetCondition.DescriptionMultiplyText = sourceCondition.DescriptionMultiplyText;
        }
    }
}
