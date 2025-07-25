using Content.Shared.Containers.ItemSlots;
using Content.Shared.Humanoid;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Storage.Components;
using Content.Shared.Verbs;
using Content.Shared.Xenobiology.Components.Container;
using Content.Shared.Xenobiology.Components.Machines;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.Xenobiology.Systems.Machines;

public sealed class CellMutagenicInjectorSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedCellSystem _cell = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _powerReceiver = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CellMutagenicInjectorComponent, GetVerbsEvent<AlternativeVerb>>(AddInjectorVerb);
        SubscribeLocalEvent<CellMutagenicInjectorComponent, StorageBeforeOpenEvent>(OnStorageOpen);
    }

    private void AddInjectorVerb(Entity<CellMutagenicInjectorComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!_powerReceiver.IsPowered(ent.Owner) || ent.Comp.Enabled)
            return;

        AlternativeVerb verb = new()
        {
            Text = Loc.GetString("bybyby"),
            Act = () =>
            {
                MutagenicInjectorActivate(ent);
            },
        };
        args.Verbs.Add(verb);
    }

    private void MutagenicInjectorActivate(Entity<CellMutagenicInjectorComponent> ent)
    {
        if (!TryDish(ent, out var cell) || cell == null)
        {
            _popup.PopupPredicted(Loc.GetString("cell-injector-no-dish"), ent, null, PopupType.MediumCaution);
            return;
        }

        if (!TryEntity(ent, out var target) || target == null)
        {
            _popup.PopupPredicted(Loc.GetString("cell-injector-no-target"), ent, null, PopupType.MediumCaution);
            return;
        }

        if (HasComp<HumanoidAppearanceComponent>(target))
        {
            _popup.PopupPredicted(Loc.GetString("cell-injector-no-humanoid"), ent, null, PopupType.MediumCaution);
            return;
        }

        ent.Comp.Enabled = true;
        ent.Comp.Cell = cell;
        ent.Comp.Target = target;
        ent.Comp.ActivateTime = _gameTiming.RealTime;
        ent.Comp.PlayingStream =
            _audio.PlayPvs(ent.Comp.LoopingSound, ent, AudioParams.Default.WithLoop(true).WithMaxDistance(5))?.Entity;
        // End
    }

    private bool TryDish(Entity<CellMutagenicInjectorComponent> ent, out EntityUid? itemUid)
    {
        itemUid = null;
        if (_itemSlots.TryGetSlot(ent, ent.Comp.DishSlot, out var slot))
        {
            if (!TryComp<CellContainerComponent>(slot.Item, out var cell) || cell.Empty)
                return false;

            itemUid = slot.Item;
            return slot.HasItem;
        }
        return false;
    }

    private bool TryEntity(Entity<CellMutagenicInjectorComponent> ent, out EntityUid? target)
    {
        target = null;
        var containerSystem = EntityManager.System<SharedContainerSystem>();
        if (!containerSystem.TryGetContainer(ent, ent.Comp.EntitySlot, out var container))
            return false;

        if (container.ContainedEntities.Count > 0)
        {
            target = container.ContainedEntities[0];
            return true;
        }
        return false;
    }

    private void OnStorageOpen(Entity<CellMutagenicInjectorComponent> ent, ref StorageBeforeOpenEvent args)
    {
        if (ent.Comp.Enabled)
        {
            TryStopMutagenicInjector(ent);
        }
    }

    private void TryStopMutagenicInjector(Entity<CellMutagenicInjectorComponent> ent)
    {
        if (ent.Comp.Cell == null || ent.Comp.Target == null || !TryComp<CellContainerComponent>(ent.Comp.Cell, out var cell)
            || !_powerReceiver.IsPowered(ent.Owner))
        {
            ent.Comp.ActivateTime = TimeSpan.Zero;
            return;
        }

        var currentTime = _gameTiming.RealTime;
        var elapsedTime = currentTime - ent.Comp.ActivateTime;
        if (elapsedTime > ent.Comp.MaxTime)
        {
            QueueDel(ent.Comp.Target);
            EntityManager.SpawnEntity(ent.Comp.FailedMob, Transform(ent).Coordinates);
            _popup.PopupPredicted(Loc.GetString("cell-injector-mutate-failed"), ent, null, PopupType.MediumCaution);
        }
        else if (elapsedTime > ent.Comp.MinTime)
        {
            // TODO Здесь присвоение компонентов сущности их инициализация и прочее говно
            _popup.PopupPredicted(Loc.GetString("cell-injector-mutate-succes"), ent, null, PopupType.Medium);
        }

        ent.Comp.PlayingStream = _audio.Stop(ent.Comp.PlayingStream);

        ent.Comp.Enabled = false;
        ent.Comp.Target = null;

        _cell.ClearCells(new Entity<CellContainerComponent?>(ent.Comp.Cell.Value, cell));
        ent.Comp.Cell = null;
    }
}
