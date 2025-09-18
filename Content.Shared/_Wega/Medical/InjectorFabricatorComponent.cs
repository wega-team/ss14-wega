using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.Injector.Fabticator;

[RegisterComponent]
public sealed partial class InjectorFabticatorComponent : Component
{
    public const string BeakerSlotId = "beakerSlot";
    public const string BufferSolutionName = "buffer";

    [DataField(required: true)]
    public EntProtoId Injector;

    [DataField]
    public string? CustomName;

    [DataField]
    public ItemSlot BeakerSlot = new();

    [ViewVariables]
    public FixedPoint2 BufferVolume = 0;

    [ViewVariables]
    public FixedPoint2 BufferMaxVolume = 2000;

    [ViewVariables]
    public bool IsProducing;

    [ViewVariables]
    public float ProductionTime = 4f;

    [ViewVariables]
    public int InjectorsToProduce;

    [ViewVariables]
    public int InjectorsProduced;

    [ViewVariables]
    public float ProductionTimer;

    [ViewVariables]
    public Dictionary<ReagentId, FixedPoint2>? Recipe;
}

