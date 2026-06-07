using Content.Server.Objectives.Components;
using Content.Shared.Objectives.Components;
using Content.Shared.Stacks;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Handles the "hold N credits on you" objective. Counts credit stacks anywhere in the player's
/// inventory tree and reports progress live, so returning the money un-completes the objective.
/// </summary>
public sealed partial class HoldCreditsConditionSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private MetaDataSystem _metaData = default!;

    private static readonly ProtoId<StackPrototype> CreditStack = "Credit";

    private EntityQuery<ContainerManagerComponent> _containerQuery;
    private EntityQuery<StackComponent> _stackQuery;

    public override void Initialize()
    {
        base.Initialize();

        _containerQuery = GetEntityQuery<ContainerManagerComponent>();
        _stackQuery = GetEntityQuery<StackComponent>();

        SubscribeLocalEvent<HoldCreditsConditionComponent, ObjectiveAssignedEvent>(OnAssigned);
        SubscribeLocalEvent<HoldCreditsConditionComponent, ObjectiveAfterAssignEvent>(OnAfterAssign);
        SubscribeLocalEvent<HoldCreditsConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnAssigned(Entity<HoldCreditsConditionComponent> ent, ref ObjectiveAssignedEvent args)
    {
        ent.Comp.Target = _random.Next(ent.Comp.Min, ent.Comp.Max + 1);
    }

    private void OnAfterAssign(Entity<HoldCreditsConditionComponent> ent, ref ObjectiveAfterAssignEvent args)
    {
        _metaData.SetEntityName(ent.Owner, Loc.GetString("objective-condition-hold-credits-title"), args.Meta);
        _metaData.SetEntityDescription(ent.Owner,
            Loc.GetString("objective-condition-hold-credits-desc", ("count", ent.Comp.Target)), args.Meta);
    }

    private void OnGetProgress(Entity<HoldCreditsConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        if (ent.Comp.Target <= 0)
        {
            args.Progress = 1f;
            return;
        }

        var held = CountCredits(args.Mind.OwnedEntity);
        args.Progress = Math.Clamp(held / (float) ent.Comp.Target, 0f, 1f);
    }

    /// <summary>Recursively sums every credit stack carried in the entity's containers.</summary>
    private int CountCredits(EntityUid? owner)
    {
        if (owner == null || !_containerQuery.TryGetComponent(owner, out var manager))
            return 0;

        var total = 0;
        var containerStack = new Stack<ContainerManagerComponent>();
        var current = manager;

        do
        {
            foreach (var container in current.Containers.Values)
            {
                foreach (var entity in container.ContainedEntities)
                {
                    if (_stackQuery.TryGetComponent(entity, out var stack) && stack.StackTypeId == CreditStack)
                        total += stack.Count;

                    if (_containerQuery.TryGetComponent(entity, out var childManager))
                        containerStack.Push(childManager);
                }
            }
        } while (containerStack.TryPop(out current));

        return total;
    }
}
