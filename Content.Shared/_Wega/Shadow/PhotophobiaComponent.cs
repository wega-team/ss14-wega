using Robust.Shared.Audio;

namespace Content.Shared.Shadow.Components;

[RegisterComponent]
public sealed partial class PhotophobiaComponent : Component
{
    [DataField("damagePerLight")]
    public float DamagePerLight = 0.5f;

    [DataField("interval")]
    public float Interval = 1f;

    [DataField("nextTickTime")]
    public TimeSpan NextTickTime;

    [DataField("damageSound")]
    public SoundSpecifier DamageSound = new SoundPathSpecifier("/Audio/Weapons/Guns/Hits/energy_meat2.ogg");

    [DataField("applyShadowWeakness")]
    public bool ApplyShadowWeakness = false;

    [DataField("speedModifier")]
    public float SpeedModifier = 0.75f;
    [DataField("damageModfier")]
    public float DamageModfier = 0.2f;
}
