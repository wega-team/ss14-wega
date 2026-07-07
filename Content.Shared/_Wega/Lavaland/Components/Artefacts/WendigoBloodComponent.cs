using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.Lavaland.Artefacts.Components;

[RegisterComponent]
public sealed partial class WendigoBloodComponent : Component
{
    [DataField]
    public EntProtoId EntAction = "ActionPolymorphPolarBear";

    [DataField]
    public SoundSpecifier UseSound = new SoundPathSpecifier("/Audio/Items/drink.ogg");
}
