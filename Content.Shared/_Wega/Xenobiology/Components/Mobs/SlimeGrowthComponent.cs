using Robust.Shared.GameStates;

namespace Content.Shared.Xenobiology.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class SlimeGrowthComponent : Component
{
    [ViewVariables]
    public SlimeStage CurrentStage = SlimeStage.Young;

    [DataField("nextStageHungerThreshold")]
    public float NextStageHungerThreshold = 200f;

    [DataField("slimeType")]
    public SlimeType SlimeType = SlimeType.Gray;

    [DataField("mutationChance")]
    public float MutationChance = 0.3f;

    [DataField("rainbowChance")]
    public float RainbowChance = 0.01f;

    public bool Stabilized = false;

    public bool Reinforced = false;
}
