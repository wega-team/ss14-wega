using Content.Shared.Surgery.Components;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Surgery;

[Serializable]
[DataDefinition]
public sealed partial class InternalDamageCondition : SurgeryStepCondition
{
    [DataField("damageType", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<InternalDamagePrototype>))]
    public string DamageType { get; private set; } = default!;

    [DataField("bodyPart", required: true)]
    public string BodyPart { get; private set; }

    public override bool Check(EntityUid patient, IEntityManager entityManager)
    {
        if (!entityManager.TryGetComponent<OperatedComponent>(patient, out var operated))
            return false;

        if (!operated.InternalDamages.TryGetValue(DamageType, out var bodyParts))
            return false;

        return bodyParts.Contains(BodyPart);
    }
}