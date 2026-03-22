using Content.Shared.Modular.Suit;
using Content.Shared.Storage;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;

namespace Content.Server.Modular.Suit;

public sealed class ModularSuitStorageModuleSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ModularSuitStorageModuleComponent, EntityTerminatingEvent>(OnTerminating);
        SubscribeLocalEvent<ModularSuitStorageModuleComponent, ModularSuitRemovedEvent>(OnModuleRemoved);

        SubscribeLocalEvent<ModularSuitActionHolderComponent, OpenStorageModuleEvent>(OnOpenStorage);
    }

    private void OnTerminating(Entity<ModularSuitStorageModuleComponent> module, ref EntityTerminatingEvent args)
    {
        if (_container.TryGetContainer(module.Owner, module.Comp.ContainerId, out var container))
            _container.EmptyContainer(container, true);
    }

    private void OnModuleRemoved(Entity<ModularSuitStorageModuleComponent> module, ref ModularSuitRemovedEvent args)
    {
        if (_container.TryGetContainer(module.Owner, module.Comp.ContainerId, out var container))
            _container.EmptyContainer(container, true);

        if (_ui.HasUi(module.Owner, StorageComponent.StorageUiKey.Key))
            _ui.CloseUi(module.Owner, StorageComponent.StorageUiKey.Key);
    }

    private void OnOpenStorage(Entity<ModularSuitActionHolderComponent> ent, ref OpenStorageModuleEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<ModularSuitComponent>(ent, out var suit) || !suit.Active)
            return;

        EntityUid? moduleEnt = null;
        var moduleContainer = _container.GetContainer(ent.Owner, ModularSuitSystem.ModuleContainer);
        foreach (var module in moduleContainer.ContainedEntities)
        {
            if (!HasComp<StorageComponent>(module))
                continue;

            if (!TryComp<ModularSuitModuleComponent>(module, out var moduleComp) || !moduleComp.IsActive)
                continue;

            moduleEnt = module;
            break;
        }

        if (!moduleEnt.HasValue)
            return;

        args.Handled = _ui.TryToggleUi(moduleEnt.Value, StorageComponent.StorageUiKey.Key, args.Performer);
    }
}
