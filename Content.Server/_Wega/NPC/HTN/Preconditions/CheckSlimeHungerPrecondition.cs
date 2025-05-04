using Content.Shared.Xenobiology.Components;

namespace Content.Server.NPC.HTN.Preconditions;

public sealed partial class CheckSlimeHungerPrecondition : HTNPrecondition
{
    [Dependency] private readonly IEntityManager _entMan = default!;

    [DataField("targetState")]
    public SlimeBehaviorState TargetState;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        if (!_entMan.TryGetComponent<SlimeHungerComponent>(owner, out var hunger))
            return false;

        if (hunger.CurrentState == SlimeBehaviorState.Dividing)
            return false;

        return hunger.CurrentState == TargetState;
    }
}
