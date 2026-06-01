using Content.Server.Antag.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Ninja.Components;

/// <summary>
/// Assigns objectives to ninja after crew has spawned,
/// so that PickRandomPerson can find valid targets.
/// </summary>
[RegisterComponent]
public sealed partial class NinjaDelayedObjectivesComponent : Component
{
    [DataField]
    public List<AntagObjectiveSet> Sets = new();

    [DataField]
    public float MaxDifficulty = 5f;

    [DataField]
    public List<EntProtoId> FixedObjectives = new();
}
