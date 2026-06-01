using Robust.Shared.GameStates;

namespace Content.Shared.Ninja.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class NinjaCloningComponent : Component
{
    /// <summary>True while waiting for the clone to spawn; drives the client overlay.</summary>
    [DataField, AutoNetworkedField]
    public bool InCapsule;

    [DataField]
    public bool Used;

    [DataField]
    public int[] SavedChoices = Array.Empty<int>();

    [DataField]
    public int SavedSuitColor = 2;

    [DataField]
    public int SavedSuitGender;

    [DataField]
    public int SavedSuitStyleVariant;

    [DataField]
    public TimeSpan RespawnAt;

    /// <summary>The capsule on the ninja planet this ninja is bound to.</summary>
    [DataField]
    public EntityUid? CapsuleEntity;

    public static readonly TimeSpan CapsuleDuration = TimeSpan.FromSeconds(30);
}
