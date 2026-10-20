using Robust.Shared.GameStates;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.Sharpening.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SharpenerComponent : Component
{
    [DataField("strength"), AutoNetworkedField]
    public int Strength = 1;

    [DataField("usesMultiplier"), AutoNetworkedField]
    public float UsesMultiplier = 1f;

    [DataField("sharpeningSound"), AutoNetworkedField]
    public SoundSpecifier SharpeningSound = new SoundPathSpecifier("/Audio/_Wega/Effects/anvil.ogg");

    [DataField("deleteOnSharpening"), AutoNetworkedField]
    public bool DeleteOnSharpening = false;

    [DataField("spawnOnDelete"), AutoNetworkedField]
    public EntProtoId Prototype = string.Empty;
}
