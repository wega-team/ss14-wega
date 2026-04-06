using Content.Shared.Inventory.Events;
using Content.Shared.Hands;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Content.Shared.Blood.Cult.Components;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Veil.Cult.Components;
using Robust.Shared.Random;

namespace Content.Server.Blood.Cult;

public sealed partial class BloodCultSystem
{
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    private void InitializeEquipment()
    {
        SubscribeLocalEvent<CultEquipmentComponent, GotEquippedEvent>(OnDidEquip);
        SubscribeLocalEvent<CultEquipmentComponent, BeforeGettingEquippedHandEvent>(OnHandPickUp);
		SubscribeLocalEvent<CultWeaponComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnDidEquip(Entity<CultEquipmentComponent> ent, ref GotEquippedEvent args)
    {

        if (HasComp<BloodCultistComponent>(args.Equipee) || HasComp<VeilCultistComponent>(args.Equipee))
            return;

        _transform.SetCoordinates(ent, Transform(args.Equipee).Coordinates);
        _transform.AttachToGridOrMap(ent);
        _throwing.TryThrow(ent, _random.NextVector2(), 1);
        _popup.PopupEntity(Loc.GetString("blood-cult-on-equip"),
            args.Equipee,
            args.Equipee,
            PopupType.MediumCaution);
    }

    private void OnHandPickUp(Entity<CultEquipmentComponent> ent, ref BeforeGettingEquippedHandEvent args)
    {
        if (args.Cancelled)
            return;

        if (HasComp<BloodCultistComponent>(args.User) || HasComp<VeilCultistComponent>(args.User))
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
	
	private void OnMeleeHit(EntityUid uid, CultWeaponComponent comp, MeleeHitEvent args)
	{
		if (!args.IsHit || args.HitEntities.Count == 0)
			return;

		if (args.HitEntities is not List<EntityUid> hitList)
			return;

		for (int i = hitList.Count - 1; i >= 0; i--)
		{
			var target = hitList[i];

			if (HasComp<BloodCultistComponent>(target) || HasComp<VeilCultistComponent>(target))
				hitList.RemoveAt(i);
		}

		if (hitList.Count == 0)
			args.Handled = true;
	}
}

