using Robust.Shared.GameStates;

namespace Content.Shared.Lavaland.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class OreValueComponent : Component
{
    [DataField]
    public double Points { get; set; } = 0;

    [DataField]
    public bool Mined { get; set; }
}
