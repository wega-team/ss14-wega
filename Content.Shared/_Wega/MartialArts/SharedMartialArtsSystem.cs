
using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.Martial.Arts.Components;
using Content.Shared.Martial.Arts.Prototypes;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

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
        if (ent.Comp.Style == null)
            return;

        var style = ent.Comp.Style.FirstOrDefault();
        if (!_prototype.TryIndex(style, out var stylePrototype))
            return;

        if (stylePrototype.Actions == null)
            return;

        if (!ent.Comp.AddedActions.ContainsKey(style))
        {
            ent.Comp.AddedActions[style] = new List<EntityUid>();
        }

        foreach (var action in stylePrototype.Actions)
        {
            var newAction = _action.AddAction(ent, action);
            if (newAction != null)
            {
                ent.Comp.AddedActions[style].Add(newAction.Value);
            }
        }
    }

    private void OnRemoved(Entity<MartialArtsComponent> ent, ref ComponentRemove args)
    {
        if (ent.Comp.AddedActions == null)
            return;

        foreach (var (style, actionList) in ent.Comp.AddedActions)
        {
            foreach (var actionEntity in actionList)
            {
                _action.RemoveAction(ent.Owner, actionEntity);
            }
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

        // Инициализируем список для этого стиля, если его еще нет
        if (!martial.AddedActions.ContainsKey(component.Style))
        {
            martial.AddedActions[component.Style] = new List<EntityUid>();
        }

        foreach (var action in stylePrototype.Actions)
        {
            var newAction = _action.AddAction(args.Equipee, action);
            if (newAction != null)
            {
                martial.AddedActions[component.Style].Add(newAction.Value);
            }
        }


        if (martial.Style == null)
            martial.Style = new List<ProtoId<MartialArtsPrototype>>();

        if (!martial.Style.Contains(component.Style))
            martial.Style.Add(component.Style);

        if (component.GotMessage && !string.IsNullOrEmpty(component.EquippedMessage))
            _popup.PopupEntity(Loc.GetString(component.EquippedMessage), args.Equipee, args.Equipee);
    }

    private void OnUnequipped(EntityUid uid, MartialArtsClothingComponent component, GotUnequippedEvent args)
    {
        if (!TryComp<MartialArtsComponent>(args.Equipee, out var martial))
            return;

        if (martial.AddedActions == null)
            return;

        if (martial.AddedActions.TryGetValue(component.Style, out var actionList))
        {
            foreach (var actionEntity in actionList)
            {
                _action.RemoveAction(args.Equipee, actionEntity);
            }
            martial.AddedActions.Remove(component.Style);
        }

        if (martial.Style != null)
            martial.Style.Remove(component.Style);

        if (component.GotMessage && !string.IsNullOrEmpty(component.UnequippedMessage))
            _popup.PopupEntity(Loc.GetString(component.UnequippedMessage), args.Equipee, args.Equipee);
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

            if (comp.Style != null)
                comp.Style.Add(style);
            Dirty(uid, comp);
            return true;
        }
        return false;
    }
}
