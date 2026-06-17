using Robust.Shared.GameStates;
using Content.Shared.Whitelist; // Corvax-Wega-Morgue-change

namespace Content.Shared.Foldable;

/// <summary>
/// Used to create "foldable structures" that you can pickup like an item when folded.
/// </summary>
/// <remarks>
/// Will prevent any insertions into containers while this item is unfolded.
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(FoldableSystem))]
public sealed partial class FoldableComponent : Component
{
    [DataField("folded"), AutoNetworkedField]
    public bool IsFolded = false;

    [DataField]
    public bool CanFoldInsideContainer = false;

    [DataField] // Corvax-Wega-Morgue-change
    public EntityWhitelist? DeployedContainerWhitelist; // Corvax-Wega-Morgue-change

    [DataField]
    public LocId UnfoldVerbText = "unfold-verb";

    [DataField]
    public LocId FoldVerbText = "fold-verb";
}
