using Content.Shared.StatusIcon;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Blood.Cult.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BloodCultistComponent : Component
{
    public bool BloodMagicActive = false;

    public EntityUid? SelectedSpell { get; set; }

    public List<EntityUid?> SelectedEmpoweringSpells = new();

    [DataField, AutoNetworkedField]
    public EntityUid? RecallDaggerActionEntity;

    public EntityUid? RecallSpearAction { get; set; }

    [DataField, AutoNetworkedField]
    public EntityUid? RecallSpearActionEntity;

    [DataField]
    public int BloodCount = 5;

    [DataField]
    public int Empowering = 0;

    public static readonly EntProtoId CultObjective = "ActionBloodCultObjective";
    public static readonly EntProtoId CultCommunication = "ActionBloodCultComms";
    public static readonly EntProtoId BloodMagic = "ActionBloodMagic";
    public static readonly EntProtoId RecallBloodDagger = "ActionRecallBloodDagger";
    public static readonly EntProtoId RecallBloodSpear = "RecallBloodCultSpear";

    [DataField("cultistStatusIcon")]
    public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "BloodCultistFaction";
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ShowCultistIconsComponent : Component;

[RegisterComponent]
public sealed partial class AutoCultistComponent : Component;

[RegisterComponent]
public sealed partial class BloodCultObjectComponent : Component;

[RegisterComponent]
public sealed partial class BloodDaggerComponent : Component
{
    [DataField]
    public bool IsSharpered = false;
}

[RegisterComponent]
public sealed partial class BloodSpellComponent : Component
{
    [DataField]
    public List<string> Prototype = new();
}

[RegisterComponent]
public sealed partial class BloodRuneComponent : Component
{
    [DataField]
    public string Prototype = default!;

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
}

[RegisterComponent, NetworkedComponent]
public sealed partial class BloodStructureComponent : Component
{
    [DataField("structureGear")]
    public List<string> StructureGear { get; private set; } = new();

    [ViewVariables(VVAccess.ReadOnly), DataField]
    public TimeSpan ActivateTime = TimeSpan.Zero;

    [DataField("fixture")]
    public string FixtureId = string.Empty;

    [DataField]
    public SoundSpecifier? Sound { get; private set; }

    [DataField]
    public bool CanInteract = true;

    public bool IsActive = true;
}

[RegisterComponent]
public sealed partial class BloodPylonComponent : Component
{
    public float NextTimeTick { get; set; }
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
    public string SoulProto { get; set; } = default!;

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
public sealed partial class CultistEyesComponent : Component;

[RegisterComponent, NetworkedComponent]
public sealed partial class PentagramDisplayComponent : Component;

[Serializable, NetSerializable]
public enum RuneColorVisuals
{
    Color
}

[Serializable, NetSerializable]
public enum StoneSoulVisualLayers : byte
{
    Base,
    Soul
}

[Serializable, NetSerializable]
public enum StoneSoulVisuals : byte
{
    HasSoul
}
