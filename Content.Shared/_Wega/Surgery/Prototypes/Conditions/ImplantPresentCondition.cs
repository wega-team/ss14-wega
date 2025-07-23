using Content.Shared.Implants.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Surgery;

[Serializable]
[DataDefinition]
public sealed partial class ImplantPresentCondition : SurgeryStepCondition
{
    [DataField("implant", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ImplantId { get; private set; } = default!;

    public override bool Check(EntityUid patient, IEntityManager entityManager)
    {
        if (!entityManager.TryGetComponent<ImplantedComponent>(patient, out var implanted))
            return false;

        foreach (var implant in implanted.ImplantContainer.ContainedEntities)
        {
            var meta = entityManager.GetComponent<MetaDataComponent>(implant);
            if (meta.EntityPrototype?.ID == ImplantId)
                return true;
        }

        return false;
    }
}
