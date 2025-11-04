namespace Content.Shared.Mining.Components;

[RegisterComponent]
public sealed partial class MiningServerComponent : Component
{
    [DataField("powerConsumption")]
    public float PowerConsumption = 1000f; // Вт

    [DataField("heatGeneration")]
    public float HeatGeneration = 0.15f; // Тепло в секунду

    [ViewVariables(VVAccess.ReadWrite)]
    public float ActualPowerConsumption => PowerConsumption * MiningStage;

    [ViewVariables(VVAccess.ReadWrite)]
    public float ActualHeatGeneration => HeatGeneration * MiningStage;

    [ViewVariables(VVAccess.ReadWrite)]
    public int MiningStage = 1; // 1-3 стадии

    [ViewVariables(VVAccess.ReadWrite)]
    public MiningMode Mode = MiningMode.Credits;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool IsActive;

    [ViewVariables]
    public bool IsBroken;

    [ViewVariables]
    public float CurrentTemperature = 293f;

    [DataField("breakdownTemperature")]
    public float BreakdownTemperature = 350f; // K
}

public enum MiningMode : byte
{
    Credits,
    Research
}
