using Content.Shared.Actions;
using Content.Shared.Dataset;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Ninja.Components;

/// <summary>
/// Added to a borg chassis when the chameleon module is installed.
/// Provides a toggle action to switch between ninja skin and original chassis skin.
/// While disguised, the borg's name and description change to avoid metagaming.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class NinjaBorgChameleonComponent : Component
{
    [DataField]
    public EntProtoId ToggleAction = "ActionNinjaBorgToggleChameleon";

    [DataField]
    public EntityUid? ToggleActionEntity;

    [DataField, AutoNetworkedField]
    public bool IsDisguised;

    /// <summary>Dataset to pick a random disguise name from (same dataset as real borgs).</summary>
    [DataField]
    public ProtoId<LocalizedDatasetPrototype> DisguisedNameDataset = "NamesBorg";

    /// <summary>Locale key for the description shown while disguised.</summary>
    [DataField]
    public string DisguisedDescription = "ninja-borg-chameleon-disguised-description";

    /// Runtime: which name was picked this disguise session.
    public string? PickedDisguiseName;

    /// Runtime-only: original name/description/identifier saved before disguising.
    public string? OriginalName;
    public string? OriginalDescription;
    public string? OriginalIdentifier;
}

public sealed partial class NinjaBorgToggleChameleonEvent : InstantActionEvent;
