using Content.Shared.Damage.Systems;
using Content.Shared.Inventory.Events;
using Content.Shared.Lavaland.Artefacts.Components;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Robust.Shared.Timing;

namespace Content.Server.Lavaland.Artefacts.Systems;

public sealed partial class GodslayerArmourSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private DamageableSystem _damage = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GodslayerArmourComponent, GotEquippedEvent>(OnDidEquip);
        SubscribeLocalEvent<GodslayerArmourComponent, GotUnequippedEvent>(OnDidUnequip);
        SubscribeLocalEvent<GodslayerArmourAffectedComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnDidEquip(Entity<GodslayerArmourComponent> ent, ref GotEquippedEvent args)
    {
        if (HasComp<GodslayerArmourAffectedComponent>(args.EquipTarget))
            return;

        EnsureComp<GodslayerArmourAffectedComponent>(args.EquipTarget).ArmourEntity = ent.Owner;
    }

    private void OnDidUnequip(Entity<GodslayerArmourComponent> ent, ref GotUnequippedEvent args)
    {
        if (!HasComp<GodslayerArmourAffectedComponent>(args.EquipTarget))
            return;

        RemComp<GodslayerArmourAffectedComponent>(args.EquipTarget);
    }

    private void OnMobStateChanged(Entity<GodslayerArmourAffectedComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Critical)
            return;

        if (!Exists(ent.Comp.ArmourEntity))
            return;

        if (!TryComp<GodslayerArmourComponent>(ent.Comp.ArmourEntity, out var armour))
            return;

        if (_timing.CurTime < armour.NextUseTime)
        {
            var remaining = (int)(armour.NextUseTime - _timing.CurTime).TotalSeconds;
            _popup.PopupEntity(Loc.GetString("godslayer-armour-cooldown", ("time", remaining)), ent.Owner, ent.Owner, PopupType.MediumCaution);
            return;
        }

        ResurrectPlayer(ent.Owner, armour);
    }

    private void ResurrectPlayer(EntityUid player, GodslayerArmourComponent armour)
    {
        armour.NextUseTime = _timing.CurTime + armour.CooldownTime;

        var healSpec = _damage.CreateWeightedHealFromGroups(player, armour.HealAmount);

        _damage.TryChangeDamage(player, healSpec, true, false);
        _popup.PopupEntity(Loc.GetString("godslayer-armour-revive"), player, player, PopupType.Large);
    }
}
