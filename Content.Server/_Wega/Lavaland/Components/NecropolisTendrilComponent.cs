using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Lavaland.Components;

[RegisterComponent]
public sealed partial class NecropolisTendrilComponent : Component
{
    [DataField]
    public EntProtoId ChasmPrototype = "FloorChasmEntity";

    [DataField]
    public float ChasmDelay = 10f;

    [DataField]
    public SoundSpecifier ChasmSound = new SoundPathSpecifier("/Audio/_Wega/Effects/Planet/rumble.ogg", AudioParams.Default.WithVolume(6));
}
