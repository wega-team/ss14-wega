using System.Linq;
using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared.Surgery;

[Serializable]
[DataDefinition]
public sealed partial class OrganPresentCondition : SurgeryStepCondition
{
    [DataField("organ")]
    public ProtoId<OrganCategoryPrototype>? OrganId { get; private set; }

    public override bool Check(EntityUid patient, IEntityManager entityManager)
    {
        if (OrganId == null)
            return false;

        if (!entityManager.TryGetComponent<BodyComponent>(patient, out var bodyComp) || bodyComp.Organs == null)
            return false;

        return bodyComp.Organs.ContainedEntities.Select(entityManager.GetComponent<OrganComponent>)
            .Any(o => o.Category == OrganId);
    }
}
