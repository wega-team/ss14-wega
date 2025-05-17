using Content.Shared.Body.Systems;

namespace Content.Shared.Surgery;

[Serializable]
[DataDefinition]
public sealed partial class BodyPartCondition : SurgeryStepCondition
{
    [DataField("bodyPart")]
    public string BodyPart { get; private set; } = string.Empty;

    public override bool Check(EntityUid patient, IEntityManager entityManager)
    {
        var bodySystem = entityManager.System<SharedBodySystem>();
        return bodySystem.HasBodyPart(patient, BodyPart);
    }
}