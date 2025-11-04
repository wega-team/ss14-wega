using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Voucher.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.Voucher;

public sealed partial class VoucherSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VoucherComponent, InteractUsingEvent>(OnInteractUsing);

        SubscribeLocalEvent<VoucherCardComponent, UseInHandEvent>(OnCardActivate);
        SubscribeLocalEvent<VoucherCardComponent, VoucherKitSelectedMessage>(OnKitSelected);
    }

    private void OnInteractUsing(Entity<VoucherComponent> entity, ref InteractUsingEvent args)
    {
        if (!TryComp<VoucherCardComponent>(args.Used, out var cardComp) || cardComp.CurrentKit == null)
        {
            _popup.PopupClient(Loc.GetString("voucher-need-card"), args.User, args.User);
            return;
        }

        if (entity.Comp.TypeVoucher != cardComp.TypeVoucher)
        {
            _popup.PopupClient(Loc.GetString("voucher-wrong-type"), args.User, args.User);
            return;
        }

        if (!_prototype.TryIndex(cardComp.CurrentKit.Value, out var kit))
            return;

        foreach (var itemId in kit.Items)
        {
            Spawn(itemId, Transform(entity).Coordinates);
        }

        QueueDel(args.Used);

        _popup.PopupClient(Loc.GetString("voucher-kit-activated", ("kitName", kit.Name)), entity, args.User);
        _audio.PlayPvs(entity.Comp.SoundVend, entity);
    }

    private void OnCardActivate(EntityUid uid, VoucherCardComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        OpenKitSelectionUI(uid, component, args.User);
        args.Handled = true;
    }

    private void OnKitSelected(EntityUid uid, VoucherCardComponent component, VoucherKitSelectedMessage args)
    {
        component.CurrentKit = args.KitId;

        _popup.PopupEntity(Loc.GetString("voucher-kit-selected", ("kitName", _prototype.Index(args.KitId).Name)), uid);
    }

    private void OpenKitSelectionUI(EntityUid card, VoucherCardComponent component, EntityUid user)
    {
        var availableKits = new List<ProtoId<VoucherKitPrototype>>();

        foreach (var kitId in component.Kits)
        {
            if (_prototype.TryIndex(kitId, out var kit))
                availableKits.Add(kit);
        }

        if (availableKits.Count > 0)
        {
            var state = new VoucherKitSelectionState(availableKits);

            _ui.OpenUi(card, VoucherUiKey.Key, user);
            _ui.SetUiState(card, VoucherUiKey.Key, state);
        }
    }
}
