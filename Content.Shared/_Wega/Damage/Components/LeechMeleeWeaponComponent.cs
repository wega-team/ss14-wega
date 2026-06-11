using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Damage.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class LeechMeleeWeaponComponent : Component
{
    [DataField]
    public DamageSpecifier? Heal = default!;

    [DataField]
    public GroupHealSpecifier? HealGroups = default!;

    [DataField]
    public bool Weighted = false;

    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;
}
