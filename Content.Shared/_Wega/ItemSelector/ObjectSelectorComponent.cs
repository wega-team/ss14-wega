using Robust.Shared.Prototypes;

namespace Content.Shared.Item.Selector.Components;

[RegisterComponent]
public sealed partial class ObjectSelectorComponent : Component
{
    /// <summary>
    /// List of objects prototype IDs that can be selected.
    /// </summary>
    [DataField("objects")]
    public List<EntProtoId> Objects = new();

    /// <summary>
    /// Components that an entity must have to display the UI (whitelist)
    /// If it is empty, the check is not performed.
    /// </summary>
    [DataField("requiredComponents")]
    public List<string> WhitelistComponents = new();

    /// <summary>
    /// Components that an entity should not have to display the UI (blacklist)
    /// If it is empty, the check is not performed.
    /// </summary>
    [DataField("forbiddenComponents")]
    public List<string> BlacklistComponents = new();

    /// <summary>
    /// A switch that allows you to interact with an object by simply touching it.
    /// It can be disabled if necessary.
    /// </summary>
    [DataField("disabledInteract")]
    public bool DisabledInteract = false;
}
