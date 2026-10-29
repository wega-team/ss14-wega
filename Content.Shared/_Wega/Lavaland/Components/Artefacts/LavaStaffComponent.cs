using Content.Shared.Maps;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.Lavaland.Artefacts.Components;

[RegisterComponent]
public sealed partial class LavaStaffComponent : Component
{
    [DataField]
    public EntProtoId LavaEntity = "FloorLavaEntity";

    [DataField]
    public ProtoId<ContentTileDefinition> BasaltTile = "FloorBasalt";

    [DataField]
    public SoundSpecifier? UseSound;
}
