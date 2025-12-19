using Robust.Shared.GameStates;
using Robust.Shared.Audio;

namespace Content.Shared.Sharpening.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SharpenerComponent : Component
{
    [DataField("Strength"), AutoNetworkedField]
    public int Strength = 1;

    [DataField("UsesMultiplier"), AutoNetworkedField]
    public float UsesMultiplier = 1f;

    [DataField("SharpeningSound"), AutoNetworkedField]
    public SoundSpecifier SharpeningSound = new SoundPathSpecifier("/Audio/_Wega/Effects/anvil.ogg");
}
