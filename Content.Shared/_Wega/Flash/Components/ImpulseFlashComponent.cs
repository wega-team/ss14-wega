using Robust.Shared.GameStates;
using Content.Shared.Actions;
using Robust.Shared.Prototypes;
namespace Content.Shared.Flash.Components;

/// <summary>
/// Upon being triggered will flash in an area around it.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ImpulseFlashComponent : Component
{
    [DataField]
    public float Range = 1.0f;

    [DataField]
    public float FlashCharge = 100f;

    [DataField]
    public EntProtoId FlashAction = "ActionToggleFlashHelm";

    [DataField]
    public EntityUid? FlashActionEntity;

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(8);

    [DataField]
    public float Probability = 1.0f;

    [DataField]
    public float SlowTo = 0.5f;
}
