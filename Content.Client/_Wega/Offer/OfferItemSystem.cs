using Content.Shared.Offer;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.Offer;

public sealed class OfferItemSystem : SharedOfferItemSystem
{
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    private OfferItemIndicatorsOverlay? _overlayInstance;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OfferGiverComponent, ComponentStartup>(OnGiverStartup);
        SubscribeLocalEvent<OfferGiverComponent, ComponentShutdown>(OnGiverShutdown);
        SubscribeLocalEvent<OfferGiverComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<OfferGiverComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<OfferGiverComponent, AfterAutoHandleStateEvent>(OnComponentStateUpdated);
    }

    private void OnGiverStartup(EntityUid uid, OfferGiverComponent component, ComponentStartup args)
    {
        UpdateOverlay(uid, component);
    }

    private void OnGiverShutdown(EntityUid uid, OfferGiverComponent component, ComponentShutdown args)
    {
        UpdateOverlay(uid, component);
    }

    private void OnPlayerAttached(EntityUid uid, OfferGiverComponent component, LocalPlayerAttachedEvent args)
    {
        UpdateOverlay(uid, component);
    }

    private void OnPlayerDetached(EntityUid uid, OfferGiverComponent component, LocalPlayerDetachedEvent args)
    {
        UpdateOverlay(uid, component);
    }

    private void OnComponentStateUpdated(EntityUid uid, OfferGiverComponent component, ref AfterAutoHandleStateEvent args)
    {
        UpdateOverlay(uid, component);
    }

    private void UpdateOverlay(EntityUid uid, OfferGiverComponent component)
    {
        if (uid != _player.LocalEntity)
            return;

        if (component.IsOffering)
        {
            if (_overlayInstance == null)
            {
                _overlayInstance = new();
                _overlay.AddOverlay(_overlayInstance);
            }
        }
        else
        {
            if (_overlayInstance != null)
            {
                _overlay.RemoveOverlay(_overlayInstance);
                _overlayInstance = null;
            }
        }
    }
}
