using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;

namespace Content.Shared.Interaction;

/// <summary>
/// The condition checks the status of the mob.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class MobStateCondition : InteractionCondition
{
    /// <summary>
    /// The expected state of the mob. <see cref="MobState"/>
    /// </summary>
    [DataField("state", required: true)]
    public MobState TargetState { get; private set; } = MobState.Alive;

    /// <summary>
    /// Which entity should be checked: user or target. <see cref="ConditionTarget"/>
    /// </summary>
    [DataField]
    public ConditionTarget Target { get; private set; } = ConditionTarget.Target;

    public override bool Check(EntityUid user, EntityUid target, IEntityManager entityManager)
    {
        var checkEntity = Target == ConditionTarget.User ? user : target;
        if (!entityManager.TryGetComponent<MobStateComponent>(checkEntity, out var mobState))
            return false;

        return mobState.CurrentState == TargetState;
    }
}
