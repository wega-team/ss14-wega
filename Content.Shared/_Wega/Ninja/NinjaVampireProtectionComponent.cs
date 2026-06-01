using Robust.Shared.GameStates;

namespace Content.Shared._Wega.Ninja;

/// <summary>Marker added to a ninja after successfully completing the collect-blood objective.</summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NinjaVampireProtectionComponent : Component;
