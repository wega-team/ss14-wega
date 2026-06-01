using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.Ninja.Components;

/// <summary>
/// Added to the ninja suit to give the player jetpack-like flight powered by suit charge.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NinjaJetpackComponent : Component
{
    [DataField] public EntProtoId ToggleAction = "ActionToggleNinjaJetpack";

    [DataField, AutoNetworkedField]
    public EntityUid? ToggleActionEntity;

    /// <summary>Who is currently using this jetpack.</summary>
    [DataField, AutoNetworkedField]
    public EntityUid? JetpackUser;

    /// <summary>Suit charge drained per second while active.</summary>
    [DataField]
    public float ChargePerSecond = 10f;

    [DataField] public float Acceleration = 1f;
    [DataField] public float WeightlessModifier = 1.2f;
    [DataField] public float Friction = 0.25f;
}

/// <summary>Added to the suit while the ninja jetpack is active.</summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ActiveNinjaJetpackComponent : Component
{
    /// <summary>Accumulates time on server to drain charge once per second.</summary>
    public float ChargeAccumulator = 1f;

    // Particle effect throttling (client-side).
    public float EffectCooldown = 0.3f;
    public float MaxDistance = 0.7f;
    public EntityCoordinates LastCoordinates;
    public TimeSpan TargetTime = TimeSpan.Zero;
}

public sealed partial class ToggleNinjaJetpackEvent : InstantActionEvent;
