using Content.Shared.Tools;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Weapons.Misc.Upgrades.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(CrusherUpgradeSystem))]
public sealed partial class UpgradeableCrusherComponent : Component
{
    [DataField]
    public string UpgradesContainerId = "crusher_upgrades";

    [DataField]
    public EntityWhitelist Whitelist = new();

    [DataField]
    public SoundSpecifier InsertSound = new SoundPathSpecifier("/Audio/Weapons/Guns/MagIn/revolver_magin.ogg");

    [DataField]
    public int MaxUpgradeCount = 3;

    [DataField]
    public ProtoId<ToolQualityPrototype> Tool = "Slicing";
}
