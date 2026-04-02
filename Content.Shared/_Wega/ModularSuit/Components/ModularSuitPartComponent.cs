using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Modular.Suit;

[Access(typeof(SharedModularSuitSystem))]
[RegisterComponent, NetworkedComponent]
public sealed partial class ModularSuitPartComponent : Component
{
    [DataField]
    public SuitPartType PartType;

    [DataField]
    public TimeSpan ToggleDelay = TimeSpan.FromSeconds(1.5);

    [DataField]
    public SpriteSpecifier? VerbIcon;
}
