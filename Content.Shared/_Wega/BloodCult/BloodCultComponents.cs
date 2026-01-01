using Content.Shared.StatusIcon;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Blood.Cult.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class BloodCultistComponent : Component
{
    public bool BloodMagicActive = false;

    [DataField] public EntityUid? SelectedSpell { get; set; }

    [DataField] public List<EntityUid?> SelectedEmpoweringSpells = new();

    [DataField] public EntityUid? RecallDaggerActionEntity;

    [DataField] public EntityUid? RecallSpearAction { get; set; }

    [DataField] public EntityUid? RecallSpearActionEntity;

    [DataField]
    public int BloodCount = 5;

    public static readonly EntProtoId BloodMagic = "ActionBloodMagic";
    public static readonly EntProtoId RecallBloodDagger = "ActionRecallBloodDagger";
    public static readonly EntProtoId RecallBloodSpear = "RecallBloodCultSpear";

    [DataField("cultistStatusIcon")]
    public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "BloodCultistFaction";
}

[RegisterComponent]
public sealed partial class AutoCultistComponent : Component;

[RegisterComponent, NetworkedComponent]
public sealed partial class ShowCultistIconsComponent : Component;

[RegisterComponent]
public sealed partial class BloodCultObjectComponent : Component;

[RegisterComponent]
public sealed partial class BloodCultWeaponComponent : Component;

[RegisterComponent]
public sealed partial class BloodDaggerComponent : Component
{
    [DataField]
    public bool IsSharpered = false;
}

[RegisterComponent]
public sealed partial class BloodSpellComponent : Component
{
    [DataField(required: true)]
    public BloodCultSpell SpellType = default!;
}

[RegisterComponent]
public sealed partial class BloodRuneComponent : Component
{
    [DataField(required: true)]
    public BloodCultRune RuneType = default!;

    [DataField]
    public string Desc { get; private set; } = string.Empty;

    [ViewVariables(VVAccess.ReadOnly)]
    public string LocDesc => Loc.GetString(Desc);

    public bool IsActive = true;

    public bool BarrierActive = false;
}

[RegisterComponent]
public sealed partial class BloodRitualDimensionalRendingComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public TimeSpan ActivateTime = TimeSpan.Zero;

    public bool Activate = false;

    public float NextTimeTick { get; set; }

    [DataField("ritualMusic")]
    public SoundSpecifier RitualMusic = new SoundCollectionSpecifier("BloodCultMusic");

    public bool SoundPlayed;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class BloodStructureComponent : Component
{
    [DataField("structureGear")]
    public List<EntProtoId> StructureGear { get; private set; } = new();

    [ViewVariables(VVAccess.ReadOnly), DataField]
    public TimeSpan ActivateTime = TimeSpan.Zero;

    [DataField("fixture")]
    public string FixtureId = string.Empty;

    [DataField]
    public SoundSpecifier? Sound { get; private set; }

    public bool IsActive = true;
}

[RegisterComponent]
public sealed partial class BloodPylonComponent : Component
{
    public float NextTimeTick { get; set; }
}

[RegisterComponent]
public sealed partial class BloodShieldActivaebleComponent : Component
{
    public string CurrentSlot = "outerClothing";
}

[RegisterComponent]
public sealed partial class BloodOrbComponent : Component
{
    public int Blood = 0;
}

[RegisterComponent]
public sealed partial class StoneSoulComponent : Component
{
    [DataField("soulProto", required: true)]
    public EntProtoId SoulProto { get; set; } = default!;

    public EntityUid? SoulEntity;

    [ViewVariables]
    public ContainerSlot SoulContainer = default!;

    public bool IsSoulSummoned = false;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ConstructComponent : Component;

[RegisterComponent, NetworkedComponent]
public sealed partial class BloodCultConstructComponent : Component;

[RegisterComponent, NetworkedComponent]
public sealed partial class BloodCultGhostComponent : Component;

[RegisterComponent, NetworkedComponent]
public sealed partial class BloodShuttleCurseComponent : Component;

[RegisterComponent, NetworkedComponent]
public sealed partial class VeilShifterComponent : Component
{
    [DataField]
    public int ActivationsCount = 4;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class BloodSharpenerComponent : Component;

/// <summary>
/// Заглушка для логики
/// </summary>
[RegisterComponent]
public sealed partial class BloodCultistEyesComponent : Component;

[RegisterComponent, NetworkedComponent]
public sealed partial class BloodPentagramDisplayComponent : Component;
