using System.Linq;
using Content.Shared.Body.Systems;

namespace Content.Shared.Surgery;

[Serializable]
[DataDefinition]
public sealed partial class OrganPresentCondition : SurgeryStepCondition
{
    [DataField("organ")]
    public string OrganId { get; private set; } = string.Empty;

    public override bool Check(EntityUid patient, IEntityManager entityManager)
    {
        var bodySystem = entityManager.System<SharedBodySystem>();
        return bodySystem.GetBodyOrgans(patient)
            .Any(o => o.Component.OrganType == OrganId);
    }
}