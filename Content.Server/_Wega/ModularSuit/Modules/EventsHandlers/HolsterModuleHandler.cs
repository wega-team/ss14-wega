using Content.Shared.Actions;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Modular.Suit;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Whitelist;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;

namespace Content.Server.Modular.Suit;

public sealed class HolsterModuleHandler : ModuleActionHandler
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedWieldableSystem _wieldable = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ModularSuitActionHolderComponent, ToggleHolsterModuleEvent>(OnToggle);
    }

    private void OnToggle(Entity<ModularSuitActionHolderComponent> ent, ref ToggleHolsterModuleEvent args)
    {
        if (args.Handled)
            return;

        if (!TryFindModuleByAction(ent, args.Action, out var moduleEnt))
            return;

        if (!TryComp<ModularSuitModuleComponent>(moduleEnt, out var moduleComp) || !moduleComp.IsActive)
            return;

        var holster = Container.GetContainer(moduleEnt.Value, args.TargetContainerId);
        if (holster == null)
            return;

        var user = args.Performer;
        if (holster.ContainedEntities.Count > 0)
        {
            var item = holster.ContainedEntities[0];
            if (_hands.TryPickup(user, item))
            {
                if (TryComp<WieldableComponent>(item, out var wieldable))
                    _wieldable.TryWield(item, wieldable, user);

                if (TryComp<ChamberMagazineAmmoProviderComponent>(item, out var chamber)
                    && chamber.BoltClosed != null && !chamber.BoltClosed.Value)
                    _gun.SetBoltClosed(item, chamber, true);

                Audio.PlayPvs(args.EjectSound, ent.Owner);
                ModularSuit.UseCoreCharge(ent.Owner, moduleComp.PowerInstanceUsage);
            }
        }
        else
        {
            if (!_itemSlots.TryGetSlot(moduleEnt.Value, args.TargetContainerId, out var slot))
                return;

            var item = _hands.GetActiveItem(user);
            if (item == null)
            {
                Popup.PopupEntity(Loc.GetString("modsuit-holster-empty-hand"), ent.Owner, user);
                return;
            }

            if (_whitelist.IsWhitelistFail(slot.Whitelist, item.Value) || _whitelist.IsWhitelistPass(slot.Blacklist, item.Value)
                || !Container.Insert(item.Value, holster))
            {
                Popup.PopupEntity(Loc.GetString("modsuit-holster-cant-holster"), ent.Owner, user);
            }
            else
            {
                Audio.PlayPvs(args.InsertSound, ent.Owner);
                ModularSuit.UseCoreCharge(ent.Owner, moduleComp.PowerInstanceUsage);
            }
        }

        args.Handled = true;
        _actions.SetToggled(args.Action.Owner, holster.ContainedEntities.Count > 0);
    }
}
