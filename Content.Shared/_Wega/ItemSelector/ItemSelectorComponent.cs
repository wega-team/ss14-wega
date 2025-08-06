using Robust.Shared.Prototypes;

namespace Content.Shared.Item.Selector.Components;

[RegisterComponent]
public sealed partial class ItemSelectorComponent : Component
{
    /// <summary>
    /// List of item prototype IDs that can be selected.
    /// </summary>
    [DataField("items"), ValidatePrototypeId<EntityPrototype>]
    public List<string> Items = new();

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
}
