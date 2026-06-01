using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Cargo;

/// <summary>
/// Added to the ninja while gloves are active. Lets the ninja hack a cargo order console
/// to drain 75% of the cargo account into cash at their feet.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedCargoHackerSystem))]
public sealed partial class CargoHackerComponent : Component
{
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(15);

    [DataField]
    public float StealFraction = 0.75f;
}

[Serializable, NetSerializable]
public sealed partial class CargoHackDoAfterEvent : SimpleDoAfterEvent;
