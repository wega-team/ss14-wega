using Content.Shared.Modular.Suit;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Containers;

namespace Content.Server.Modular.Suit;

public sealed class ModularSuitStorageModuleSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ModularSuitStorageModuleComponent, ModularSuitInstalledEvent>(OnModuleInstalled);
        SubscribeLocalEvent<ModularSuitStorageModuleComponent, ModularSuitRemovedEvent>(OnModuleRemoved);
    }

    private void OnModuleInstalled(Entity<ModularSuitStorageModuleComponent> module, ref ModularSuitInstalledEvent args)
    {
        AddStorageToSuit(args.Suit, module);
    }

    private void OnModuleRemoved(Entity<ModularSuitStorageModuleComponent> module, ref ModularSuitRemovedEvent args)
    {
        RemoveStorageFromSuit(args.Suit, module);
    }

    private void AddStorageToSuit(EntityUid suit, Entity<ModularSuitStorageModuleComponent> module)
    {
        if (!TryComp<StorageComponent>(module.Owner, out var moduleStorage))
            return;

        if (HasComp<StorageComponent>(suit))
            return;

        _storage.CopyComponent((module.Owner, moduleStorage), suit);
        if (TryComp<StorageComponent>(suit, out var storage))
        {
            storage.ShowVerb = true;
            storage.ClickInsert = true;
            storage.OpenOnActivate = true;
            Dirty(suit, storage);
        }
    }

    private void RemoveStorageFromSuit(EntityUid suit, Entity<ModularSuitStorageModuleComponent> module)
    {
        if (!TryComp<StorageComponent>(suit, out var storage))
            return;

        if (storage.Container.ID != module.Comp.ContainerId)
            return;

        var coords = Transform(suit).Coordinates;
        if (TryComp<ModularSuitComponent>(suit, out var modular) && modular.Wearer != null)
            coords = Transform(modular.Wearer.Value).Coordinates;

        _container.EmptyContainer(storage.Container, true, coords);

        RemComp<StorageComponent>(suit);
    }
}
