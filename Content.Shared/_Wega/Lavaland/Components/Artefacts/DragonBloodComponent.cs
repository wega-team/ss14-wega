using Content.Shared.Polymorph;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.Lavaland.Artefacts.Components;

[RegisterComponent]
public sealed partial class DragonBloodComponent : Component
{
    [DataField]
    public ProtoId<PolymorphPrototype> Skeleton = "SkeletonPolymorph";

    [DataField]
    public EntProtoId LowerDrake = "BecomeToDrakeAction";

    [DataField]
    public SoundSpecifier UseSound = new SoundPathSpecifier("/Audio/Items/drink.ogg");
}
