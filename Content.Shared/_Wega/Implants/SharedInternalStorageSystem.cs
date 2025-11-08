using Content.Shared.Actions;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Implants.Components;
using Content.Shared.Item;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.Implants;

public sealed class SharedInternalStorageSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    private static readonly EntProtoId ToothImplantAction = "ActionToothImplant";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InternalStorageComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<InternalStorageComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<InternalStorageComponent, ToothImplantActionEvent>(OnToothImplantAction);
    }

    private void OnInit(EntityUid uid, InternalStorageComponent component, ComponentInit args)
    {
        component.ToothContainer = _container.EnsureContainer<ContainerSlot>(uid, "internal_storage_tooth");
        component.HeadContainer = _container.EnsureContainer<ContainerSlot>(uid, "internal_storage_head");
        component.BodyContainer = _container.EnsureContainer<Container>(uid, "internal_storage_body");
    }

    private void OnShutdown(EntityUid uid, InternalStorageComponent component, ComponentShutdown args)
    {
        _container.EmptyContainer(component.ToothContainer, true);
        _container.EmptyContainer(component.HeadContainer, true);
        _container.EmptyContainer(component.BodyContainer, true);
    }

    private void OnToothImplantAction(Entity<InternalStorageComponent> ent, ref ToothImplantActionEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.ToothContainer.ContainedEntity == null)
            return;

        var pill = ent.Comp.ToothContainer.ContainedEntity.Value;
        if (!_solutionContainer.TryGetSolution(pill, "food", out _, out var solution) || solution.Volume <= FixedPoint2.Zero
            || !_solutionContainer.TryGetInjectableSolution(ent.Owner, out var targetSolution, out _))
        {
            _container.Remove(pill, ent.Comp.ToothContainer);
            _action.RemoveAction(ent.Owner, ent.Comp.ToothImplantActionEntity);
            Del(pill);
            return;
        }

        _solutionContainer.TryTransferSolution(targetSolution.Value, solution, solution.Volume);

        _container.Remove(pill, ent.Comp.ToothContainer);
        _action.RemoveAction(ent.Owner, ent.Comp.ToothImplantActionEntity);
        Del(pill);

        _popup.PopupClient(Loc.GetString("internal-storage-eat-pill"), ent, ent);
        _audio.PlayPredicted(new SoundPathSpecifier("/Audio/Items/pill.ogg"), ent, ent);

        args.Handled = true;
    }

    /// <summary>
    /// Trying to place an item in the internal storage
    /// </summary>
    public bool TryStoreItem(EntityUid uid, EntityUid item, string part, InternalStorageComponent? storage = null)
    {
        if (!Resolve(uid, ref storage))
            return false;

        if (!TryComp<ItemComponent>(item, out var itemComp))
            return false;

        switch (part)
        {
            case "head":
                return itemComp.Size == storage.HeadMaxSize
                    && storage.HeadContainer.ContainedEntity == null
                    && _container.Insert(item, storage.HeadContainer);

            case "torso":
                return (itemComp.Size == storage.HeadMaxSize || itemComp.Size == storage.BodyMaxSize)
                    && storage.BodyContainer.ContainedEntities.Count < 3
                    && _container.Insert(item, storage.BodyContainer);

            case "tooth":
                if (HasComp<PillComponent>(item) && storage.ToothContainer.ContainedEntity == null
                    && _container.Insert(item, storage.ToothContainer))
                {
                    storage.ToothImplantActionEntity = _action.AddAction(uid, ToothImplantAction);
                    return true;
                }
                return false;
        }

        return false;
    }

    /// <summary>
    /// Retrieves an items from storage
    /// </summary>
    public bool TryRemoveItems(EntityUid uid, string part, InternalStorageComponent? storage = null)
    {
        if (!Resolve(uid, ref storage))
            return false;

        switch (part)
        {
            case "head":
                _container.EmptyContainer(storage.HeadContainer);
                return true;

            case "torso":
                _container.EmptyContainer(storage.BodyContainer);
                return true;

            case "tooth":
                if (storage.ToothContainer.ContainedEntity != null)
                {
                    _action.RemoveAction(uid, storage.ToothImplantActionEntity);
                    _container.EmptyContainer(storage.ToothContainer);
                    return true;
                }
                return false;
        }

        return false;
    }
}

public sealed partial class ToothImplantActionEvent : InstantActionEvent { }
