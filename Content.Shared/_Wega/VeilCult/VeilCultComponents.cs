using Content.Shared.Mind;
using Content.Shared.StatusIcon;
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

[RegisterComponent]
public sealed partial class VeilCultBeaconComponent : Component
{
    public float NextTimeTick { get; set; }
}

[RegisterComponent, NetworkedComponent]
public sealed partial class AutoVeilCultistComponent : Component;

[RegisterComponent, NetworkedComponent]
public sealed partial class InteractionCogInfectedComponent : Component
{
	public float PowerRate = 25000f;
	
	[DataField("drainSound")]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_Wega/Items/Specific/interaction_cog_drain.ogg");
	
    public float NextTimeTick { get; set; }
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

/// <summary>
/// Заглушка для логики
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class VeilCultistHandsComponent : Component;

[RegisterComponent, NetworkedComponent]
public sealed partial class EnchantedComponent : Component;

[RegisterComponent, NetworkedComponent]
public sealed partial class VeilCogDisplayComponent : Component;