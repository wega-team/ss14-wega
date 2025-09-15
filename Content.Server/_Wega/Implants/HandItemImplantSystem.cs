using Content.Server.Actions;
using Content.Server.Hands.Systems;
using Content.Shared._Wega.Android;
using Content.Shared._Wega.Implants.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction.Components;
using Content.Shared.Toggleable;
using Robust.Server.Audio;
using Robust.Server.Containers;
using Robust.Shared.Containers;

namespace Content.Server._Wega.Implants;

public sealed class HandItemImplantSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;

    [Dependency] private readonly ContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HandItemImplantComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(EntityUid uid, HandItemImplantComponent component, ComponentStartup args)
    {
        var action = _actions.AddAction(uid, ref component.ToggleActionEntity, component.ToggleActionPrototype);

        if (!TryComp<ContainerManagerComponent>(uid, out var containerManager))
            return;

        var entity = Spawn(component.ItemPrototype, Transform(uid).Coordinates);
        component.ItemEntity = entity;

        var container = _container.EnsureContainer<ContainerSlot>(uid, component.ContainerName);
        component.Container = container;

        _container.Insert(entity, container, null, true);
    }

    private void OnShutdown(EntityUid uid, T component) where T : HandItemImplantComponent
    {
        _actions.RemoveAction(component.ToggleActionEntity);

        if (!component.ItemEntity.HasValue || component.Container == null)
            return;

        _container.ShutdownContainer(component.Container);
        EntityManager.DeleteEntity(component.ItemEntity);
    }

    private void OnToggleAction<T>(EntityUid uid, T component, ref ToggleActionEvent args) where T : HandItemImplantComponent
    {
        if (args.Handled || args.Action != component.ToggleActionEntity)
            return;

        if (!args.Action.Comp.Toggled)
            EnableItem(uid, component);
        else
            DisableItem(uid, component);

        args.Toggle = true;
        args.Handled = true;
    }

    public void EnableItem(EntityUid uid, HandItemImplantComponent component)
    {
        if (!component.ItemEntity.HasValue || component.Container == null)
            return;

        if (!_hands.TryGetHand(uid, component.HandId, out var _))
            return;

        _container.Remove(component.ItemEntity.Value, component.Container);
        _hands.TryForcePickup(uid, component.ItemEntity.Value, component.HandId);
        _audio.PlayPvs(component.ToggleSound, uid);

        EnsureComp<UnremoveableComponent>(component.ItemEntity.Value);
    }

    public void DisableItem(EntityUid uid, HandItemImplantComponent component)
    {
        if (!component.ItemEntity.HasValue || component.Container == null)
            return;

        RemComp<UnremoveableComponent>(component.ItemEntity.Value);

        _hands.DoDrop(uid, component.HandId);
        _container.Insert(component.ItemEntity.Value, component.Container, null);
        _audio.PlayPvs(component.ToggleSound, uid);
    }
}
