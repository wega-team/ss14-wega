using Robust.Shared.Prototypes;

namespace Content.Shared.Modular.Suit;

[RegisterComponent]
public sealed partial class ModularSuitModuleEffectComponent : Component
{
    [DataField]
    public string TargetSlot = "back";

    [DataField(required: true)]
    public ComponentRegistry? ActiveComponents { get; set; }

    /// <summary>
    /// A specific field if you need to return a specific state to an existing component.
    /// </summary>
    [DataField]
    public ComponentRegistry? ReturnedComponents { get; set; }
}
