using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.Injector.Fabticator;

[Serializable, NetSerializable]
public enum InjectorFabticatorUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class InjectorFabticatorBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly bool IsProducing;
    public readonly bool CanProduce;
    public readonly NetEntity? Beaker;
    public readonly ContainerInfo? BeakerContainerInfo;
    public readonly Solution? BufferSolution;
    public readonly FixedPoint2 BufferVolume;
    public readonly FixedPoint2 BufferMaxVolume;
    public readonly Dictionary<ReagentId, FixedPoint2>? Recipe;
    public readonly string? CustomName;
    public readonly int InjectorsToProduce;
    public readonly int InjectorsProduced;

    public InjectorFabticatorBoundUserInterfaceState(
        bool isProducing,
        bool canProduce,
        NetEntity? beaker,
        ContainerInfo? beakerContainerInfo,
        Solution? bufferSolution,
        FixedPoint2 bufferVolume,
        FixedPoint2 bufferMaxVolume,
        Dictionary<ReagentId, FixedPoint2>? recipe,
        string? customName,
        int injectorsToProduce,
        int injectorsProduced)
    {
        IsProducing = isProducing;
        CanProduce = canProduce;
        Beaker = beaker;
        BeakerContainerInfo = beakerContainerInfo;
        BufferSolution = bufferSolution;
        BufferVolume = bufferVolume;
        BufferMaxVolume = bufferMaxVolume;
        Recipe = recipe;
        CustomName = customName;
        InjectorsToProduce = injectorsToProduce;
        InjectorsProduced = injectorsProduced;

    }
}

[Serializable, NetSerializable]
public sealed class InjectorFabticatorTransferBufferToBeakerMessage : BoundUserInterfaceMessage
{
    public readonly ReagentId ReagentId;
    public readonly FixedPoint2 Amount;

    public InjectorFabticatorTransferBufferToBeakerMessage(ReagentId reagentId, FixedPoint2 amount)
    {
        ReagentId = reagentId;
        Amount = amount;
    }
}

[Serializable, NetSerializable]
public sealed class InjectorFabticatorTransferBeakerToBufferMessage : BoundUserInterfaceMessage
{
    public readonly ReagentId ReagentId;
    public readonly FixedPoint2 Amount;

    public InjectorFabticatorTransferBeakerToBufferMessage(ReagentId reagentId, FixedPoint2 amount)
    {
        ReagentId = reagentId;
        Amount = amount;
    }
}

[Serializable, NetSerializable]
public sealed class InjectorFabticatorSetReagentMessage : BoundUserInterfaceMessage
{
    public readonly ReagentId ReagentId;
    public readonly FixedPoint2 Amount;

    public InjectorFabticatorSetReagentMessage(ReagentId reagentId, FixedPoint2 amount)
    {
        ReagentId = reagentId;
        Amount = amount;
    }
}

[Serializable, NetSerializable]
public sealed class InjectorFabticatorRemoveReagentMessage : BoundUserInterfaceMessage
{
    public readonly ReagentId ReagentId;

    public InjectorFabticatorRemoveReagentMessage(ReagentId reagentId)
    {
        ReagentId = reagentId;
    }
}

[Serializable, NetSerializable]
public sealed class InjectorFabticatorProduceMessage : BoundUserInterfaceMessage
{
    public readonly int Amount;
    public readonly string? CustomName;

    public InjectorFabticatorProduceMessage(int amount, string? customName)
    {
        Amount = amount;
        CustomName = customName;
    }
}

[Serializable, NetSerializable]
public sealed class InjectorFabticatorEjectMessage : BoundUserInterfaceMessage { }

[Serializable, NetSerializable]
public sealed class InjectorFabticatorSyncRecipeMessage : BoundUserInterfaceMessage
{
    public readonly Dictionary<ReagentId, FixedPoint2>? Recipe;

    public InjectorFabticatorSyncRecipeMessage(Dictionary<ReagentId, FixedPoint2>? recipe)
    {
        Recipe = recipe;
    }
}
