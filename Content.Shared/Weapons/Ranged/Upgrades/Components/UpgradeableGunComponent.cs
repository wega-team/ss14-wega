using Content.Shared.Tools; // Corvax-Wega-Lavaland
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes; // Corvax-Wega-Lavaland

namespace Content.Shared.Weapons.Ranged.Upgrades.Components;

/// <summary>
/// Component that stores and manages <see cref="GunUpgradeComponent"/> that modify a given weapon.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(GunUpgradeSystem))]
public sealed partial class UpgradeableGunComponent : Component
{
    /// <summary>
    /// ID of container that holds upgrades.
    /// </summary>
    [DataField]
    public string UpgradesContainerId = "upgrades";

    /// <summary>
    /// Whitelist which denotes the types of upgrades that can be added.
    /// </summary>
    [DataField]
    public EntityWhitelist Whitelist = new();

    /// <summary>
    /// Sound played when upgrade is inserted.
    /// </summary>
    [DataField]
    public SoundSpecifier? InsertSound = new SoundPathSpecifier("/Audio/Effects/thunk.ogg");

    /// <summary>
    /// The maximum amount of upgrades this gun can hold.
    /// </summary>
    [DataField]
    public int MaxUpgradeCount = 2;

    // Corvax-Wega-Lavaland-start
    [DataField]
    public ProtoId<ToolQualityPrototype> Tool = "Screwing";
    // Corvax-Wega-Lavaland-end
}
