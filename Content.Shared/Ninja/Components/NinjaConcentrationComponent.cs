using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Ninja.Components;

[Serializable, NetSerializable]
public enum WidowInput : byte
{
    Harm,
    Disarm,
    Help,
    Grab,
}

/// <summary>
/// Added to the ninja mob when the Widow Art suit is worn.
/// Tracks concentration state and current combo input history.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class NinjaConcentrationComponent : Component
{
    [DataField]
    public ProtoId<AlertPrototype> ConcentrationAlert = "NinjaConcentration";

    // Sequence of recent inputs against ComboTarget — networked so the client can render the HUD
    [DataField, AutoNetworkedField]
    public List<WidowInput> ComboHistory = new();

    // Entity that recent inputs were made against
    [DataField]
    public EntityUid? ComboTarget;

    // CurTime when the last input was recorded (used to reset combo after 8 s)
    [DataField]
    public TimeSpan LastInputTime;

    // Server-only flag: true while the HUD icon is showing the cooldown animation.
    // Used to fire ShowAlert(severity=1) exactly once when the cooldown expires.
    public bool AlertShowingCooldown;

    // CurTime when concentration cooldown expires (zero = no cooldown = concentrated)
    [DataField, AutoNetworkedField]
    public TimeSpan CooldownEnd;

    // Cooldown after a normal successful combo
    [DataField]
    public TimeSpan ConcentrationCooldown = TimeSpan.FromSeconds(15);

    // Long cooldown after Neck Slice
    [DataField]
    public TimeSpan NeckSliceCooldown = TimeSpan.FromSeconds(60);

    // Combo sequence resets this many seconds after the last input
    [DataField]
    public TimeSpan ComboResetTime = TimeSpan.FromSeconds(8);

    // Server-only: CurTime when tornado spin ends (zero = not spinning)
    public TimeSpan TornadoSpinUntil;
}
