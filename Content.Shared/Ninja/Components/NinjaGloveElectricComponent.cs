using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Ninja.Components;

/// <summary>
/// Added to the ninja user when gloves are toggled on.
/// Deals stamina damage and plays a zap sound on disarm.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NinjaGloveElectricComponent : Component
{
    [DataField]
    public SoundSpecifier ZapSound = new SoundPathSpecifier("/Audio/Items/Defib/defib_zap.ogg");

    [DataField]
    public float DisarmStaminaDamage = 25f;
}
