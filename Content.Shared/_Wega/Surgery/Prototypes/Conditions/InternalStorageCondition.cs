using Content.Shared.Implants.Components;

namespace Content.Shared.Surgery;

[Serializable]
[DataDefinition]
public sealed partial class InternalStorageCondition : SurgeryStepCondition
{
    [DataField("part")]
    public string Part { get; private set; } = "torso";

    /// <summary>
    /// true - checks if it is possible to add, false - checks if there are items
    /// </summary>
    [DataField("checkForSpace")]
    public bool CheckForSpace { get; private set; } = false;

    public override bool Check(EntityUid patient, IEntityManager entityManager)
    {
        if (!entityManager.TryGetComponent<InternalStorageComponent>(patient, out var storage))
            return false;

        switch (Part)
        {
            case "head":
                if (CheckForSpace)
                {
                    return storage.HeadContainer.ContainedEntity == null;
                }
                else
                {
                    return storage.HeadContainer.ContainedEntity != null;
                }

            case "torso":
                if (CheckForSpace)
                {
                    return storage.BodyContainer.ContainedEntities.Count < 3;
                }
                else
                {
                    return storage.BodyContainer.ContainedEntities.Count > 0;
                }

            case "tooth":
                if (CheckForSpace)
                {
                    return storage.ToothContainer.ContainedEntity == null;
                }
                else
                {
                    return storage.ToothContainer.ContainedEntity != null;
                }
        }

        return false;
    }
}
