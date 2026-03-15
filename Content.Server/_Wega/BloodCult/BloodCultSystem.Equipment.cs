using Content.Shared.Inventory.Events;
using Content.Shared.Hands;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Content.Shared.Blood.Cult.Components;

namespace Content.Server.Blood.Cult;

public sealed partial class BloodCultSystem
{
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    private void InitializeEquipment()
    {
        SubscribeLocalEvent<BloodCultEquipmentComponent, GotEquippedEvent>(OnDidEquip);
        SubscribeLocalEvent<BloodCultEquipmentComponent, BeforeGettingEquippedHandEvent>(OnHandPickUp);
    }

    private void OnDidEquip(Entity<BloodCultEquipmentComponent> ent, ref GotEquippedEvent args)
    {

        if (HasComp<BloodCultistComponent>(args.Equipee))
            return;

        _transform.SetCoordinates(ent, Transform(args.Equipee).Coordinates);
        _transform.AttachToGridOrMap(ent);
        _throwing.TryThrow(ent, _random.NextVector2(), 1);
        _popup.PopupEntity(Loc.GetString("blood-cult-on-equip"),
            args.Equipee,
            args.Equipee,
            PopupType.MediumCaution);
    }

    private void OnHandPickUp(Entity<BloodCultEquipmentComponent> ent, ref BeforeGettingEquippedHandEvent args)
    {
        if (args.Cancelled)
            return;

        if (HasComp<BloodCultistComponent>(args.User))
            return;

        args.Cancelled = true;

        _transform.SetCoordinates(ent, Transform(args.User).Coordinates);
        _transform.AttachToGridOrMap(ent);
        _throwing.TryThrow(ent, _random.NextVector2(), 1);
        _popup.PopupEntity(Loc.GetString("blood-cult-on-equip-hand"),
            args.User,
            args.User,
            PopupType.MediumCaution);
    }
}