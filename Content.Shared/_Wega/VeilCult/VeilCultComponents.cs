using Content.Shared.Mind;
using Content.Shared.StatusIcon;
using Content.Shared.Teleportation.Components;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Veil.Cult.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class VeilCultistComponent : Component
{
    public static readonly EntProtoId MidasTouch = "ActionMidasTouch";

    [DataField]
    public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "VeilCultistFaction";

    [DataField]
    public ProtoId<MindChannelPrototype> CultMindChannel { get; set; } = "MindVeilCult";
}

[RegisterComponent, NetworkedComponent]
public sealed partial class VeilCultConstructComponent : Component;

[RegisterComponent, NetworkedComponent]
public sealed partial class VeilCultAltarComponent : Component
{
    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_Wega/Effects/altar.ogg");
}

[RegisterComponent, NetworkedComponent]
public sealed partial class VeilCultStructureComponent : Component
{
    public bool IsActive = true;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class VeilCultBeaconComponent : Component
{
    public float NextTimeTick { get; set; } = 5;

    [ViewVariables(VVAccess.ReadWrite), Access(Other = AccessPermissions.ReadWriteExecute)]
    [DataField]
    public string AssignedName = string.Empty;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public int MaxNameChars = 15;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class AutoVeilCultistComponent : Component;

[RegisterComponent, NetworkedComponent]
public sealed partial class InteractionCogInfectedComponent : Component
{
    public float PowerRate = 25000f;

    [DataField("drainSound")]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_Wega/Effects/interaction_cog_drain.ogg");

    public float NextTimeTick { get; set; } = 5;
}

[RegisterComponent]
public sealed partial class EnchantableComponent : Component
{
    [DataField(required: true)]
    public List<EntProtoId> Enchants = new();

    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(5);

    [DataField]
    public float Cost = 100f;
}

[RegisterComponent]
public sealed partial class VeilCultPortalComponent : Component
{
    public float NextTimeTick { get; set; }

    [DataField]
    public SoundSpecifier RitualMusic = new SoundCollectionSpecifier("BloodCultMusic");

    public bool SoundPlayed;
}

/// <summary>
/// Заглушка для логики
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class VeilCultistHandsComponent : Component;

[RegisterComponent, NetworkedComponent]
public sealed partial class EnchantedComponent : Component;

[RegisterComponent, NetworkedComponent]
public sealed partial class VeilCogDisplayComponent : Component;

[RegisterComponent, NetworkedComponent]
public sealed partial class MidasHandComponent : Component;

[RegisterComponent, NetworkedComponent]
public sealed partial class StrangeShardComponent : Component;

[RegisterComponent, NetworkedComponent]
public sealed partial class CogscarabComponent : Component;

[RegisterComponent, NetworkedComponent]
public sealed partial class VeilCultLatheComponent : Component;

[RegisterComponent, NetworkedComponent]
public sealed partial class SoulVesselComponent : Component;

[RegisterComponent, NetworkedComponent]
public sealed partial class AbsorbedByVeilComponent : Component;

/// <summary>
/// Зачарования.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StunEnchantComponent : Component
{
    [DataField]
    public bool Knockout = false;
    
    [DataField]
    public TimeSpan StunTime = TimeSpan.FromSeconds(5);

    [DataField]
    public TimeSpan MuteTime = TimeSpan.FromSeconds(8);

    [DataField]
    public bool Mute = true;

    [DataField]
    public bool EmpBorgs = true;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ForcePassageEnchantComponent : Component
{
    [DataField]
    public EntProtoId? Proto; // for future
}

[RegisterComponent, NetworkedComponent]
public sealed partial class TerraformEnchantComponent : Component;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TeleportationEnchantComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<TeleportPoint> AvailableWarps = new();

    [DataField]
    public LocId Name = "teleportation-enchant-window-title";
}

[RegisterComponent, NetworkedComponent]
public sealed partial class SealWoundsEnchantComponent : Component;

[RegisterComponent, NetworkedComponent]
public sealed partial class HidingsClockEnchantComponent : Component
{
    [DataField]
    public int Uses = 2;

    [DataField]
    public float Radius = 5f;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ElectricalTouchEnchantComponent : Component
{
    [DataField]
    public int Uses = 3;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ConfusionEnchantComponent : Component
{
    [DataField]
    public TimeSpan Time = TimeSpan.FromSeconds(15);
}

[RegisterComponent, NetworkedComponent]
public sealed partial class CrusherEnchantComponent : Component;

[RegisterComponent, NetworkedComponent]
public sealed partial class DismantlingEnchantComponent : Component;

[RegisterComponent, NetworkedComponent]
public sealed partial class KnockbackEnchantComponent : Component
{
    [DataField]
    public int Uses = 3;

    [DataField]
    public float Distance = 3f;

    [DataField]
    public float Speed = 3f;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class SwordsmenEnchantComponent : Component
{
    [DataField]
    public float AttackRate = 4f;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class BloodshedEnchantComponent : Component;

[RegisterComponent, NetworkedComponent]
public sealed partial class HasteEnchantComponent : Component
{
    [DataField]
    public TimeSpan Time = TimeSpan.FromSeconds(8);

    [DataField]
    public float SprintModifier = 1.5f;

    [DataField]
    public float WalkModifier = 1.5f;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ReflectionEnchantComponent : Component
{
    [DataField]
    public int Uses = 4;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class CamouflageEnchantComponent : Component;

[RegisterComponent, NetworkedComponent]
public sealed partial class AbsorbEnchantComponent : Component;

[RegisterComponent, NetworkedComponent]
public sealed partial class SmokeEnchantComponent : Component;

[RegisterComponent, NetworkedComponent]
public sealed partial class HardenPlatesEnchantComponent : Component
{
    [DataField]
    public TimeSpan Time = TimeSpan.FromSeconds(8);
}

[RegisterComponent, NetworkedComponent]
public sealed partial class NorthStarEnchantComponent : Component
{
    [DataField]
    public float AttackRate = 4f;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class RedFlameEnchantComponent : Component
{
    [DataField]
    public TimeSpan Time = TimeSpan.FromSeconds(5);
}

[RegisterComponent, NetworkedComponent]
public sealed partial class TimeStopEnchantComponent : Component
{
    [DataField]
    public TimeSpan Time = TimeSpan.FromSeconds(7);
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ReconstructionEnchantComponent : Component
{
    [DataField]
    public float Radius = 4f;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class EmpEnchantComponent : Component
{
    [DataField]
    public float RadiusStrong = 4f;

    [DataField]
    public float RadiusWeak = 6f;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ConfusionComponent : Component;
