using Content.Shared._Wega.Implants.Components;
using Content.Shared.Body.Part;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Wega.Implants.Components
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class BodyPartImplantComponent : Component
    {
        [DataField]
        public Dictionary<string, BodyPartType> Connections = new();

        [DataField("key")]
        public string? ImplantKey;
        [DataField]
        public ComponentRegistry? ImplantComponents = default!;
    }
}

[ByRefEvent]
public readonly record struct BodyPartImplantAddedEvent(string Slot, Entity<BodyPartImplantComponent?> Part);

[ByRefEvent]
public readonly record struct BodyPartImplantRemovedEvent(string Slot, Entity<BodyPartImplantComponent?> Part);
