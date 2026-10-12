using Content.Shared.Feroxi;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client._Wega.Feroxi;

public sealed partial class BloodSenseOverlaySystem : EntitySystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IOverlayManager _overlayManager = default!;

    private BloodSenseOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new BloodSenseOverlay();

        SubscribeLocalEvent<BloodSenseActiveComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<BloodSenseActiveComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<BloodSenseActiveComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<BloodSenseActiveComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlayManager.RemoveOverlay(_overlay);
    }

    private void OnStartup(Entity<BloodSenseActiveComponent> ent, ref ComponentStartup args)
    {
        if (ent.Owner == _player.LocalEntity)
            _overlayManager.AddOverlay(_overlay);
    }

    private void OnShutdown(Entity<BloodSenseActiveComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Owner == _player.LocalEntity)
            _overlayManager.RemoveOverlay(_overlay);
    }

    private void OnPlayerAttached(Entity<BloodSenseActiveComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        _overlayManager.AddOverlay(_overlay);
    }

    private void OnPlayerDetached(Entity<BloodSenseActiveComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        _overlayManager.RemoveOverlay(_overlay);
    }
}
