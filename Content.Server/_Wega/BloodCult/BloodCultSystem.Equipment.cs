using Content.Shared.Inventory.Events;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Content.Shared.Blood.Cult.Components;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Random;


namespace Content.Server.NullRod;

public sealed class BloodCultSystem : EntitySystem
{
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
	
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodCultEquipmentComponent, GotEquippedEvent>(OnDidEquip);
        SubscribeLocalEvent<BloodCultEquipmentComponent, BeforeGettingEquippedHandEvent>(OnHandPickUp);
		SubscribeLocalEvent<BloodCultWeaponComponent, MeleeHitEvent>(OnBloodCultMeleeHit);
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
	
	private void OnBloodCultMeleeHit(EntityUid uid, BloodCultWeaponComponent comp, MeleeHitEvent args)
	{
		if (!args.IsHit || args.HitEntities.Count == 0)
			return;

		if (args.HitEntities is not List<EntityUid> hitList)
			return;

		for (int i = hitList.Count - 1; i >= 0; i--)
		{
			var target = hitList[i];

			if (HasComp<BloodCultistComponent>(target))
				hitList.RemoveAt(i);
		}

		if (hitList.Count == 0)
			args.Handled = true;
	}
}
