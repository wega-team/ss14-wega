using Content.Shared.Clothing;
using Content.Shared.Inventory;
using Robust.Shared.GameStates;
using Robust.Shared.GameObjects;

namespace Content.Shared.Thermal.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ThermalVisionComponent : Component
    /// <summary>
    /// работает только для слота eyes, переключает Fov
    /// </summary>
{
    [DataField("fovMultiplier")]
    public float FovMultiplier = 1.5f;

    [DataField("drawFovDisabled")]
    public bool DrawFovDisabled = false;
}
