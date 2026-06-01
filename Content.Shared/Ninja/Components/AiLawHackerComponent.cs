using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Ninja.Components;

/// <summary>
/// Added to the ninja when the AI sabotage objective is active.
/// Lets the ninja hack an AI upload console to replace the AI laws with ion storm laws.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AiLawHackerComponent : Component
{
    /// <summary>
    /// How long the hack takes.
    /// </summary>
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(30);

    /// <summary>
    /// How many random ion laws to generate and apply.
    /// </summary>
    [DataField]
    public int IonLawCount = 3;
}

/// <summary>
/// DoAfter event for AI upload console hacking.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class AiLawHackDoAfterEvent : SimpleDoAfterEvent { }
