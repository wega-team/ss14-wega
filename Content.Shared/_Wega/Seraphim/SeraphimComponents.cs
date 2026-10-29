using Content.Shared.EntityEffects;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Wega.Seraphim;

/// <summary>
/// Имплант Seraphim-II: даёт экшены, запрещает огнестрел.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SeraphimImplantComponent : Component
{
    [DataField]
    public List<EntProtoId> ActionIds = new()
    {
        "ActionSeraphimKnee",
        "ActionSeraphimFuckOff",
        "ActionSeraphimConcentrate",
        "ActionSeraphimOverdrive",
    };

    public List<EntityUid?> Actions = new();

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(15);

    [DataField]
    public ProtoId<EntityEffectPrototype> FinishEffects = "SeraphimImplantFinish";
}

//Маркинг
[RegisterComponent, NetworkedComponent]
public sealed partial class SeraphimMarkVisualComponent : Component;

/// <summary>
/// Заготовленный удар кулаком (колено / отброс).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SeraphimPreparedComponent : Component
{
    [DataField, AutoNetworkedField]
    public string Type = string.Empty;

    [DataField, AutoNetworkedField]
    public TimeSpan ExpireTime = TimeSpan.Zero;

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(15);
}

/// <summary>
/// Концентрация: рефлект клинка + пацифизм.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SeraphimConcentratingComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan EndTime = TimeSpan.Zero;

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(12);

    [DataField]
    public float BuffReflectProb = 0.55f;

    public EntityUid? Weapon;
    public float OldReflectProb;
    public bool AddedPacified;
}

/// <summary>
/// ЗАЖЕЧЬ ПО ПОЛНОЙ: ускорение и визуал.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SeraphimOverdriveComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan EndTime = TimeSpan.Zero;

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(25);

    [DataField, AutoNetworkedField]
    public float SpeedMultiplier = 1.18f;

    [DataField]
    public float AttackRateMultiplier = 1.3f;

    [DataField]
    public ProtoId<EntityEffectPrototype> StartEffects = "SeraphimOverdriveStart";

    [DataField]
    public ProtoId<EntityEffectPrototype> EndEffects = "SeraphimOverdriveEnd";
}

/// <summary>
/// Вялость после овердрайва.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SeraphimExhaustedComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan EndTime = TimeSpan.Zero;

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(20);

    [DataField, AutoNetworkedField]
    public float SpeedMultiplier = 0.8f;
}

/// <summary>
/// Замедление цели от удара в колено.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SeraphimSlowedComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan EndTime = TimeSpan.Zero;

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    public float SpeedMultiplier = 0.65f;
}

/// <summary>
/// Корпоративный клинок (маркер).
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CorporateBladeComponent : Component
{
    [DataField]
    public float ThrowSpeedMultiplier = 0.25f;
}

/// <summary>
/// Автоимплантер: разовая установка импланта.
/// </summary>
[RegisterComponent]
public sealed partial class SeraphimImplanterComponent : Component
{
    [DataField]
    public TimeSpan EffectInterval = TimeSpan.FromSeconds(8);

    [DataField]
    public SoundSpecifier? WorkSound = new SoundCollectionSpecifier("SeraphimSurgery");

    [DataField]
    public ProtoId<EntityEffectPrototype> TickBleed = "SeraphimImplantTickBleed";

    [DataField]
    public ProtoId<EntityEffectPrototype> TickDrugs = "SeraphimImplantTickDrugs";

    [DataField]
    public ProtoId<EntityEffectPrototype> TickPain = "SeraphimImplantTickPain";
}
