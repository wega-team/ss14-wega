using System.Diagnostics.CodeAnalysis;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared.Modular.Suit;
using Robust.Shared.Containers;

namespace Content.Server.Modular.Suit;

public sealed class ModularSuitSuitEffectSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ModularSuitModuleEffectComponent, ModularSuitInstalledEvent>(OnModuleInstalled);
        SubscribeLocalEvent<ModularSuitModuleEffectComponent, ModularSuitRemovedEvent>(OnModuleRemoved);
        SubscribeLocalEvent<ModularSuitModuleEffectComponent, ModularSuitModuleToggledEvent>(OnModuleToggled);
    }

    private void OnModuleInstalled(Entity<ModularSuitModuleEffectComponent> module, ref ModularSuitInstalledEvent args)
    {
        if (TryComp<ModularSuitModuleComponent>(module.Owner, out var moduleComp) && moduleComp.IsActive)
            ApplyEffects(args.User, args.Suit, module.Comp);
    }

    private void OnModuleRemoved(Entity<ModularSuitModuleEffectComponent> module, ref ModularSuitRemovedEvent args)
    {
        if (module.Comp.ActiveComponents != null)
        {
            RemoveEffects(args.User, args.Suit, module.Comp);
        }
    }

    private void OnModuleToggled(Entity<ModularSuitModuleEffectComponent> module, ref ModularSuitModuleToggledEvent args)
    {
        if (args.Activated)
        {
            ApplyEffects(args.Wearer, args.Suit, module.Comp);
        }
        else
        {
            RemoveEffects(args.Wearer, args.Suit, module.Comp);
        }
    }

    private void ApplyEffects(EntityUid? user, EntityUid suit, ModularSuitModuleEffectComponent component)
    {
        if (component.ActiveComponents == null)
            return;

        if (!TryGetTargetEntity(user, suit, component.TargetSlot, out var targetEntity))
            return;

        EntityManager.AddComponents(targetEntity.Value, component.ActiveComponents);

        // Sync
        foreach (var (_, entry) in component.ActiveComponents)
        {
            var compType = entry.Component.GetType();
            if (EntityManager.TryGetComponent(targetEntity.Value, compType, out var comp))
                Dirty(targetEntity.Value, comp);
        }
    }

    private void RemoveEffects(EntityUid? user, EntityUid suit, ModularSuitModuleEffectComponent component)
    {
        if (component.ActiveComponents == null)
            return;

        if (!TryGetTargetEntity(user, suit, component.TargetSlot, out var targetEntity))
            return;

        EntityManager.RemoveComponents(targetEntity.Value, component.ActiveComponents);

        if (component.ReturnedComponents != null)
        {
            EntityManager.AddComponents(targetEntity.Value, component.ReturnedComponents);
        }
    }

    private bool TryGetTargetEntity(EntityUid? user, EntityUid suit, string targetSlot, [NotNullWhen(true)] out EntityUid? targetEntity)
    {
        targetEntity = null;
        if (user != null && _inventory.TryGetSlotEntity(user.Value, targetSlot, out var wearerSlot))
        {
            targetEntity = wearerSlot;
            return true;
        }

        if (targetSlot == "back")
        {
            targetEntity = suit;
            return true;
        }

        var partContainer = _container.GetContainer(suit, SharedModularSuitSystem.PartContainer);
        foreach (var part in partContainer.ContainedEntities)
        {
            if (TryComp<ClothingComponent>(part, out var clothing) && clothing.Slots.HasFlag(GetSlotFlag(targetSlot)))
            {
                targetEntity = part;
                return true;
            }
        }

        return false;
    }

    private SlotFlags GetSlotFlag(string slot)
    {
        return slot switch
        {
            "head" => SlotFlags.HEAD,
            "eyes" => SlotFlags.EYES,
            "ears" => SlotFlags.EARS,
            "mask" => SlotFlags.MASK,
            "outerClothing" => SlotFlags.OUTERCLOTHING,
            "jumpsuit" => SlotFlags.INNERCLOTHING,
            "neck" => SlotFlags.NECK,
            "belt" => SlotFlags.BELT,
            "gloves" => SlotFlags.GLOVES,
            "shoes" => SlotFlags.FEET,
            _ => SlotFlags.NONE
        };
    }
}
