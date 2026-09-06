using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.Upgrades.Components;
using Content.Shared.Inventory;
using Robust.Client.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Client.Clothing;

/// <summary>
/// мяу-мяу ёпта
/// </summary>
public sealed partial class ClientClothingSystem
{
    [Dependency] private SharedContainerSystem _container = default!;

    private bool StateExists(EntityUid uid, string state, string? speciesId)
    {
        if (TryComp<SpriteComponent>(uid, out var sprite) && sprite.BaseRSI != null)
        {
            if (!string.IsNullOrEmpty(speciesId))
            {
                var speciesState = $"{state}-{speciesId}";
                if (sprite.BaseRSI.TryGetState(speciesState, out _))
                    return true;
            }

            return sprite.BaseRSI.TryGetState(state, out _);
        }
        return false;
    }

    [SubscribeLocalEvent]
    private void OnHandleState(EntityUid uid, ToggleableSpriteClothingComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not ToggleableSpriteClothingComponentState state)
            return;

        component.ActiveSuffix = state.ActiveSuffix;
        UpdateClothingVisuals(uid);
    }

    private void UpdateClothingVisuals(EntityUid uid)
    {
        if (!TryComp<ClothingComponent>(uid, out var clothing)
            || clothing.InSlot == null)
            return;

        var parent = Transform(uid).ParentUid;
        if (!HasComp<SpriteComponent>(parent) || !TryComp<InventoryComponent>(parent, out var inventory))
            return;

        RenderEquipment(parent, uid, clothing.InSlot, inventory, clothingComponent: clothing);
    }

    private IEnumerable<Entity<ClothingUpgradeComponent>> GetCurrentUpgrades(EntityUid clothing, UpgradeableClothingComponent component)
    {
        if (!_container.TryGetContainer(clothing, component.UpgradesContainerId, out var container))
            yield break;

        foreach (var contained in container.ContainedEntities)
        {
            if (TryComp<ClothingUpgradeComponent>(contained, out var upgradeComp))
                yield return (contained, upgradeComp);
        }
    }
}
