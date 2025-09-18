using Robust.Shared.Prototypes;

namespace Content.Shared.Lavaland.Artefacts.Components;

[RegisterComponent]
public sealed partial class ShipInBottleComponent : Component
{
    [DataField]
    public EntProtoId BoatPrototype = "VehicleDragonBoat";

    [DataField]
    public float MaxSpawnDistance = 1.5f;

    [DataField]
    public float BoatCheckRadius = 3f;

    [DataField]
    public EntityUid? SpawnedBoat;
}
