using Robust.Shared.Prototypes;

namespace Content.Shared.Modular.Suit;

[RegisterComponent]
public sealed partial class ModularSuitActionModuleComponent : Component
{
    [DataField(required: true)]
    public EntProtoId Action;
}
