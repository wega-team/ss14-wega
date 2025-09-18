using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.RCD.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class RCDUpgradeKitComponent : Component
{
    [DataField("UpgradeSound")]
    public SoundSpecifier UpgradeSound = new SoundPathSpecifier("/Audio/Machines/id_insert.ogg");
}
