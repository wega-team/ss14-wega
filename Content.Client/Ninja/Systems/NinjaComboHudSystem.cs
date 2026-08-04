using Content.Client.Ninja.Overlays;
using Robust.Client.Graphics;

namespace Content.Client.Ninja.Systems;

/// <summary>
/// Registers <see cref="NinjaComboOverlay"/> once at startup.
/// The overlay's own Draw() checks every frame whether the local player
/// has NinjaConcentrationComponent with inputs to render.
/// </summary>
public sealed partial class NinjaComboHudSystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlayMan = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlayMan.AddOverlay(new NinjaComboOverlay());
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlayMan.RemoveOverlay<NinjaComboOverlay>();
    }
}
