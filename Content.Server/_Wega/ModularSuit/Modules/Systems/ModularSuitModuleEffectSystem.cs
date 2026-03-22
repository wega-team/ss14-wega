using Content.Shared.Modular.Suit;
using Content.Shared.Inventory;

namespace Content.Server.Modular.Suit;

public sealed class ModularSuitSuitEffectSystem : EntitySystem
{
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
            ApplyEffects(args.User, module.Comp);
    }

    private void OnModuleRemoved(Entity<ModularSuitModuleEffectComponent> module, ref ModularSuitRemovedEvent args)
    {
        if (module.Comp.ActiveComponents != null)
        {
            RemoveEffects(args.User, module.Comp);
        }
    }

    private void OnModuleToggled(Entity<ModularSuitModuleEffectComponent> module, ref ModularSuitModuleToggledEvent args)
    {
        if (args.Activated)
        {
            ApplyEffects(args.Wearer, module.Comp);
        }
        else
        {
            RemoveEffects(args.Wearer, module.Comp);
        }
    }

    private void ApplyEffects(EntityUid? user, ModularSuitModuleEffectComponent component)
    {
        if (user == null || component.ActiveComponents == null)
            return;

        if (!_inventory.TryGetSlotEntity(user.Value, component.TargetSlot, out var targetEntity))
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

    private void RemoveEffects(EntityUid? user, ModularSuitModuleEffectComponent component)
    {
        if (user == null || component.ActiveComponents == null)
            return;

        if (!_inventory.TryGetSlotEntity(user.Value, component.TargetSlot, out var targetEntity))
            return;

        EntityManager.RemoveComponents(targetEntity.Value, component.ActiveComponents);

        if (component.ReturnedComponents != null)
        {
            EntityManager.AddComponents(targetEntity.Value, component.ReturnedComponents);
        }
    }
}
