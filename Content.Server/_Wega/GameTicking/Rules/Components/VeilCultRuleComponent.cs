using Content.Server.Veil.Cult;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Stores data for <see cref="VeilCultRuleSystem"/> and <see cref="VeilCultSystem"/>.
/// </summary>
[RegisterComponent, Access(typeof(VeilCultRuleSystem), typeof(VeilCultSystem))]
public sealed partial class VeilCultRuleComponent : Component
{
    [DataField]
    public float EnergyCount;

    [DataField]
    public EntityUid? Station;

    [DataField]
    public VeilCultWinType WinType = VeilCultWinType.Neutral;

    [DataField]
    public List<VeilCultWinType> VeilCultWinCondition = new();

    [DataField]
    public HashSet<EntityUid> SelectedTargets = new();

    public EntProtoId ObjectivePrototype = "VeilCultBeaconObjective";

    [DataField]
    public int Beacons;

    [DataField] public bool FirstTriggered;
    [DataField] public bool SecondTriggered;
    [DataField] public bool RitualStage;
    [DataField] public bool RitualGoing;
}

public enum VeilCultWinType : byte
{
    GodCalled,
    RitualConducted,
    Neutral,
    CultLose
}
