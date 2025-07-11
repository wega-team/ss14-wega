using System.Threading;
using System.Threading.Tasks;
using Content.Server.NPC.Components;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;

namespace Content.Server.NPC.HTN.PrimitiveTasks.Operators;

public sealed partial class SlimeMeleeOperator : HTNOperator, IHtnConditionalShutdown
{
    [Dependency] private readonly IEntityManager _entMan = default!;

    [DataField("targetKey")]
    public string TargetKey = "AttackTarget";

    [DataField("damageType")]
    public string DamageType = "Poison";

    [DataField("damageAmount")]
    public float DamageAmount = 5f;

    [DataField("cooldown")]
    public float Cooldown = 2f;

    [DataField("shutdownState")]
    public HTNPlanState ShutdownState { get; private set; } = HTNPlanState.TaskFinished;

    [DataField("targetState")]
    public MobState TargetState = MobState.Dead;

    public override void Startup(NPCBlackboard blackboard)
    {
        base.Startup(blackboard);
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        var melee = _entMan.EnsureComponent<NPCMeleeCombatComponent>(owner);

        melee.MissChance = 0.2f;
        melee.Target = blackboard.GetValue<EntityUid>(TargetKey);

        var damage = new DamageSpecifier();
        damage.DamageDict.Add(DamageType, DamageAmount);
    }

    public override async Task<(bool Valid, Dictionary<string, object>? Effects)> Plan(NPCBlackboard blackboard,
        CancellationToken cancelToken)
    {
        if (!blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entMan))
        {
            return (false, null);
        }

        return (true, null);
    }

    public void ConditionalShutdown(NPCBlackboard blackboard)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);
        _entMan.System<SharedCombatModeSystem>().SetInCombatMode(owner, false);
        _entMan.RemoveComponent<NPCMeleeCombatComponent>(owner);
    }

    public override void TaskShutdown(NPCBlackboard blackboard, HTNOperatorStatus status)
    {
        base.TaskShutdown(blackboard, status);
        ConditionalShutdown(blackboard);
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        var owner = blackboard.GetValue<EntityUid>(NPCBlackboard.Owner);

        if (!_entMan.TryGetComponent<NPCMeleeCombatComponent>(owner, out var combat) ||
            !blackboard.TryGetValue<EntityUid>(TargetKey, out var target, _entMan))
        {
            return HTNOperatorStatus.Failed;
        }

        combat.Target = target;
        if (_entMan.TryGetComponent<MobStateComponent>(target, out var mobState) &&
            mobState.CurrentState == MobState.Dead)
        {
            return HTNOperatorStatus.Finished;
        }

        switch (combat.Status)
        {
            case CombatStatus.TargetOutOfRange:
                return HTNOperatorStatus.Continuing;
            case CombatStatus.Normal:
                return HTNOperatorStatus.Continuing;
            default:
                return HTNOperatorStatus.Failed;
        }
    }
}
