using Content.Shared.Veil.Cult.Components;

namespace Content.Shared.Surgery;

[Serializable]
[DataDefinition]
public sealed partial class VeilCultistCondition : SurgeryStepCondition
{
    public override bool Check(EntityUid patient, IEntityManager entityManager)
    {
        if (entityManager.HasComponent<VeilCultistComponent>(patient))
            return true;

        return false;
    }
}
