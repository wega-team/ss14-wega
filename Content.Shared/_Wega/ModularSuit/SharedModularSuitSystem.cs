using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.Modular.Suit;

public abstract partial class SharedModularSuitSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming GameTiming = default!;
    [Dependency] protected readonly InventorySystem Inventory = default!;
    [Dependency] protected readonly ItemToggleSystem Toggle = default!;
    [Dependency] protected readonly SharedContainerSystem Container = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;

    public const string CoreContainer = "suit_core";
    public const string PartContainer = "suit_part";
    public const string ModuleContainer = "suit_module";
    public const string HiddenClothingContainer = "suit_hidden";

    public override void Initialize()
    {
        SubscribeLocalEvent<ModularSuitComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ModularSuitComponent, GetItemActionsEvent>(OnGetItemActions);
        SubscribeLocalEvent<ModularSuitComponent, ToggleSuitUiActionEvent>(OnToggleUi);

        SubscribeLocalEvent<ModularSuitComponent, ClothingGotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<ModularSuitComponent, GotUnequippedEvent>(OnUnequipped);

        SubscribeLocalEvent<ModularSuitPartComponent, BeingEquippedAttemptEvent>(OnPartEquippedAttempt);
        SubscribeLocalEvent<ModularSuitPartComponent, GotUnequippedEvent>(OnPartUnequipped);

        SubscribeLocalEvent<ModularSuitModuleComponent, MapInitEvent>(OnModuleInit);
    }

    private void OnMapInit(Entity<ModularSuitComponent> ent, ref MapInitEvent args)
    {
        _actionContainer.EnsureAction(ent, ref ent.Comp.ToggleUiActionEntity, ent.Comp.ToggleUiAction);
        ent.Comp.NextUpdate = GameTiming.CurTime + ent.Comp.UpdateInterval;
    }

    private void OnGetItemActions(Entity<ModularSuitComponent> ent, ref GetItemActionsEvent args)
    {
        if (args.InHands)
            return;

        args.AddAction(ref ent.Comp.ToggleUiActionEntity, ent.Comp.ToggleUiAction);
    }

    private void OnToggleUi(Entity<ModularSuitComponent> ent, ref ToggleSuitUiActionEvent args)
    {
        if (args.Handled)
            return;

        ToggleUi(ent, args.Performer);
        args.Handled = true;
    }

    public void ToggleUi(Entity<ModularSuitComponent> ent, EntityUid user)
    {
        if (_uiSystem.IsUiOpen(ent.Owner, ModularSuitUiKey.Key))
        {
            _uiSystem.CloseUi(ent.Owner, ModularSuitUiKey.Key, user);
        }
        else
        {
            _uiSystem.OpenUi(ent.Owner, ModularSuitUiKey.Key, user);
            if (ent.Comp.UiOpenSound != null)
            {
                _audioSystem.PlayEntity(ent.Comp.UiOpenSound, user, user);
            }
        }
    }

    private void OnEquipped(Entity<ModularSuitComponent> ent, ref ClothingGotEquippedEvent args)
    {
        ent.Comp.Wearer = args.Wearer;
        EnsureComp<ModularSuitCarrierComponent>(args.Wearer).CurrentSlot = args.Clothing.InSlot;

        EquipAllParts(ent, args.Wearer);
    }

    private void OnUnequipped(Entity<ModularSuitComponent> ent, ref GotUnequippedEvent args)
    {
        SetActive(ent, false);
        UnequipAllParts(ent, args.Equipee);

        ent.Comp.Wearer = null;
        RemComp<ModularSuitCarrierComponent>(args.Equipee);
        _uiSystem.CloseUi(ent.Owner, ModularSuitUiKey.Key, args.Equipee);
    }

    private void EquipAllParts(Entity<ModularSuitComponent> suit, EntityUid wearer)
    {
        var partContainer = Container.GetContainer(suit, PartContainer);
        var partsToEquip = new List<EntityUid>(partContainer.ContainedEntities);
        var equippedComp = EnsureComp<ModularSuitEquippedComponent>(suit.Owner);

        foreach (var partUid in partsToEquip)
        {
            if (!HasComp<ModularSuitPartComponent>(partUid))
                continue;

            if (!TryGetSlotFromClothing(partUid, out var slot) || string.IsNullOrEmpty(slot))
                continue;

            StoreExistingClothingInSlot(suit, wearer, slot);
            if (Inventory.TryEquip(wearer, partUid, slot, force: true))
            {
                equippedComp.EquippedParts[slot] = partUid;
                var attached = EnsureComp<AttachedModularSuitPartComponent>(partUid);
                attached.Suit = suit;

                Dirty(partUid, attached);
            }
        }

        Dirty(suit.Owner, equippedComp);
    }

    private void UnequipAllParts(Entity<ModularSuitComponent> suit, EntityUid wearer, ModularSuitEquippedComponent? equippedComp = null)
    {
        if (!Resolve(suit.Owner, ref equippedComp))
            return;

        var partContainer = Container.GetContainer(suit, PartContainer);
        foreach (var (slot, partUid) in equippedComp.EquippedParts.ToList())
        {
            if (Inventory.TryGetSlotEntity(wearer, slot, out var equipped) && equipped == partUid)
            {
                if (Inventory.TryUnequip(wearer, slot, out var removedItem))
                {
                    Container.Insert(removedItem.Value, partContainer);
                    RemComp<AttachedModularSuitPartComponent>(removedItem.Value);

                    RestoreHiddenClothingInSlot(suit, wearer, slot);
                }
            }
        }

        equippedComp.EquippedParts.Clear();
        Dirty(suit, equippedComp);
    }

    private bool TryGetSlotFromClothing(EntityUid uid, out string? slot)
    {
        slot = string.Empty;
        if (!TryComp<ClothingComponent>(uid, out var clothing))
            return false;

        var flags = clothing.Slots;
        if (flags == SlotFlags.NONE)
            return false;

        slot = flags switch
        {
            SlotFlags.HEAD => "head",
            SlotFlags.EYES => "eyes",
            SlotFlags.EARS => "ears",
            SlotFlags.MASK => "mask",
            SlotFlags.OUTERCLOTHING => "outerClothing",
            SlotFlags.INNERCLOTHING => "jumpsuit",
            SlotFlags.NECK => "neck",
            SlotFlags.BACK => "back",
            SlotFlags.BELT => "belt",
            SlotFlags.GLOVES => "gloves",
            SlotFlags.FEET => "shoes",
            _ => null
        };

        return slot != null;
    }

    private void StoreExistingClothingInSlot(Entity<ModularSuitComponent> suit, EntityUid wearer, string slot)
    {
        if (!Inventory.TryGetSlotEntity(wearer, slot, out var existingItem))
            return;

        if (HasComp<ModularSuitPartComponent>(existingItem))
            return;

        var hiddenComp = EnsureComp<ModularSuitHiddenClothingComponent>(suit.Owner);
        if (hiddenComp.HiddenItems.ContainsKey(slot))
            return;

        hiddenComp.HiddenItems[slot] = existingItem.Value;
        if (Inventory.TryUnequip(wearer, slot, out var removedItem, force: true))
        {
            var hiddenContainer = Container.GetContainer(suit.Owner, HiddenClothingContainer);
            Container.Insert(removedItem.Value, hiddenContainer);
        }

        Dirty(suit.Owner, hiddenComp);
    }

    private void RestoreHiddenClothingInSlot(Entity<ModularSuitComponent> suit, EntityUid wearer, string slot)
    {
        if (!TryComp<ModularSuitHiddenClothingComponent>(suit.Owner, out var hiddenComp))
            return;

        if (!hiddenComp.HiddenItems.TryGetValue(slot, out var itemUid))
            return;

        var hiddenContainer = Container.GetContainer(suit.Owner, HiddenClothingContainer);
        if (!hiddenContainer.Contains(itemUid))
        {
            hiddenComp.HiddenItems.Remove(slot);
            Dirty(suit.Owner, hiddenComp);
            return;
        }

        if (Inventory.TryGetSlotEntity(wearer, slot, out var currentItem))
        {
            if (HasComp<ModularSuitPartComponent>(currentItem))
                return;
        }

        if (Container.Remove(itemUid, hiddenContainer))
            Inventory.TryEquip(wearer, itemUid, slot);

        hiddenComp.HiddenItems.Remove(slot);
        Dirty(suit.Owner, hiddenComp);
    }

    private void OnPartEquippedAttempt(EntityUid uid, ModularSuitPartComponent component, BeingEquippedAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        Popup.PopupEntity(Loc.GetString("modsuit-impossible-equipped-part"), args.Equipee, args.Equipee, PopupType.SmallCaution);
        args.Cancel();
    }

    private void OnPartUnequipped(Entity<ModularSuitPartComponent> ent, ref GotUnequippedEvent args)
    {
        Toggle.TryDeactivate(ent.Owner, ent.Owner, showPopup: false);
    }

    private void OnModuleInit(Entity<ModularSuitModuleComponent> ent, ref MapInitEvent args)
    {
        if (!string.IsNullOrEmpty(ent.Comp.ModuleId))
            return;

        ent.Comp.ModuleId = $"{ent.Comp.ModulePrefix}{Guid.NewGuid().ToString("N")[..6]}";
        Dirty(ent.Owner, ent.Comp);
    }

    public void SetModulePermanent(Entity<ModularSuitModuleComponent?> module, bool value)
    {
        if (!Resolve(module, ref module.Comp))
            return;

        module.Comp.IsPermanent = value;
        Dirty(module.Owner, module.Comp);
    }

    public virtual void SetActive(Entity<ModularSuitComponent> ent, bool active)
    {
        if (ent.Comp.Active == active)
            return;

        ent.Comp.Active = active;
        Dirty(ent.Owner, ent.Comp);
    }
}
