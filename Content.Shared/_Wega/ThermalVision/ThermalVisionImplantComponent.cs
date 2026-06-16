using Robust.Shared.GameStates;

namespace Content.Shared._Wega.ThermalVision;

/// <summary>
/// Marks a subdermal implant that grants its holder <see cref="ThermalVisionComponent"/>
/// while implanted.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ThermalVisionImplantComponent : Component;
