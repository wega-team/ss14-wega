using Content.Shared._Wega.Implants.Components;
using Content.Shared.Body;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Wega.Implants.Components
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class BodyPartImplantComponent : Component
    {
        [DataField]
        public Dictionary<ProtoId<OrganCategoryPrototype>, string> Connections = new();

        [DataField]
        public Dictionary<string, ProtoId<OrganCategoryPrototype>> Parts = new();

        [DataField("key")]
        public string? ImplantKey;
        [DataField]
        public ComponentRegistry? ImplantComponents = default!;
    }
}

[ByRefEvent]
public readonly record struct BodyPartImplantAddedEvent(Entity<BodyPartImplantComponent?> Part);

[ByRefEvent]
public readonly record struct BodyPartImplantRemovedEvent(Entity<BodyPartImplantComponent?> Part);
