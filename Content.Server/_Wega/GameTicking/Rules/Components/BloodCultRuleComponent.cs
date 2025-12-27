using Content.Server.Blood.Cult;
using Content.Shared.Blood.Cult;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Stores data for <see cref="BloodCultRuleSystem"/> and <see cref="BloodCultSystem"/>.
/// </summary>
[RegisterComponent, Access(typeof(BloodCultRuleSystem), typeof(BloodCultSystem))]
public sealed partial class BloodCultRuleComponent : Component
{
    [DataField]
    public BloodCultGod? SelectedGod;

    [DataField]
    public BloodCultWinType WinType = BloodCultWinType.Neutral;

    [DataField]
    public List<BloodCultWinType> BloodCultWinCondition = new();

    [DataField]
    public HashSet<EntityUid> SelectedTargets = new();

    public EntProtoId ObjectivePrototype = "BloodCultTargetObjective";

    [DataField]
    public int Curses = 2;

    [DataField]
    public int Offerings = 3;

    [DataField] public bool FirstTriggered;
    [DataField] public bool SecondTriggered;
    [DataField] public bool RitualStage;
}

public enum BloodCultWinType : byte
{
    GodCalled,
    RitualConducted,
    Neutral,
    CultLose
}
