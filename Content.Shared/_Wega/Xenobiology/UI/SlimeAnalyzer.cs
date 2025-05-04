using Content.Shared.Xenobiology.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Xenobiology.UI;

[Serializable, NetSerializable]
public enum SlimeAnalyzerUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class SlimeAnalyzerScannedUserMessage : BoundUserInterfaceMessage
{
    public NetEntity TargetEntity;
    public float Hunger;
    public float MaxHunger;
    public SlimeBehaviorState BehaviorState;
    public SlimeStage GrowthStage;
    public SlimeType SlimeType;
    public float MutationChance;
    public float RainbowChance;
    public List<(SlimeType type, float weight)>? PossibleMutations;

    public SlimeAnalyzerScannedUserMessage(
        NetEntity targetEntity,
        float hunger,
        float maxHunger,
        SlimeBehaviorState behaviorState,
        SlimeStage growthStage,
        SlimeType slimeType,
        float mutationChance,
        float rainbowChance,
        List<(SlimeType type, float weight)>? possibleMutations)
    {
        TargetEntity = targetEntity;
        Hunger = hunger;
        MaxHunger = maxHunger;
        BehaviorState = behaviorState;
        GrowthStage = growthStage;
        SlimeType = slimeType;
        MutationChance = mutationChance;
        RainbowChance = rainbowChance;
        PossibleMutations = possibleMutations;
    }
}
