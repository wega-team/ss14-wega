using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Weapons.Misc.Upgrades;

[RegisterComponent, NetworkedComponent, Access(typeof(CrusherUpgradeEffectsSystem))]
public sealed partial class CrusherBroodmotherTongueUpgradeComponent : Component
{
    [DataField] public EntProtoId SpawnProto = "EffectGoliathTentacleSpawn";
    [DataField] public float SpawnProb = 0.1f;
    [DataField] public int SpawnCount = 3;

    [DataField]
    public List<Direction> OffsetDirections = new()
    {
        Direction.North,
        Direction.South,
        Direction.East,
        Direction.West,
    };
}
