using Content.Shared.Implants.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Surgery;

[Serializable]
[DataDefinition]
public sealed partial class ImplantPresentCondition : SurgeryStepCondition
{
    [DataField("implant", required: true)]
    public EntProtoId ImplantId { get; private set; } = default!;

    public override bool Check(EntityUid patient, IEntityManager entityManager)
    {
        if (!entityManager.TryGetComponent<ImplantedComponent>(patient, out var implanted))
            return false;

        foreach (var implant in implanted.ImplantContainer.ContainedEntities)
        {
            var meta = entityManager.GetComponent<MetaDataComponent>(implant);
            var entProto = meta.EntityPrototype?.ID;
            if (!string.IsNullOrEmpty(entProto) && entProto == ImplantId)
                return true;
        }

        return false;
    }
}
