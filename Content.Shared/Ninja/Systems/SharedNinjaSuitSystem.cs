using Content.Shared.Actions;
using Content.Shared.Charges.Systems;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Emp;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory.Events;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Ninja.Components;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.Ninja.Systems;

/// <summary>
/// Handles (un)equipping and provides some API functions.
/// </summary>
public abstract class SharedNinjaSuitSystem : EntitySystem
{
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly SharedChargesSystem _charges = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;
    [Dependency] private readonly SharedSpaceNinjaSystem _ninja = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NinjaSuitComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<NinjaSuitComponent, ClothingGotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<NinjaSuitComponent, GetItemActionsEvent>(OnGetItemActions);
        SubscribeLocalEvent<NinjaSuitComponent, DashEvent>(OnDash);
        SubscribeLocalEvent<NinjaSuitComponent, ToggleClothingCheckEvent>(OnCloakCheck);
        SubscribeLocalEvent<NinjaSuitComponent, CheckItemCreatorEvent>(OnStarCheck);
        SubscribeLocalEvent<NinjaSuitComponent, CreateItemAttemptEvent>(OnCreateStarAttempt);
        SubscribeLocalEvent<NinjaSuitComponent, ItemToggleActivateAttemptEvent>(OnActivateAttempt);
        SubscribeLocalEvent<NinjaSuitComponent, GotUnequippedEvent>(OnUnequipped);
    }

    private void OnEquipped(Entity<NinjaSuitComponent> ent, ref ClothingGotEquippedEvent args)
    {
        var user = args.Wearer;
        if (_ninja.NinjaQuery.TryComp(user, out var ninja))
            NinjaEquipped(ent, (user, ninja));
    }

    protected virtual void NinjaEquipped(Entity<NinjaSuitComponent> ent, Entity<SpaceNinjaComponent> user)
    {
        // mark the user as wearing this suit, used when being attacked among other things
        _ninja.AssignSuit(user, ent);
    }

    private void OnMapInit(Entity<NinjaSuitComponent> ent, ref MapInitEvent args)
    {
        var (uid, comp) = ent;
        _actionContainer.EnsureAction(uid, ref comp.RecallKatanaActionEntity, comp.RecallKatanaAction);
        _actionContainer.EnsureAction(uid, ref comp.EmpActionEntity, comp.EmpAction);
        _actionContainer.EnsureAction(uid, ref comp.SpiritFormActionEntity, comp.SpiritFormAction);
        _actionContainer.EnsureAction(uid, ref comp.ChainKunaiActionEntity, comp.ChainKunaiAction);
        _actionContainer.EnsureAction(uid, ref comp.SmokeScreenActionEntity, comp.SmokeScreenAction);
        _actionContainer.EnsureAction(uid, ref comp.EnergyClonesActionEntity, comp.EnergyClonesAction);
        _actionContainer.EnsureAction(uid, ref comp.EmergencyTeleportActionEntity, comp.EmergencyTeleportAction);
        _actionContainer.EnsureAction(uid, ref comp.AdrenalineBurstActionEntity, comp.AdrenalineBurstAction);
        _actionContainer.EnsureAction(uid, ref comp.HealingCocktailActionEntity, comp.HealingCocktailAction);
        _actionContainer.EnsureAction(uid, ref comp.EnergyNetActionEntity, comp.EnergyNetAction);
        _actionContainer.EnsureAction(uid, ref comp.ChameleonScannerActionEntity, comp.ChameleonScannerAction);
        _actionContainer.EnsureAction(uid, ref comp.DashActionEntity, comp.DashAction);
        Dirty(uid, comp);
    }

    /// <summary>
    /// Add base abilities on equip. Selectable abilities are granted via SpiderOS activation.
    /// If SpiderOS is already activated (re-equip after activation), restore chosen abilities immediately.
    /// </summary>
    private void OnGetItemActions(Entity<NinjaSuitComponent> ent, ref GetItemActionsEvent args)
    {
        if (!_ninja.IsNinja(args.User))
            return;

        var comp = ent.Comp;

        // Base abilities — always granted
        args.AddAction(ref comp.RecallKatanaActionEntity, comp.RecallKatanaAction);
        args.AddAction(ref comp.DashActionEntity, comp.DashAction);

        // If SpiderOS was already activated (e.g. re-equip), restore chosen abilities
        if (TryComp<SpiderOSComponent>(ent.Owner, out var spiderOS) && spiderOS.IsActivated)
            AddChosenActionsViaEvent(args, comp, spiderOS.AbilityChoices);
    }

    /// <summary>
    /// Adds selectable ability actions via GetItemActionsEvent based on SpiderOS choices.
    /// Phase Cloak (row 1, choice 0) is handled by ToggleClothing + OnCloakCheck.
    /// </summary>
    protected static void AddChosenActionsViaEvent(GetItemActionsEvent args, NinjaSuitComponent comp, int[] choices)
    {
        // Row 0: Smoke Screen (0) or Chain Kunai (1)
        if (choices[0] == 0)      args.AddAction(ref comp.SmokeScreenActionEntity,        comp.SmokeScreenAction);
        else if (choices[0] == 1) args.AddAction(ref comp.ChainKunaiActionEntity,          comp.ChainKunaiAction);

        // Row 1: Phase Cloak (0) is ToggleClothing-based; Healing Cocktail (1) or Adrenaline Burst (2)
        if (choices[1] == 1)      args.AddAction(ref comp.HealingCocktailActionEntity,     comp.HealingCocktailAction);
        else if (choices[1] == 2) args.AddAction(ref comp.AdrenalineBurstActionEntity,     comp.AdrenalineBurstAction);

        // Row 2: Energy Clones (0), Emergency Teleport (1), EMP (2)
        if (choices[2] == 0)      args.AddAction(ref comp.EnergyClonesActionEntity,        comp.EnergyClonesAction);
        else if (choices[2] == 1) args.AddAction(ref comp.EmergencyTeleportActionEntity,   comp.EmergencyTeleportAction);
        else if (choices[2] == 2) args.AddAction(ref comp.EmpActionEntity,                 comp.EmpAction);

        // Row 3: Chameleon (col 0), Caltrop (col 1), or Energy Net (col 2)
        if      (choices.Length > 3 && choices[3] == 0) args.AddAction(ref comp.ChameleonScannerActionEntity, comp.ChameleonScannerAction);
        else if (choices.Length > 3 && choices[3] == 1) args.AddAction(ref comp.CaltropActionEntity,          comp.CaltropAction);
        else if (choices.Length > 3 && choices[3] == 2) args.AddAction(ref comp.EnergyNetActionEntity,        comp.EnergyNetAction);

        // Row 4: Spirit Form (0) is the only real option
        if (choices.Length > 4 && choices[4] == 0)
            args.AddAction(ref comp.SpiritFormActionEntity, comp.SpiritFormAction);
    }

    private void OnDash(Entity<NinjaSuitComponent> ent, ref DashEvent args)
    {
        var user = args.Performer;
        if (!_ninja.NinjaQuery.TryComp(user, out var ninja) || ninja.Katana is not { } katana)
            return;

        if (!_hands.IsHolding(user, katana, out _))
        {
            Popup.PopupClient(Loc.GetString("dash-ability-not-held", ("item", katana)), user, user);
            return;
        }

        var origin = _transform.GetMapCoordinates(user);
        var target = _transform.ToMapCoordinates(args.Target);
        if (!_examine.InRangeUnOccluded(origin, target, SharedInteractionSystem.MaxRaycastRange, null))
        {
            Popup.PopupClient(Loc.GetString("dash-ability-cant-see", ("item", katana)), user, user);
            return;
        }

        if (!_charges.TryUseCharge(katana))
        {
            Popup.PopupClient(Loc.GetString("dash-ability-no-charges", ("item", katana)), user, user);
            return;
        }

        if (TryComp<PullableComponent>(user, out var pull) && _pulling.IsPulled(user, pull))
            _pulling.TryStopPull(user, pull);
        if (TryComp<PullerComponent>(user, out var puller) && TryComp<PullableComponent>(puller.Pulling, out var pullable))
            _pulling.TryStopPull(puller.Pulling.Value, pullable);

        var xform = Transform(user);
        _transform.SetCoordinates(user, xform, args.Target);
        _transform.AttachToGridOrMap(user, xform);

        var afterEv = new AfterDashEvent(user, origin, args.Target);
        RaiseLocalEvent(katana, ref afterEv);

        args.Handled = true;
    }

    /// <summary>
    /// Only allow Phase Cloak when ninja has activated SpiderOS and chose Phase Cloak (row 1, choice 0).
    /// </summary>
    private void OnCloakCheck(Entity<NinjaSuitComponent> ent, ref ToggleClothingCheckEvent args)
    {
        if (!_ninja.IsNinja(args.User))
        {
            args.Cancelled = true;
            return;
        }

        if (TryComp<SpiderOSComponent>(ent.Owner, out var spiderOS))
        {
            // Block cloaking before SpiderOS activation
            if (!spiderOS.IsActivated)
            {
                args.Cancelled = true;
                return;
            }
            // Block cloaking when Phase Cloak wasn't chosen (row 1 != 0)
            if (spiderOS.AbilityChoices.Length > 1 && spiderOS.AbilityChoices[1] != 0)
                args.Cancelled = true;
        }
    }

    private void OnStarCheck(Entity<NinjaSuitComponent> ent, ref CheckItemCreatorEvent args)
    {
        if (!_ninja.IsNinja(args.User))
        {
            args.Cancelled = true;
            return;
        }

        if (TryComp<SpiderOSComponent>(ent.Owner, out var spiderOS))
        {
            if (!spiderOS.IsActivated)
            {
                args.Cancelled = true;
                return;
            }
            // Only allow throwing stars when Steel (col 2) was chosen for row 0
            if (spiderOS.AbilityChoices[0] != 2)
                args.Cancelled = true;
        }
    }

    private void OnCreateStarAttempt(Entity<NinjaSuitComponent> ent, ref CreateItemAttemptEvent args)
    {
        if (CheckDisabled(ent, args.User))
            args.Cancelled = true;
    }

    /// <summary>
    /// Call the shared and serverside code for when anyone unequips a suit.
    /// </summary>
    private void OnUnequipped(Entity<NinjaSuitComponent> ent, ref GotUnequippedEvent args)
    {
        var user = args.EquipTarget;
        if (_ninja.NinjaQuery.TryComp(user, out var ninja))
            UserUnequippedSuit(ent, (user, ninja));
    }

    /// <summary>
    /// Force uncloaks the user and disables suit abilities.
    /// </summary>
    public void RevealNinja(Entity<NinjaSuitComponent?> ent, EntityUid user, bool disable = true)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var uid = ent.Owner;
        var comp = ent.Comp;
        if (_toggle.TryDeactivate(uid, user) || !disable)
            return;

        // previously cloaked, disable abilities for a short time
        _audio.PlayPredicted(comp.RevealSound, uid, user);
        Popup.PopupClient(Loc.GetString("ninja-revealed"), user, user, PopupType.MediumCaution);
        _useDelay.TryResetDelay(uid, id: comp.DisableDelayId);
    }

    private void OnActivateAttempt(Entity<NinjaSuitComponent> ent, ref ItemToggleActivateAttemptEvent args)
    {
        if (!_ninja.IsNinja(args.User))
        {
            args.Cancelled = true;
            return;
        }

        if (IsDisabled((ent, ent.Comp, null)))
        {
            args.Cancelled = true;
            args.Popup = Loc.GetString("ninja-suit-cooldown");
        }
    }

    /// <summary>
    /// Returns true if the suit is currently disabled
    /// </summary>
    public bool IsDisabled(Entity<NinjaSuitComponent?, UseDelayComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp1, ref ent.Comp2))
            return false;

        return _useDelay.IsDelayed((ent, ent.Comp2), ent.Comp1.DisableDelayId);
    }

    protected bool CheckDisabled(Entity<NinjaSuitComponent> ent, EntityUid user)
    {
        if (IsDisabled((ent, ent.Comp, null)))
        {
            Popup.PopupEntity(Loc.GetString("ninja-suit-cooldown"), user, user, PopupType.Medium);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Called when a suit is unequipped, not necessarily by a space ninja.
    /// In the future it might be changed to also have explicit deactivation via toggle.
    /// </summary>
    protected virtual void UserUnequippedSuit(Entity<NinjaSuitComponent> ent, Entity<SpaceNinjaComponent> user)
    {
        // mark the user as not wearing a suit
        _ninja.AssignSuit(user, null);
        // disable glove abilities
        if (user.Comp.Gloves is { } uid)
            _toggle.TryDeactivate(uid, user: user);
    }
}
