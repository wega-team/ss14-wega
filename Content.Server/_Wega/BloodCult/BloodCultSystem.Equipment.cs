using Content.Shared.Inventory.Events;
using Content.Shared.Hands;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Content.Shared.Blood.Cult.Components;
using Content.Shared.Blood.Cult;
using Content.Shared.Veil.Cult.Components;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Melee;

namespace Content.Server.Blood.Cult;

public sealed partial class BloodCultSystem
{
    [Dependency] private ThrowingSystem _throwing = default!;

    private void InitializeEquipment()
    {
        SubscribeLocalEvent<CultEquipmentComponent, GotEquippedEvent>(OnDidEquip);
        SubscribeLocalEvent<CultEquipmentComponent, BeforeGettingEquippedHandEvent>(OnHandPickUp);
        SubscribeLocalEvent<MeleeWeaponComponent, MeleeHitEvent>(OnCultMeleeHit);
    }

    private void OnDidEquip(Entity<CultEquipmentComponent> ent, ref GotEquippedEvent args)
    {

        if (HasComp<BloodCultistComponent>(args.EquipTarget) && ent.Comp.Cult == CultType.Blood)
            return;

        if (HasComp<VeilCultistComponent>(args.EquipTarget) && ent.Comp.Cult == CultType.Veil)
            return;

        if (HasComp<AllowCultEquipmentComponent>(args.EquipTarget))
            return;

        _transform.SetCoordinates(ent, Transform(args.EquipTarget).Coordinates);
        _transform.AttachToGridOrMap(ent);
        _throwing.TryThrow(ent, _random.NextVector2(), 1);
        _popup.PopupEntity(Loc.GetString("blood-cult-on-equip"),
            args.EquipTarget, args.EquipTarget, PopupType.MediumCaution);
    }

    private void OnHandPickUp(Entity<CultEquipmentComponent> ent, ref BeforeGettingEquippedHandEvent args)
    {
        if (HasComp<BloodCultBolaComponent>(ent) &&
            TerminatingOrDeleted(ent))
            return;

        if (args.Cancelled)
            return;

        if (HasComp<BloodCultistComponent>(args.User) && ent.Comp.Cult == CultType.Blood)
            return;

        if (HasComp<VeilCultistComponent>(args.User) && ent.Comp.Cult == CultType.Veil)
            return;

        if (HasComp<AllowCultEquipmentComponent>(args.User))
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

    private void OnCultMeleeHit(EntityUid uid, MeleeWeaponComponent comp, MeleeHitEvent args)
    {
        if (!args.IsHit || args.HitEntities.Count == 0)
            return;

        if (args.HitEntities is not HashSet<EntityUid> hitSet)
            return;

        var bloodCultist = HasComp<BloodCultistComponent>(args.User);
        var veilCultist = HasComp<VeilCultistComponent>(args.User);

        if (!bloodCultist && !veilCultist)
            return;

        hitSet.RemoveWhere(target =>
            bloodCultist && HasComp<BloodCultistComponent>(target) ||
            veilCultist && HasComp<VeilCultistComponent>(target));

        if (hitSet.Count == 0)
            args.Handled = true;
    }
}
