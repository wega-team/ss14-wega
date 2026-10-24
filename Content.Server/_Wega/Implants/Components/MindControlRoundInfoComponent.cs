namespace Content.Server._Wega.Implants.Components;

[RegisterComponent]
public sealed partial class MindControlRoundInfoComponent : Component
{
    [DataField]
    public string ControlledName = string.Empty;

    [DataField]
    public EntityUid MasterMind = EntityUid.Invalid;

    [DataField]
    public string MasterName = string.Empty;
}