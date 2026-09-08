using Content.Shared.Whitelist;

namespace Content.Shared.Surgery;

[Serializable]
[DataDefinition]
public sealed partial class WhitelistCondition : SurgeryStepCondition
{
    [DataField("whitelist", required: true)]
    public EntityWhitelist? Whitelist { get; private set; } = default!;
    public override bool Check(EntityUid patient, IEntityManager entityManager)
    {
        var whitelistSystem = entityManager.System<EntityWhitelistSystem>();

        if (whitelistSystem.IsWhitelistPassOrNull(Whitelist, patient))
            return true;

        return false;
    }
}

