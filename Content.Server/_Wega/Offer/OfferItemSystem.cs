using Content.Shared.Alert;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Offer;
using Content.Shared.Popups;

namespace Content.Server.Offer;

public sealed class OfferItemSystem : SharedOfferItemSystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<RequestToggleOfferEvent>(OnRequestToggleOffer);

        SubscribeLocalEvent<OfferGiverComponent, InteractUsingEvent>(OnGiverInteractUsing);
        SubscribeLocalEvent<OfferReceiverComponent, AcceptOfferAlertEvent>(OnReceiverAlertAcceptOffer);
    }

    private void OnRequestToggleOffer(RequestToggleOfferEvent msg)
    {
        TryToggleOfferMode(GetEntity(msg.Player));
    }

    private void OnGiverInteractUsing(Entity<OfferGiverComponent> ent, ref InteractUsingEvent args)
    {
        if (!TryComp<OfferGiverComponent>(args.User, out var offer))
            return;

        if (!offer.IsOffering || args.User == args.Target || !HasComp<HandsComponent>(args.Target))
            return;

        if (!_transform.InRange(args.User, args.Target, offer.MaxOfferDistance))
            return;

        args.Handled = true;

        offer.Target = args.Target;
        offer.IsOffering = false;
        Dirty(args.User, offer);

        var receiver = EnsureComp<OfferReceiverComponent>(args.Target);
        receiver.Offerer = args.User;
        receiver.Item = offer.Item;
        Dirty(args.Target, receiver);

        _alerts.ShowAlert(args.Target, receiver.Alert);

        if (offer.Item is { } item)
        {
            _popup.PopupEntity(Loc.GetString("offer-item-try-give",
                ("item", Name(item)), ("target", Identity.Name(args.Target, EntityManager, args.User))), args.User, args.User);

            _popup.PopupEntity(Loc.GetString("offer-item-try-give-target",
                ("user", Identity.Name(args.User, EntityManager, args.Target)), ("item", Name(item))), args.Target, args.Target);
        }
    }

    private void OnReceiverAlertAcceptOffer(EntityUid uid, OfferReceiverComponent component, AcceptOfferAlertEvent args)
    {
        if (args.AlertId != component.Alert || component.Offerer is not { } offerer)
            return;

        TryAcceptOffer(uid, offerer);
    }

    public bool TryAcceptOffer(EntityUid acceptor, EntityUid offerer)
    {
        if (!TryComp<OfferGiverComponent>(offerer, out var offerComp) || !HasComp<HandsComponent>(acceptor))
            return false;

        if (_hands.GetEmptyHandCount(acceptor) == 0)
        {
            _popup.PopupEntity(Loc.GetString("offer-item-full-hand"), acceptor, acceptor);
            CancelOffer(offerer, offerComp);
            return false;
        }

        if (offerComp.Item is { } item)
        {
            if (_hands.TryPickupAnyHand(acceptor, item))
            {
                _popup.PopupEntity(Loc.GetString("offer-item-give",
                    ("item", Name(item)), ("target", Identity.Name(acceptor, EntityManager, offerer))), offerer, offerer);

                _popup.PopupEntity(Loc.GetString("offer-item-give-target",
                    ("item", Name(item)), ("target", Identity.Name(offerer, EntityManager, acceptor))), acceptor, acceptor);
            }
            else
            {
                _popup.PopupEntity(Loc.GetString("offer-item-no-give",
                    ("item", Name(item)), ("target", Identity.Name(acceptor, EntityManager, offerer))), offerer, offerer);
            }
        }

        CancelOffer(offerer, offerComp);

        return true;
    }
}
