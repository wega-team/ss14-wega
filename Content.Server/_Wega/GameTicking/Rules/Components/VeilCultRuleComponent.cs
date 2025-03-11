namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Stores data for <see cref="VeilCultRuleSystem"/>.
/// </summary>
[RegisterComponent, Access(typeof(VeilCultRuleSystem))]
public sealed partial class VeilCultRuleComponent : Component
{
    [DataField]
    public VeilCultWinType WinType = VeilCultWinType.Neutral;

    [DataField]
    public List<VeilCultWinType> VeilCultWinCondition = new();
}

public enum VeilCultWinType : byte
{
    GodCalled,
    RitualConducted,
    Neutral,
    CultLose
}
