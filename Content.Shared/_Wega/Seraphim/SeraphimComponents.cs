using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

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

    [DataField, AutoNetworkedField]
    public float SpeedMultiplier = 1.18f;
}

/// <summary>
/// Вялость после овердрайва.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SeraphimExhaustedComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan EndTime = TimeSpan.Zero;

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

    [DataField, AutoNetworkedField]
    public float SpeedMultiplier = 0.65f;
}

/// <summary>
/// Корпоративный клинок (маркер).
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CorporateBladeComponent : Component;

/// <summary>
/// Автоимплантер: разовая установка импланта.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SeraphimImplanterComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Used;

    [DataField, AutoNetworkedField]
    public bool Active;

    [DataField, AutoNetworkedField]
    public TimeSpan EndTime = TimeSpan.Zero;

    public TimeSpan NextEffectTime = TimeSpan.Zero;

    public EntityUid? Target;

    public EntityUid? SoundStream;

    [NonSerialized]
    public DoAfterId? DoAfter;

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(60);

    [DataField]
    public TimeSpan EffectInterval = TimeSpan.FromSeconds(8);

    [DataField]
    public EntProtoId Implant = "SeraphimImplant";

    [DataField]
    public SoundSpecifier? WorkSound = new SoundCollectionSpecifier("SeraphimSurgery");
}

[Serializable, NetSerializable]
public enum SeraphimImplanterVisuals : byte
{
    Empty,
}
