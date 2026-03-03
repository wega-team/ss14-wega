using Content.Shared.Damage;
using Content.Shared.Weapons.Misc.Upgrades; // Corvax-Wega-Lavaland
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Marker;

/// <summary>
/// Applies <see cref="DamageMarkerComponent"/> when colliding with an entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedDamageMarkerSystem), typeof(CrusherUpgradeEffectsSystem))] // Corvax-Wega-Lavaland-Edit
public sealed partial class DamageMarkerOnCollideComponent : Component
{
    [DataField("whitelist"), AutoNetworkedField]
    public EntityWhitelist? Whitelist = new();

    [ViewVariables(VVAccess.ReadWrite), DataField("duration"), AutoNetworkedField]
    public TimeSpan Duration = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Additional damage to be applied.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("damage")]
    public DamageSpecifier Damage = new();

    /// <summary>
    /// How many more times we can apply it.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("amount"), AutoNetworkedField]
    public int Amount = 1;

    // Corvax-Wega-Lavaland-Add-start
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public bool Weakening;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float WeakeningModifier = 1f;
    // Corvax-Wega-Lavaland-Add-end
}
