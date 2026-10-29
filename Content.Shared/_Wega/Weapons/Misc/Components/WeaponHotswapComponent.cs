using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.Weapons.Misc.Components;

[RegisterComponent]
public sealed partial class WeaponHotswapComponent : Component
{
    public const string PairedWeaponContainerId = "hotswap-paired";

    [DataField(required: true)]
    public EntProtoId AlternateForm = default!;

    [DataField(required: true)]
    public bool IsAlternate = false;

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? PairedWeapon;

    [DataField]
    public SoundSpecifier? SwapSound = new SoundPathSpecifier("/Audio/Items/toolbox_insert.ogg");
}
