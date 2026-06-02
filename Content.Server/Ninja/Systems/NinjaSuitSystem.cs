using Content.Server.Ninja.Events;
using Content.Shared.Actions;
using Content.Shared.Clothing.Components;
using Content.Shared.Emp;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Ninja.Components;
using Content.Shared.Ninja.Systems;
using Content.Shared.Power.Components;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Robust.Shared.Containers;

namespace Content.Server.Ninja.Systems;

/// <summary>
/// Handles power cell upgrading and actions.
/// TODO: Move all of this to shared and predict it
/// </summary>
public sealed partial class NinjaSuitSystem : SharedNinjaSuitSystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedEmpSystem _emp = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private NinjaCloakSystem _cloak = default!;
    [Dependency] private SpaceNinjaSystem _ninja = default!;
    [Dependency] private PowerCellSystem _powerCell = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private NinjaWidowArtSystem _widowArt = default!;
    [Dependency] private NinjaCloningSystem _ninjaCloning = default!;

    // How much the cell score should be increased per 1 AutoRechargeRate.
    private const int AutoRechargeValue = 100;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NinjaSuitComponent, ContainerIsInsertingAttemptEvent>(OnSuitInsertAttempt);
        SubscribeLocalEvent<NinjaSuitComponent, RecallKatanaEvent>(OnRecallKatana);
        SubscribeLocalEvent<NinjaSuitComponent, NinjaEmpEvent>(OnEmp);
    }

    protected override void NinjaEquipped(Entity<NinjaSuitComponent> ent, Entity<SpaceNinjaComponent> user)
    {
        base.NinjaEquipped(ent, user);

        // raise event to let ninja components get starting battery
        _ninja.GetNinjaBattery(user.Owner, out var uid, out var _);

        if (uid is not { } battery_uid)
            return;

        var ev = new NinjaBatteryChangedEvent(battery_uid, ent.Owner);
        RaiseLocalEvent(ent, ref ev);
        RaiseLocalEvent(user, ref ev);
    }

    private void OnSuitInsertAttempt(EntityUid uid, NinjaSuitComponent comp, ContainerIsInsertingAttemptEvent args)
    {
        // this is for handling battery upgrading, not stopping actions from being added
        // if another container like ActionsContainer is specified, don't handle it
        if (TryComp<PowerCellSlotComponent>(uid, out var slot) && args.Container.ID != slot.CellSlotId)
            return;

        // no power cell for some reason??? allow it
        if (!_powerCell.TryGetBatteryFromSlot(uid, out var battery))
            return;

        if (!TryComp<BatteryComponent>(args.EntityUid, out var inserting))
        {
            args.Cancel();
            return;
        }

        var user = Transform(uid).ParentUid;

        // can only upgrade power cell, not swap to recharge instantly otherwise ninja could just swap batteries with flashlights in maints for easy power
        if (GetCellScore(args.EntityUid, inserting) <= GetCellScore(battery.Value, battery.Value))
        {
            args.Cancel();
            Popup.PopupEntity(Loc.GetString("ninja-cell-downgrade"), user, user);
            return;
        }

        // tell ninja abilities that use battery to update it so they don't use charge from the old one
        if (!_ninja.IsNinja(user))
            return;

        var ev = new NinjaBatteryChangedEvent(args.EntityUid, uid);
        RaiseLocalEvent(uid, ref ev);
        RaiseLocalEvent(user, ref ev);
    }

    // this function assigns a score to a power cell depending on the capacity, to be used when comparing which cell is better.
    private float GetCellScore(EntityUid uid, BatteryComponent battcomp)
    {
        // if a cell is able to automatically recharge, boost the score drastically depending on the recharge rate,
        // this is to ensure a ninja can still upgrade to a micro reactor cell even if they already have a medium or high.
        if (TryComp<BatterySelfRechargerComponent>(uid, out var selfcomp))
            return battcomp.MaxCharge + selfcomp.AutoRechargeRate * AutoRechargeValue;
        return battcomp.MaxCharge;
    }

    protected override void UserUnequippedSuit(Entity<NinjaSuitComponent> ent, Entity<SpaceNinjaComponent> user)
    {
        base.UserUnequippedSuit(ent, user);

    }

    private void OnRecallKatana(Entity<NinjaSuitComponent> ent, ref RecallKatanaEvent args)
    {
        var (uid, comp) = ent;
        var user = args.Performer;

        if (_cloak.TryRevealCloak(user))
            return;

        if (!_ninja.NinjaQuery.TryComp(user, out var ninja) || ninja.Katana == null)
            return;

        args.Handled = true;

        var katana = ninja.Katana.Value;
        var coords = _transform.GetWorldPosition(katana);
        var distance = (_transform.GetWorldPosition(user) - coords).Length();
        var chargeNeeded = distance * comp.RecallCharge;
        if (!_ninja.TryUseCharge(user, chargeNeeded))
        {
            Popup.PopupEntity(Loc.GetString("ninja-no-power"), user, user);
            return;
        }

        if (CheckDisabled(ent, user))
            return;

        // TODO: teleporting into belt slot
        var message = _hands.TryPickupAnyHand(user, katana)
            ? "ninja-katana-recalled"
            : "ninja-hands-full";
        Popup.PopupEntity(Loc.GetString(message), user, user);
    }

    private void OnEmp(Entity<NinjaSuitComponent> ent, ref NinjaEmpEvent args)
    {
        var (uid, comp) = ent;
        var user = args.Performer;

        if (_cloak.TryRevealCloak(user))
            return;

        args.Handled = true;
        if (!_ninja.TryUseCharge(user, comp.EmpCharge))
        {
            Popup.PopupEntity(Loc.GetString("ninja-no-power"), user, user);
            return;
        }

        if (CheckDisabled(ent, user))
            return;

        _emp.EmpPulse(Transform(user).Coordinates, comp.EmpRange, comp.EmpConsumption, comp.EmpDuration, user);
    }

    /// <summary>
    /// Grant the abilities chosen via SpiderOS to the ninja.
    /// Called from SpiderOSSystem when activation completes.
    /// </summary>
    public void GrantChosenAbilities(EntityUid suitUid, EntityUid user, int[] choices)
    {
        if (!TryComp<NinjaSuitComponent>(suitUid, out var comp))
            return;

        // Row 0: Smoke Screen (0), Chain Kunai (1), or Shurikens (2)
        if (choices[0] == 0)
            _actions.AddAction(user, ref comp.SmokeScreenActionEntity,       comp.SmokeScreenAction,       suitUid);
        else if (choices[0] == 1)
            _actions.AddAction(user, ref comp.ChainKunaiActionEntity,        comp.ChainKunaiAction,        suitUid);
        else if (choices[0] == 2)
        {
            // Throwing star action lives on ItemCreatorComponent; grant the pre-existing action entity
            if (TryComp<ItemCreatorComponent>(suitUid, out var creator) && creator.ActionEntity.HasValue)
                _actions.AddActionDirect(user, creator.ActionEntity.Value);
        }

        // Row 1: Phase Cloak (0), Healing Cocktail (1), or Adrenaline Burst (2)
        if (choices[1] == 0)
        {
            // Phase Cloak action lives on ToggleClothingComponent; grant the pre-existing action entity
            if (TryComp<ToggleClothingComponent>(suitUid, out var toggle) && toggle.ActionEntity.HasValue)
                _actions.AddActionDirect(user, toggle.ActionEntity.Value);
        }
        else if (choices[1] == 1)
            _actions.AddAction(user, ref comp.HealingCocktailActionEntity,   comp.HealingCocktailAction,   suitUid);
        else if (choices[1] == 2)
            _actions.AddAction(user, ref comp.AdrenalineBurstActionEntity,   comp.AdrenalineBurstAction,   suitUid);

        // Row 2: Energy Clones (0), Emergency Teleport (1), EMP (2)
        if (choices[2] == 0)
            _actions.AddAction(user, ref comp.EnergyClonesActionEntity,      comp.EnergyClonesAction,      suitUid);
        else if (choices[2] == 1)
            _actions.AddAction(user, ref comp.EmergencyTeleportActionEntity, comp.EmergencyTeleportAction, suitUid);
        else if (choices[2] == 2)
            _actions.AddAction(user, ref comp.EmpActionEntity,               comp.EmpAction,               suitUid);

        // Row 3: Chameleon (col 0), Caltrop (col 1), or Energy Net (col 2)
        if (choices.Length > 3 && choices[3] == 1)
            _actions.AddAction(user, ref comp.CaltropActionEntity, comp.CaltropAction, suitUid);
        else if (choices.Length > 3 && choices[3] == 0)
        {
            var chameleon = EnsureComp<NinjaChameleonComponent>(suitUid);
            // Suit is already equipped at this point, so ClothingGotEquippedEvent won't fire — set wearer manually
            chameleon.WearerEntity = user;
            _actions.AddAction(user, ref comp.ChameleonScannerActionEntity, comp.ChameleonScannerAction, suitUid);
        }
        else if (choices.Length > 3 && choices[3] == 2)
            _actions.AddAction(user, ref comp.EnergyNetActionEntity, comp.EnergyNetAction, suitUid);

        // Row 4: Spirit Form (0), Cloning (1), or Widow's Martial Art (2)
        if (choices.Length > 4 && choices[4] == 0)
            _actions.AddAction(user, ref comp.SpiritFormActionEntity, comp.SpiritFormAction, suitUid);
        else if (choices.Length > 4 && choices[4] == 1)
        {
            if (!HasComp<NinjaCloningComponent>(user))
            {
                var cloningComp = AddComp<NinjaCloningComponent>(user);
                cloningComp.SavedChoices = (int[]) choices.Clone();
                if (TryComp<SpiderOSComponent>(suitUid, out var spiderOS))
                {
                    cloningComp.SavedSuitColor = spiderOS.SuitColor;
                    cloningComp.SavedSuitGender = spiderOS.SuitGender;
                    cloningComp.SavedSuitStyleVariant = spiderOS.SuitStyleVariant;
                }
                // Bind the capsule on the same map so cloning respawns at the right planet
                _ninjaCloning.BindCapsule(user, cloningComp);
                Dirty(user, cloningComp);
            }
        }
        else if (choices.Length > 4 && choices[4] == 2)
            _widowArt.GrantWidowArt(suitUid, user);

        Dirty(suitUid, comp);
    }
}
