using Robust.Shared.Prototypes;

namespace Content.Shared.Lavaland.Artefacts.Components;

[RegisterComponent]
public sealed partial class ProphetClothingComponent : Component
{
    [DataField(required: true)]
    public EntProtoId BulletProto;

    [DataField]
    public float ProbChance = 0.2f;
}
