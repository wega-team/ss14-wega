using Content.Shared.Alert;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Input;
using Content.Shared.Interaction.Components;
using Content.Shared.Popups;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
using Robust.Shared.Serialization;

namespace Content.Shared.Offer;

public abstract partial class SharedOfferItemSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OfferGiverComponent, MoveEvent>(OnGiverMoved);
        SubscribeLocalEvent<OfferGiverComponent, DidUnequipHandEvent>(OnGiverItemUnequipped);
        SubscribeLocalEvent<OfferGiverComponent, EntityTerminatingEvent>(OnGiverTerminating);

        SubscribeLocalEvent<OfferReceiverComponent, MoveEvent>(OnReceiverMoved);
        SubscribeLocalEvent<OfferReceiverComponent, EntityTerminatingEvent>(OnReceiverTerminating);
        SubscribeLocalEvent<OfferReceiverComponent, ComponentShutdown>(OnReceiverShutdown);

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.OfferItem, InputCmdHandler.FromDelegate(OnOfferItemCommand))
            .Register<SharedOfferItemSystem>();
    }

    private void OnOfferItemCommand(ICommonSession? session)
    {
        if (session?.AttachedEntity is not { } player)
            return;

        RaiseNetworkEvent(new RequestToggleOfferEvent(GetNetEntity(player)));
    }

    public bool TryToggleOfferMode(EntityUid uid, OfferGiverComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return false;

        if (component.IsOffering || component.Target != null)
        {
            _popup.PopupEntity(Loc.GetString("offer-cancel-offer"), uid, uid);
            CancelOffer(uid, component);
            return true;
        }

        if (!_hands.TryGetActiveItem(uid, out var item))
        {
            _popup.PopupEntity(Loc.GetString("offer-item-empty-hand"), uid, uid);
            return false;
        }

        // You will not be able to transfer such items.
        if (HasComp<UnremoveableComponent>(item) || HasComp<DeleteOnDropComponent>(item))
            return false;

        component.IsOffering = true;
        component.Item = item;
        Dirty(uid, component);

        return true;
    }

    public void CancelOffer(EntityUid uid, OfferGiverComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        component.IsOffering = false;
        component.Item = null;

        if (component.Target is { } target && HasComp<OfferReceiverComponent>(target))
            RemoveReceiverComponent(target, component);

        Dirty(uid, component);
    }

    private void RemoveReceiverComponent(EntityUid uid, OfferGiverComponent component)
    {
        component.Target = null;
        RemCompDeferred<OfferReceiverComponent>(uid);
    }

    // TODO: Плохо что оно постоянно вызывается, возможно стоит переделеать в будущем.
    private void OnGiverMoved(EntityUid uid, OfferGiverComponent component, MoveEvent args)
    {
        if (component.Target != null && !_transform.InRange(uid, component.Target.Value, component.MaxOfferDistance))
            CancelOffer(uid, component);
    }

    private void OnGiverItemUnequipped(EntityUid uid, OfferGiverComponent component, DidUnequipHandEvent args)
    {
        if (args.Unequipped == component.Item)
            CancelOffer(uid, component);
    }

    private void OnGiverTerminating(EntityUid uid, OfferGiverComponent component, ref EntityTerminatingEvent args)
    {
        CancelOffer(uid, component);
    }

    private void OnReceiverMoved(EntityUid uid, OfferReceiverComponent component, MoveEvent args)
    {
        if (component.Offerer != null && TryComp<OfferGiverComponent>(component.Offerer, out var giver)
            && !_transform.InRange(uid, component.Offerer.Value, giver.MaxOfferDistance))
            CancelOffer(component.Offerer.Value, giver);
    }

    private void OnReceiverTerminating(EntityUid uid, OfferReceiverComponent component, ref EntityTerminatingEvent args)
    {
        if (component.Offerer is { } offerer && TryComp<OfferGiverComponent>(offerer, out var giver))
            CancelOffer(offerer, giver);
    }

    private void OnReceiverShutdown(EntityUid uid, OfferReceiverComponent component, ref ComponentShutdown args)
    {
        if (component.Offerer is { } offerer && TryComp<OfferGiverComponent>(offerer, out var giver))
            CancelOffer(offerer, giver);

        _alerts.ClearAlert(uid, component.Alert);
    }
}

[Serializable, NetSerializable]
public sealed class RequestToggleOfferEvent : EntityEventArgs
{
    public NetEntity Player { get; }

    public RequestToggleOfferEvent(NetEntity player)
    {
        Player = player;
    }
}

public sealed partial class AcceptOfferAlertEvent : BaseAlertEvent;
