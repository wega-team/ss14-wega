using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Content.Shared.Actions.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Movement.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FlyAbilityComponent : Component
{
    [DataField("FlyAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? Action = "ActionSwitchHarpyFly";

    [DataField("ActionEntity")] public EntityUid? ActionEntity;

    [DataField, AutoNetworkedField]
    public float StaminaDamage = 8f;

    [DataField("sprintSpeedModifier")]
    public float SprintSpeedModifier = 0.15f;
    public float SprintSpeedCurrent = 1f;

    [DataField]
    public float Interval = 1f;

    [DataField]
    public TimeSpan NextTickTime;

    [DataField("sound"), AutoNetworkedField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/_Wega/Effects/wing_flap.ogg");

    [DataField("volume")]
    public float SoundVolume = 3;

    [DataField("range")]
    public float SoundRange = 6f;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadOnly)]
    public bool Active = false;

    [DataField("cooldown")]
    public double Cooldown = 45.0;
    public TimeSpan CooldownDelay => TimeSpan.FromSeconds(Cooldown);
}

public sealed partial class FlyAbilityEvent : InstantActionEvent;

[ByRefEvent]
public readonly record struct SwitchFlyAbility(Entity<ActionComponent> Action, bool Toggled);
