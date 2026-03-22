using Robust.Shared.Prototypes;

namespace Content.Shared.Modular.Suit;

[RegisterComponent]
public sealed partial class ModularSuitModuleWearerEffectComponent : Component
{
    [DataField(required: true)]
    public ComponentRegistry? ActiveComponents { get; set; }
}
