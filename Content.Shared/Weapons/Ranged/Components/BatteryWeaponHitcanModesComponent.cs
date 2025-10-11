using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
/// Allows battery weapons to fire different types of projectiles
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(BatteryWeaponHitscanModesSystem))]
[AutoGenerateComponentState]
public sealed partial class BatteryWeaponHitscanModesComponent : Component
{
    /// <summary>
    /// A list of the different firing modes the weapon can switch between
    /// </summary>
    [DataField(required: true)]
    [AutoNetworkedField]
    public List<BatteryWeaponHitscanMode> FireModes = new();

    /// <summary>
    /// The currently selected firing mode
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public int CurrentFireMode;
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class BatteryWeaponHitscanMode
{
    /// <summary>
    /// The projectile prototype associated with this firing mode
    /// </summary>
    [DataField("proto", required: true)]
    public ProtoId<HitscanPrototype> Prototype = default!;

    [DataField]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The battery cost to fire the projectile associated with this firing mode
    /// </summary>
    [DataField]
    public float FireCost = 100;

    [DataField]
    public string State = string.Empty;

    [DataField]
    public string MagState = string.Empty;
}

[Serializable, NetSerializable]
public enum  BatteryWeaponHitscanModesVisuals : byte
{
    State,
    MagState // Corvax-Wega-MagVisuals-Add
}
