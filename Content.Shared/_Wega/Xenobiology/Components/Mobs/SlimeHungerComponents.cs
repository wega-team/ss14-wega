using Robust.Shared.GameStates;

namespace Content.Shared.Xenobiology.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class SlimeHungerComponent : Component
{
    [DataField("baseDecayRate")]
    public float DecayRate = 0.1f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float Hunger = 100f;

    [ViewVariables(VVAccess.ReadOnly)]
    public float MaxHunger = 200f;

    [DataField("feedCooldown")]
    public float FeedCooldown = 4f;

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan LastFeedTime;

    [DataField("behaviorThresholds")]
    public Dictionary<SlimeBehaviorState, float> ThresholdPercentages = new()
    {
        { SlimeBehaviorState.Passive, 0.35f },
        { SlimeBehaviorState.Hungry, 0.15f },
        { SlimeBehaviorState.Aggressive, 0f }
    };

    [ViewVariables]
    public SlimeBehaviorState CurrentState;
}

[RegisterComponent]
public sealed partial class SlimeFoodComponent : Component;

public enum SlimeBehaviorState
{
    Passive,
    Hungry,
    Aggressive,
    Dividing
}
