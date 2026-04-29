using Content.Shared.Tools;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Wega.Implants.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class BodyPartImplantComponent : Component
{
    [DataField]
    public Dictionary<string, ComponentRegistry> Configurations = new();
    [DataField]
    public ProtoId<ToolQualityPrototype> ConfigurationTool = "Screwing";
    public int CurrentConfig = 0;

    [DataField("key")]
    public string? ImplantKey;
    [DataField]
    public ComponentRegistry ImplantComponents = new();
}

[ByRefEvent]
public readonly record struct BodyPartImplantAddedEvent(Entity<BodyPartImplantComponent?> Part);

[ByRefEvent]
public readonly record struct BodyPartImplantRemovedEvent(Entity<BodyPartImplantComponent?> Part);
