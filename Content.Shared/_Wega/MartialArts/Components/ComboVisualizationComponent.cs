using Robust.Shared.GameStates;

namespace Content.Shared.Martial.Arts.Components;

/// <summary>
/// Tracks the current martial arts combo icon sequence for HUD display.
/// Added to entities with <see cref="MartialArtsComponent"/> when they activate actions.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class ComboVisualizationComponent : Component
{
    /// <summary>Icon state names from intents.rsi, accumulated as actions are pressed.</summary>
    [AutoNetworkedField, DataField]
    public List<string> Icons = new();

    /// <summary>Set to true when a blow lands; icons clear after <see cref="CompletedLingerSeconds"/> seconds.</summary>
    [AutoNetworkedField, DataField]
    public bool Completed = false;

    /// <summary>Server-only: when Completed was set, for the clear timer.</summary>
    public TimeSpan CompletedAt;

    public const float CompletedLingerSeconds = 1.0f;
}
