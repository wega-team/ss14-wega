using Content.Client.Clothing;
using Content.Shared.Inventory;
using Content.Shared.Ninja.Components;
using Robust.Shared.Containers;

namespace Content.Client.Ninja.Systems;

/// <summary>
/// When a worn item gains or loses <see cref="NinjaHiddenClothingComponent"/> (the ninja chameleon
/// disguise hiding/showing the item's on-body visual), immediately re-render the wearer's
/// equipment so the change is visible without waiting for an unrelated clothing event.
/// </summary>
public sealed class NinjaHiddenClothingVisualSystem : EntitySystem
{
    [Dependency] private readonly ClientClothingSystem _clothing = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NinjaHiddenClothingComponent, ComponentStartup>(OnChanged);
        SubscribeLocalEvent<NinjaHiddenClothingComponent, ComponentShutdown>(OnChanged);
    }

    private void OnChanged<T>(EntityUid uid, NinjaHiddenClothingComponent comp, T args)
    {
        // Find the entity wearing this item and re-render its equipment.
        if (!_container.TryGetContainingContainer((uid, null), out var container))
            return;

        var wearer = container.Owner;
        if (TryComp<InventoryComponent>(wearer, out var inv))
            _clothing.InitClothing(wearer, inv);
    }
}
