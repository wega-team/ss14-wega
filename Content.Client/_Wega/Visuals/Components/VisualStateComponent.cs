namespace Content.Client.Visuals;

/// <summary>
/// A visual system component that uses sprites to change the overall state of the system without creating hundreds of different visual systems.
/// </summary>
[RegisterComponent]
public sealed partial class VisualStateComponent : Component
{
    [DataField]
    public string? MainLayer;

    [DataField]
    public Dictionary<string, List<PrototypeLayerData>> InhandStates = new();

    [DataField]
    public Dictionary<string, List<PrototypeLayerData>> EquipmentStates = new();
}
