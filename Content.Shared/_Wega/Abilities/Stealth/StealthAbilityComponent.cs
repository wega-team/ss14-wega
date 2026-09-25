using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Stealth.Components;

/// <summary>
/// A component for configuring the settings for the jump action.
/// To give the jump action to an entity use <see cref="ActionGrantComponent"/> and <see cref="ItemActionGrantComponent"/>.
/// The basic action prototype is "ActionGravityJump".
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedStealthAbilitySystem))]
public sealed partial class StealthAbilityComponent : Component
{
    /// <summary>
    /// The action prototype that allows you to jump.
    /// </summary>
    [DataField]
    public EntProtoId Action = "ActionStealthCyborg";

    /// <summary>
    /// Entity to hold the action prototype.
    /// </summary>
    public EntityUid? ActionEntity;
	
	// Время действия стелса, я молю не ставьте его больше, чем 
    [DataField, AutoNetworkedField]
    public TimeSpan Time = TimeSpan.FromSeconds(5);
	
	// параметр SetVisibility
    [DataField, AutoNetworkedField]
    public float StealthСoefficient = 0.1f;
}

public sealed partial class SteathAbilityEvent : InstantActionEvent;