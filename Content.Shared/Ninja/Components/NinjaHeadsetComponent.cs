using Robust.Shared.GameStates;

namespace Content.Shared.Ninja.Components;

/// <summary>
/// Marks the ninja headset. Allows copying encryption channels from other headsets on click.
/// Keys inside cannot be inserted or removed through normal means.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NinjaHeadsetComponent : Component;
