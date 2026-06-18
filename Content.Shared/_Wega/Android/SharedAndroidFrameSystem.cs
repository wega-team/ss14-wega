using Content.Shared.Containers.ItemSlots;
using Content.Shared.UserInterface;
using Robust.Shared.Containers;

namespace Content.Shared._Wega.Android;

public abstract partial class SharedAndroidFrameSystem : EntitySystem
{
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private ItemSlotsSystem _slots = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AndroidFrameComponent, AfterActivatableUIOpenEvent>(OnInterfaceOpen);
        SubscribeLocalEvent<AndroidFrameComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
        SubscribeLocalEvent<AndroidFrameComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
    }

    public bool TryGetFromSlot(EntityUid uid, string slotId, out EntityUid? entity)
    {
        entity = null;
        if (!HasComp<ItemSlotsComponent>(uid) || !_slots.TryGetSlot(uid, slotId, out var slot))
            return false;

        if (slot.ContainerSlot == null || slot.ContainerSlot.ContainedEntity == null)
            return false;

        entity = slot.ContainerSlot.ContainedEntity;
        return true;
    }

    private void UpdateUiState(EntityUid uid, AndroidFrameComponent component)
    {
        if (component.Profile == null)
            return;

        var state = new AndroidConstructUiState(
            component.Species,
            component.Profile,
            TryGetFromSlot(uid, component.BatterySlot, out _),
            TryGetFromSlot(uid, component.BrainSlot, out _)
        );

        _ui.SetUiState(uid, AndroidConstructUiKey.Key, state);
    }

    private void OnInterfaceOpen(EntityUid uid, AndroidFrameComponent component, AfterActivatableUIOpenEvent args)
    {
        UpdateUiState(uid, component);
    }

    private void OnEntInserted(EntityUid uid, AndroidFrameComponent component, EntInsertedIntoContainerMessage args)
    {
        UpdateUiState(uid, component);
    }

    private void OnEntRemoved(EntityUid uid, AndroidFrameComponent component, EntRemovedFromContainerMessage args)
    {
        UpdateUiState(uid, component);
    }
}
