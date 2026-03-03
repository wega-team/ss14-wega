using Content.Shared.Polymorph;
using Robust.Shared.Prototypes;

namespace Content.Shared.Lavaland.Components;

[RegisterComponent]
public sealed partial class LegionReversibleComponent : Component
{
    [DataField] public ProtoId<PolymorphPrototype> BasePolymorph = "LegionPolymorph";
    [DataField] public ProtoId<PolymorphPrototype> DwarfPolymorph = "LegionDwarfPolymorph";
}
