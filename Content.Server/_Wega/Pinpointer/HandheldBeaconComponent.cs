using Robust.Shared.Audio;

namespace Content.Server.Pinpointer;

[RegisterComponent]
public sealed partial class HandheldBeaconComponent : Component
{
    [DataField]
    public bool ActiveBeacon;

    [DataField]
    public SoundSpecifier SoundActivation = new SoundPathSpecifier("/Audio/Effects/box_deploy.ogg");
}
