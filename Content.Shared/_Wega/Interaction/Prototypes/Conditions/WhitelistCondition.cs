using Content.Shared.Whitelist;

namespace Content.Shared.Interaction;

/// <summary>
/// Condition that checks whether a chosen entity passes the given <see cref="EntityWhitelist"/>.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class WhitelistCondition : InteractionCondition
{
    /// <summary>
    /// The whitelist to check against. <see cref="EntityWhitelist"/>.
    /// </summary>
    [DataField(required: true)]
    public EntityWhitelist Whitelist { get; private set; }

    /// <summary>
    /// Which entity to run the check on: user or target. <see cref="ConditionTarget"/>
    /// </summary>
    [DataField]
    public ConditionTarget Target { get; private set; } = ConditionTarget.Target;

    public override bool Check(EntityUid user, EntityUid target, IEntityManager entityManager)
    {
        var checkEntity = Target == ConditionTarget.User ? user : target;
        var whitelistSystem = entityManager.System<EntityWhitelistSystem>();

        return whitelistSystem.IsWhitelistPass(Whitelist, checkEntity);
    }
}
