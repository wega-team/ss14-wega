using Content.Server.Codewords;
using Content.Shared.Dataset;
using Content.Shared.NPC.Prototypes;
using Content.Shared.Random;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(BloodBrotherRuleSystem))]
public sealed partial class BloodBrotherRuleComponent : Component
{
    public readonly List<EntityUid> BloodBrotherMinds = new();
    public readonly Dictionary<EntityUid, EntityUid> BloodBrotherPairs = new();

    [DataField]
    public EntProtoId BloodBrotherPrototypeId = "MindRoleBloodBrother";

    [DataField]
    public ProtoId<CodewordFactionPrototype> CodewordFactionPrototypeId = "Traitor";

    [DataField]
    public ProtoId<NpcFactionPrototype> NanoTrasenFaction = "NanoTrasen";

    [DataField]
    public ProtoId<NpcFactionPrototype> SyndicateFaction = "Syndicate";

    [DataField]
    public ProtoId<LocalizedDatasetPrototype> ObjectiveIssuers = "TraitorCorporations";

    /// <summary>
    /// Give blood brothers a briefing in chat.
    /// </summary>
    [DataField]
    public bool GiveBriefing = true;

    public int TotalBloodBrothers => BloodBrotherMinds.Count;

    [DataField]
    public SoundSpecifier GreetSoundNotification = new SoundPathSpecifier("/Audio/Ambience/Antag/traitor_start.ogg");

    /// <summary>
    /// Whether both brothers must survive for individual objectives to count
    /// </summary>
    [DataField]
    public bool RequireBothAlive = true;

    /// <summary>
    /// A list of required goals for all pairs of brothers
    /// </summary>
    [DataField]
    public List<EntProtoId> RequiredObjectives = new();

    /// <summary>
    /// A list of goal sets for the brothers
    /// </summary>
    [DataField]
    public List<BloodBrotherObjectiveSet> ObjectiveSets = new();

    /// <summary>
    /// Maximum difficulty of goals for a pair of brothers
    /// </summary>
    [DataField]
    public float MaxDifficulty = 10f;
}

/// <summary>
/// A set of goals for blood brothers
/// </summary>
[DataDefinition]
public sealed partial class BloodBrotherObjectiveSet
{
    [DataField(required: true)]
    public ProtoId<WeightedRandomPrototype> Groups;

    [DataField]
    public float Prob = 1.0f;

    [DataField]
    public int MaxPicks = 2;
}
