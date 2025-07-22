
using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.Martial.Arts.Components;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;

namespace Content.Shared.Martial.Arts;

public abstract class SharedMartialArtsSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MartialArtsComponent, MapInitEvent>(OnInitialized);
        SubscribeLocalEvent<MartialArtsComponent, ComponentRemove>(OnRemoved);
        SubscribeLocalEvent<MartialArtsClothingComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<MartialArtsClothingComponent, GotUnequippedEvent>(OnUnequipped);
    }

    private void OnInitialized(Entity<MartialArtsComponent> ent, ref MapInitEvent args)
    {
        var style = ent.Comp.Style.FirstOrDefault();
        if (!_prototype.TryIndex(style, out var stylePrototype))
            return;

        if (stylePrototype.Actions == null)
            return;

        foreach (var action in stylePrototype.Actions)
        {
            var newAction = _action.AddAction(ent, action);
            if (newAction != null)
            {
                ent.Comp.AddedActions.Add(style, newAction.Value);
            }
        }
    }

    private void OnRemoved(Entity<MartialArtsComponent> ent, ref ComponentRemove args)
    {
        if (ent.Comp.AddedActions == null)
            return;

        foreach (var (_, actionEntity) in ent.Comp.AddedActions)
        {
            _action.RemoveAction(ent.Owner, actionEntity);
        }
    }

    private void OnEquipped(EntityUid uid, MartialArtsClothingComponent component, GotEquippedEvent args)
    {
        if (!TryComp<ClothingComponent>(uid, out var clothing) || !clothing.Slots.HasFlag(args.SlotFlags))
            return;

        if (!_prototype.TryIndex(component.Style, out var stylePrototype))
            return;

        if (stylePrototype.Actions == null)
            return;

        if (!TryComp<MartialArtsComponent>(args.Equipee, out var martial))
            martial = EnsureComp<MartialArtsComponent>(args.Equipee);

        foreach (var action in stylePrototype.Actions)
        {
            var newAction = _action.AddAction(args.Equipee, action);
            if (newAction != null)
            {
                martial.AddedActions.Add(component.Style, newAction.Value);
            }
        }

        if (component.GotMessage && !string.IsNullOrEmpty(component.EquippedMessage))
            _popup.PopupEntity(component.EquippedMessage, args.Equipee, args.Equipee);
    }

    private void OnUnequipped(EntityUid uid, MartialArtsClothingComponent component, GotUnequippedEvent args)
    {
        if (!TryComp<MartialArtsComponent>(args.Equipee, out var martial))
            return;

        if (martial.AddedActions == null)
            return;

        var keysToRemove = new List<string>();
        foreach (var (styleId, actionEntity) in martial.AddedActions)
        {
            if (styleId == component.Style)
            {
                _action.RemoveAction(args.Equipee, actionEntity);
                keysToRemove.Add(styleId);
            }
        }

        foreach (var key in keysToRemove)
        {
            martial.AddedActions.Remove(key);
        }

        martial.Style.Remove(component.Style);
        if (component.GotMessage && !string.IsNullOrEmpty(component.UnequippedMessage))
            _popup.PopupEntity(component.UnequippedMessage, args.Equipee, args.Equipee);
    }

    /// <summary>
    /// A method for adding a martial art.
    /// </summary>
    /// <param name="uid">The entity to which the component is added.</param>
    /// <param name="style">The style that needs to be assigned.</param>
    /// <returns></returns>
    public bool TryAddMartialArts(EntityUid uid, string style)
    {
        if (!HasComp<MartialArtsComponent>(uid))
        {
            EnsureComp<MartialArtsComponent>(uid, out var comp);

            comp.Style.Add(style);
            Dirty(uid, comp);
            return true;
        }
        return false;
    }
}
