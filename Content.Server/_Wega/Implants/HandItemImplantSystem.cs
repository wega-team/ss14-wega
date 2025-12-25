using Content.Server.Actions;
using Content.Server.Hands.Systems;
using Content.Shared._Wega.Implants.Components;
using Content.Shared.Interaction.Components;
using Content.Shared.Toggleable;
using Robust.Server.Audio;
using Robust.Server.Containers;
using Robust.Shared.Containers;

namespace Content.Server.Implants;

public sealed class HandItemImplantSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;

    [Dependency] private readonly ContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HandItemImplantComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<HandItemImplantComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<HandItemImplantComponent, ToggleActionEvent>(OnToggleAction);

        SubscribeLocalEvent<ExtendHandItemImplantComponent, BodyPartImplantAddedEvent>(OnExtendBodyPartAdded);
        SubscribeLocalEvent<HandItemImplantComponent, BodyPartImplantRemovedEvent>(OnBodyPartRemoved);
    }

    private void OnInit(EntityUid uid, HandItemImplantComponent component, ComponentInit args)
    {
        for (var i = 0; i < component.Items.Count; i++)
            InitItem(uid, component, i);
    }

    private void OnShutdown(EntityUid uid, HandItemImplantComponent component, ComponentShutdown args)
    {
        for (var i = 0; i < component.Items.Count; i++)
            RemoveItem(uid, component, i);

        if (component.Container == null)
            return;

        _container.ShutdownContainer(component.Container);
    }

    public void InitItem(EntityUid uid, HandItemImplantComponent component, int itemIndex)
    {
        var item = component.Items[itemIndex];

        var action = _actions.AddAction(uid, item.ToggleActionPrototype);
        item.ToggleActionEntity = action;

        if (!TryComp<ContainerManagerComponent>(uid, out var containerManager))
            return;

        var entity = Spawn(item.ItemPrototype, Transform(uid).Coordinates);
        item.ItemEntity = entity;

        var container = _container.EnsureContainer<Container>(uid, component.ContainerName);
        component.Container = container;
        _container.Insert(entity, container, null, true);

        component.Items[itemIndex] = item;
        DisableItem(uid, component, item);
    }

    public void RemoveItem(EntityUid uid, HandItemImplantComponent component, int itemIndex)
    {
        var item = component.Items[itemIndex];

        _actions.RemoveAction(item.ToggleActionEntity);

        if (!item.ItemEntity.HasValue || component.Container == null)
            return;

        EntityManager.DeleteEntity(item.ItemEntity);
        component.Items.RemoveAt(itemIndex);
    }

    private void OnExtendBodyPartAdded(EntityUid uid, ExtendHandItemImplantComponent component, ref BodyPartImplantAddedEvent args)
    {
        var implant = EnsureComp<HandItemImplantComponent>(uid);

        for (var i = 0; i < component.Items.Count; i++)
        {
            var item = component.Items[i];

            item.ImplantEntity = args.Part;
            implant.Items.Add(item);
            InitItem(uid, implant, implant.Items.Count - 1);
        }

        RemComp<ExtendHandItemImplantComponent>(uid);
    }

    private void OnBodyPartRemoved(EntityUid uid, HandItemImplantComponent component, ref BodyPartImplantRemovedEvent args)
    {
        for (var i = 0; i < component.Items.Count; i++)
        {
            if (args.Part == component.Items[i].ImplantEntity)
                RemoveItem(uid, component, i);
        }
    }

    private void OnToggleAction(EntityUid uid, HandItemImplantComponent component, ref ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        foreach (var item in component.Items)
        {
            if (args.Action == item.ToggleActionEntity)
            {
                if (!args.Action.Comp.Toggled)
                    EnableItem(uid, component, item);
                else
                    DisableItem(uid, component, item);

                args.Toggle = true;
                args.Handled = true;
                return;
            }
        }
    }

    public void EnableItem(EntityUid uid, HandItemImplantComponent component, HandItemImplantSlot item)
    {
        if (!item.ItemEntity.HasValue || component.Container == null)
            return;

        if (!_hands.TryGetHand(uid, item.HandId, out var _))
            return;

        _container.Remove(item.ItemEntity.Value, component.Container);
        _hands.TryForcePickup(uid, item.ItemEntity.Value, item.HandId);
        _audio.PlayPvs(component.ToggleSound, uid);

        EnsureComp<UnremoveableComponent>(item.ItemEntity.Value);
    }

    public void DisableItem(EntityUid uid, HandItemImplantComponent component, HandItemImplantSlot item)
    {
        if (!item.ItemEntity.HasValue || component.Container == null)
            return;

        RemComp<UnremoveableComponent>(item.ItemEntity.Value);

        _hands.DoDrop(uid, item.HandId);
        _container.Insert(item.ItemEntity.Value, component.Container, null);
        _audio.PlayPvs(component.ToggleSound, uid);
    }
}
