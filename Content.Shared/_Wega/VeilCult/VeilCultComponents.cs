using Content.Shared.Mind;
using Content.Shared.StatusIcon;
using Content.Shared.Teleportation.Components;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Veil.Cult.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class VeilCultistComponent : Component
{

    public static readonly EntProtoId MidasTouch = "ActionMidasTouch";

    [DataField("cultistStatusIcon")]
    public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "VeilCultistFaction";

    public ProtoId<MindChannelPrototype> CultMindChannel { get; set; } = "MindVeilCult";
}

[RegisterComponent]
public sealed partial class VeilRitualDimensionalRendingComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public TimeSpan ActivateTime = TimeSpan.Zero;

    public bool Activate = false;

    public float NextTimeTick { get; set; }

    [DataField("ritualMusic")]
    public SoundSpecifier RitualMusic = new SoundCollectionSpecifier("VeilCultMusic");

    public bool SoundPlayed;
}


[RegisterComponent, NetworkedComponent]
public sealed partial class VeilCultConstructComponent : Component;

[RegisterComponent, NetworkedComponent]
public sealed partial class VeilCultAltarComponent : Component
{
    [DataField("sound")]
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
    [DataField("enchants", required: true)]
    public List<EntProtoId> Enchants = new();
    
    [DataField("delay")]
    public TimeSpan Delay = TimeSpan.FromSeconds(5);
    
    [DataField("cost")]
    public float Cost = 100f;
    
}

[RegisterComponent]
public sealed partial class VeilCultPortalComponent : Component
{

    public float NextTimeTick { get; set; }

    [DataField("ritualMusic")]
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

/// <summary>
/// Зачарования.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class StunEnchantComponent : Component
{
    [DataField("stunTime")]
    public TimeSpan StunTime = TimeSpan.FromSeconds(5);
    
    [DataField("muteTime")]
    public TimeSpan MuteTime = TimeSpan.FromSeconds(10);
    
    [DataField("mute")]
    public bool Mute = true;
    
    [DataField("empBorgs")]
    public bool EmpBorgs = true;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ForcePassageEnchantComponent : Component
{
    [DataField("proto")]
    public EntProtoId Proto; // for future
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
    [DataField("uses")]
    public int Uses = 2;
    
    [DataField("radius")]
    public float Radius = 5f;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ElectricalTouchEnchantComponent : Component
{
    [DataField("uses")]
    public int Uses = 3;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ConfusionEnchantComponent : Component
{
    [DataField("time")]
    public TimeSpan Time = TimeSpan.FromSeconds(15);
}

[RegisterComponent, NetworkedComponent]
public sealed partial class CrusherEnchantComponent : Component;

[RegisterComponent, NetworkedComponent]
public sealed partial class KnockbackEnchantComponent : Component
{
    [DataField("uses")]
    public int Uses = 3;
    
    [DataField("distance")]
    public float Distance = 3f;
    
    [DataField("speed")]
    public float Speed = 3f;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class SwordsmenEnchantComponent : Component
{
    [DataField("attackRate")]
    public float AttackRate = 4f;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class BloodshedEnchantComponent : Component;

[RegisterComponent, NetworkedComponent]
public sealed partial class HasteEnchantComponent : Component
{
    [DataField("time")]
    public TimeSpan Time = TimeSpan.FromSeconds(8);
    
    [DataField("sprintModifier")]
    public float SprintModifier = 1.5f;
    
    [DataField("walkModifier")]
    public float WalkModifier = 1.5f;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ReflectionEnchantComponent : Component
{
    [DataField("uses")]
    public int Uses = 4;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class CamouflageEnchantComponent : Component;

[RegisterComponent, NetworkedComponent]
public sealed partial class AbsorbEnchantComponent : Component;

[RegisterComponent, NetworkedComponent]
public sealed partial class FlashEnchantComponent : Component;

[RegisterComponent, NetworkedComponent]
public sealed partial class HardenPlatesEnchantComponent : Component
{
    [DataField("time")]
    public TimeSpan Time = TimeSpan.FromSeconds(8);
}

[RegisterComponent, NetworkedComponent]
public sealed partial class NorthStarEnchantComponent : Component
{
    [DataField("attackRate")]
    public float AttackRate = 4f;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class RedFlameEnchantComponent : Component
{
    [DataField("time")]
    public TimeSpan Time = TimeSpan.FromSeconds(5);
}

[RegisterComponent, NetworkedComponent]
public sealed partial class TimeStopEnchantComponent : Component
{
    [DataField("time")]
    public TimeSpan Time = TimeSpan.FromSeconds(6);
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ReconstructionEnchantComponent : Component
{
    [DataField("radius")]
    public float Radius = 4f;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class EmpEnchantComponent : Component
{
    [DataField("radiusStrong")]
    public float RadiusStrong = 4f;
    
    [DataField("radiusWeak")]
    public float RadiusWeak = 6f;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ConfusionComponent : Component;

