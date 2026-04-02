using System.Numerics;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Content.Shared.Traits;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Humanoid;

/// <summary>
/// Holds all of the data for importing / exporting character profiles.
/// </summary>
[DataDefinition]
public sealed partial class HumanoidProfileExportV1
{
    [DataField]
    public string ForkId;

    [DataField]
    public int Version = 1;

    [DataField(required: true)]
    public HumanoidCharacterProfileV1 Profile = default!;

    public HumanoidProfileExportV2 ToV2()
    {
        return new()
        {
            ForkId = ForkId,
            Version = 2,
            Profile = Profile.ToV2()
        };
    }
}

[DataDefinition, Serializable]
public sealed partial class HumanoidCharacterProfileV1
{
    [DataField("_jobPriorities")]
    public Dictionary<ProtoId<JobPrototype>, JobPriority> JobPriorities = new();

    [DataField("_antagPreferences")]
    public HashSet<ProtoId<AntagPrototype>> AntagPreferences = new();

    [DataField("_traitPreferences")]
    public HashSet<ProtoId<TraitPrototype>> TraitPreferences = new();

    [DataField("_loadouts")]
    public Dictionary<string, RoleLoadout> Loadouts = new();

    [DataField]
    public string Name;

    [DataField]
    public string FlavorText;

    // Corvax-Wega-Graphomancy-Extended-start
    [DataField]
    public string OOCFlavorText = string.Empty;

    [DataField]
    public string CharacterFlavorText = string.Empty;

    [DataField]
    public string GreenFlavorText = string.Empty;

    [DataField]
    public string YellowFlavorText = string.Empty;

    [DataField]
    public string RedFlavorText = string.Empty;

    [DataField]
    public string TagsFlavorText = string.Empty;

    [DataField]
    public string LinksFlavorText = string.Empty;

    [DataField]
    public string NSFWFlavorText = string.Empty;
    // Corvax-Wega-Graphomancy-Extended-end

    [DataField]
    public ProtoId<SpeciesPrototype> Species;

    [DataField] //Corvax-TTS
    public string Voice = HumanoidProfileSystem.DefaultVoice;

    [DataField] // Corvax-Wega-Barks
    public string BarkVoice = HumanoidProfileSystem.DefaultBarkVoice;

    [DataField]
    public int Age;

    [DataField]
    public Sex Sex;

    [DataField]
    public Gender Gender;

    // Corvax-Wega-start
    [DataField]
    public Status Status = Status.No;

    [DataField]
    public float Height = 175.0f;
    // Corvax-Wega-end

    [DataField]
    public HumanoidCharacterAppearanceV1 Appearance;

    [DataField]
    public SpawnPriorityPreference SpawnPriority;

    [DataField]
    public PreferenceUnavailableMode PreferenceUnavailable;

    public HumanoidCharacterProfile ToV2()
    {
        // Corvax-Wega-Idk: Let it be so because it's a dummy
        return new(
            Name,
            FlavorText,
            OOCFlavorText,
            CharacterFlavorText,
            GreenFlavorText,
            YellowFlavorText,
            RedFlavorText,
            TagsFlavorText,
            LinksFlavorText,
            NSFWFlavorText,
            Species,
            BarkVoice,
            Voice,
            Age,
            Sex,
            Gender,
            Status,
            Height,
            Appearance.ToV2(Species),
            SpawnPriority,
            JobPriorities,
            PreferenceUnavailable,
            AntagPreferences,
            TraitPreferences,
            Loadouts);
    }
}


[DataDefinition, Serializable]
public sealed partial class HumanoidCharacterAppearanceV1
{
    [DataField("hair")]
    public string HairStyleId;

    [DataField]
    public Color HairColor;

    [DataField("facialHair")]
    public string FacialHairStyleId;

    [DataField]
    public Color FacialHairColor;

    [DataField]
    public Color EyeColor;

    [DataField]
    public Color SkinColor;

    [DataField]
    public List<Marking> Markings = new();

    public HumanoidCharacterAppearance ToV2(ProtoId<SpeciesPrototype> species)
    {
        var markingManager = IoCManager.Resolve<MarkingManager>();

        var incomingMarkings = Markings.ShallowClone();
        if (HairStyleId != string.Empty)
            incomingMarkings.Add(new(HairStyleId, new List<Color>() { HairColor }));
        if (FacialHairStyleId != string.Empty)
            incomingMarkings.Add(new(FacialHairStyleId, new List<Color>() { FacialHairColor }));

        return new HumanoidCharacterAppearance(EyeColor, SkinColor, markingManager.ConvertMarkings(incomingMarkings, species));
    }
}
