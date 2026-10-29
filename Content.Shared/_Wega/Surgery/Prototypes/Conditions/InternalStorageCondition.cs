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

        var hasItem = Part switch
        {
            "head" => storage.HeadContainer.ContainedEntity != null,
            "torso" => storage.BodyContainer.ContainedEntities.Count > 0,
            "tooth" => storage.ToothContainer.ContainedEntity != null,
            _ => false
        };

        var hasSpace = Part switch
        {
            "head" => storage.HeadContainer.ContainedEntity == null,
            "torso" => storage.BodyContainer.ContainedEntities.Count < 3,
            "tooth" => storage.ToothContainer.ContainedEntity == null,
            _ => false
        };

        return CheckForSpace ? hasSpace : hasItem;
    }
}
