using Content.Shared.Clothing;
using Content.Shared.Inventory;
using Robust.Shared.GameStates;
using Robust.Shared.GameObjects;

namespace Content.Shared.Thermal.Components;

/// <summary>
///     работает только для слота eyes, переключает Fov
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ThermalVisionComponent : Component
{
    [DataField("drawFov")]
    public bool DrawFovDisabled = false;
}
