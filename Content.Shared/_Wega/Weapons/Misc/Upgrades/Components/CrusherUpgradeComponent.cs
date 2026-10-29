using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Weapons.Misc.Upgrades.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(CrusherUpgradeSystem))]
public sealed partial class CrusherUpgradeComponent : Component
{
    [DataField]
    public List<ProtoId<TagPrototype>> Tags = new();

    [DataField]
    public LocId ExamineText;
}
