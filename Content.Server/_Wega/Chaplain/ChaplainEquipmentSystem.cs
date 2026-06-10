using Content.Shared.Inventory.Events;
using Content.Shared.Hands;
using Content.Shared._Wega.Chaplain.Components;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Robust.Shared.Random;
using Content.Server.Bible.Components;

namespace Content.Server._Wega.Chaplain;

public sealed partial class ChaplainEquipmentSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChaplainItemComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<ChaplainItemComponent, BeforeGettingEquippedHandEvent>(OnPickUp);
    }

    private void OnEquipped(Entity<ChaplainItemComponent> ent, ref GotEquippedEvent args)
    {
        var target = args.EquipTarget;

        if (HasComp<BibleUserComponent>(target))
            return;

        _transform.SetCoordinates(ent, Transform(target).Coordinates);
        _transform.AttachToGridOrMap(ent);
        _throwing.TryThrow(ent, _random.NextVector2(), 1);
        _popup.PopupEntity(Loc.GetString("chaplain-pickup-fail"),
            target, target, PopupType.MediumCaution);
    }

    private void OnPickUp(Entity<ChaplainItemComponent> ent, ref BeforeGettingEquippedHandEvent args)
    {
        if (args.Cancelled)
            return;

        var user = args.User;

        if (HasComp<BibleUserComponent>(user))
            return;

        args.Cancelled = true;
        _transform.SetCoordinates(ent, Transform(user).Coordinates);
        _transform.AttachToGridOrMap(ent);
        _throwing.TryThrow(ent, _random.NextVector2(), 1);
        _popup.PopupEntity(Loc.GetString("chaplain-pickup-fail"),
            user, user, PopupType.MediumCaution);
    }
}