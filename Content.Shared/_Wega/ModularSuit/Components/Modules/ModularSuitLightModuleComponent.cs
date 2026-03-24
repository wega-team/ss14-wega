using Content.Shared.Tools;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Modular.Suit;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ModularSuitLightModuleComponent : Component
{
    [DataField]
    public string TargetSlot = "head";

    [DataField]
    public ProtoId<ToolQualityPrototype> Tool = "Pulsing";

    [DataField]
    public ComponentRegistry? GuaranteedRemoved { get; set; }

    [DataField, AutoNetworkedField]
    public Color LightColor = Color.White;

    [DataField, AutoNetworkedField]
    public float LightEnergy = 2f;

    [DataField, AutoNetworkedField]
    public float LightRadius = 3f;

    [DataField, AutoNetworkedField]
    public bool Multicoloured = false;
}
