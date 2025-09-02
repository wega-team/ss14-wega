namespace Content.Server.CoreTempChange.Components;

[RegisterComponent]
public sealed partial class CoreTempChangeComponent : Component
{
    [DataField("tempChangePerSecond")]
    public float TempChangePerSecond = 0;
}