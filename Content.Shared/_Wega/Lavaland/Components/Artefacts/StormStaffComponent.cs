using Content.Shared.Explosion;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.Lavaland.Artefacts.Components;

[RegisterComponent]
public sealed partial class StormStaffComponent : Component
{
    [DataField]
    public int MaxCharges = 3;

    [ViewVariables(VVAccess.ReadOnly)]
    public int Charges = 3;

    [DataField]
    public TimeSpan ChargeCooldown = TimeSpan.FromSeconds(15f);

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextChargeTime;

    [DataField]
    public TimeSpan FireDelay = TimeSpan.FromSeconds(3f);

    [DataField]
    public EntProtoId BeamPrototype = "LightningStormStaff";

    [DataField]
    public EntProtoId EmpoweredBeamPrototype = "LightningStormStaffEmpowered";

    [DataField]
    public ProtoId<ExplosionPrototype> ExplosionEffect = "Default";

    [DataField]
    public TimeSpan WeatherCancelCooldown = TimeSpan.FromMinutes(20f);

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextWeatherCancelTime;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsFiring = false;

    [DataField]
    public SoundSpecifier UseSound = new SoundPathSpecifier("/Audio/Weapons/Guns/Hits/taser_hit.ogg");

    [DataField]
    public SoundSpecifier ChargeSound = new SoundPathSpecifier("/Audio/_Wega/Effects/staff_change.ogg",
        AudioParams.Default.WithVolume(-4));
}
