using Robust.Shared.Prototypes;

namespace Content.Shared.Lavaland.Artefacts.Components;

[RegisterComponent]
public sealed partial class LinkedCubeComponent : Component
{
    [DataField(required: true)]
    public EntProtoId PairPrototype = default!;

    [DataField]
    public float MinTeleportDistance = 4f;

    [DataField]
    public EntityUid? LinkedCube;

    [DataField]
    public bool IsPrimary = false;
}
