using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Clothing.Upgrades.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(ClothingUpgradeSystem))]
public sealed partial class ClothingUpgradeComponent : Component
{
    [DataField]
    public List<ProtoId<TagPrototype>> Tags = new();

    [DataField]
    public LocId ExamineText;

    [DataField("sprite")]
    public SpriteSpecifier? EquippedState;
}
