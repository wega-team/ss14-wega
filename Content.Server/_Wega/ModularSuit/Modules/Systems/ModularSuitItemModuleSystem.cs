using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Components;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Modular.Suit;
using Robust.Shared.Containers;

namespace Content.Server.Modular.Suit;

public sealed class ModularSuitItemModuleSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ModularSuitItemModuleComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ModularSuitItemModuleComponent, EntityTerminatingEvent>(OnTerminating);
        SubscribeLocalEvent<ModularSuitItemModuleComponent, ModularSuitInstalledEvent>(OnModuleInstalled);
        SubscribeLocalEvent<ModularSuitItemModuleComponent, ModularSuitRemovedEvent>(OnModuleRemoved);
        SubscribeLocalEvent<ModularSuitItemModuleComponent, ModularSuitModuleToggledEvent>(OnModuleToggled);
    }

    private void OnMapInit(Entity<ModularSuitItemModuleComponent> ent, ref MapInitEvent args)
    {
        _container.EnsureContainer<ContainerSlot>(ent.Owner, ent.Comp.ContainerId);

        var item = Spawn(ent.Comp.ItemPrototype, Transform(ent.Owner).Coordinates);
        if (!_container.Insert(item, _container.GetContainer(ent.Owner, ent.Comp.ContainerId)))
        {
            QueueDel(item);
            return;
        }
        else
        {
            var ev = new ModularSuitModuleItemCreatedEvent(ent.Owner);
            RaiseLocalEvent(item, ref ev);
        }
    }

    private void OnTerminating(Entity<ModularSuitItemModuleComponent> ent, ref EntityTerminatingEvent args)
    {
        if (!_container.TryGetContainer(ent.Owner, ent.Comp.ContainerId, out var container))
            return;

        foreach (var item in container.ContainedEntities)
            QueueDel(item);
    }

    private void OnModuleInstalled(Entity<ModularSuitItemModuleComponent> module, ref ModularSuitInstalledEvent args)
    {
        if (TryComp<ModularSuitModuleComponent>(module.Owner, out var moduleComp) && moduleComp.IsActive)
            TryAddHand(args.Suit, module);
    }

    private void OnModuleRemoved(Entity<ModularSuitItemModuleComponent> module, ref ModularSuitRemovedEvent args)
    {
        TryRemoveHand(args.Suit, module);
    }

    private void OnModuleToggled(Entity<ModularSuitItemModuleComponent> module, ref ModularSuitModuleToggledEvent args)
    {
        if (args.Activated)
        {
            TryAddHand(args.Suit, module);
        }
        else
        {
            TryRemoveHand(args.Suit, module);
        }
    }

    private void TryAddHand(EntityUid suit, Entity<ModularSuitItemModuleComponent> module)
    {
        if (!TryComp<ModularSuitComponent>(suit, out var suitComp) || suitComp.Wearer == null)
            return;

        var wearer = suitComp.Wearer.Value;
        if (!HasComp<HandsComponent>(wearer))
            return;

        var container = _container.GetContainer(module.Owner, module.Comp.ContainerId);
        if (container.ContainedEntities.Count == 0)
            return;

        var item = container.ContainedEntities[0];
        _hands.AddHand(wearer, module.Comp.HandId, HandLocation.Middle);

        module.Comp.HandItem = item;
        _hands.TryForcePickup(wearer, item, module.Comp.HandId, false, false);
        EnsureComp<UnremoveableComponent>(item);
    }

    private void TryRemoveHand(EntityUid suit, Entity<ModularSuitItemModuleComponent> module)
    {
        if (!TryComp<ModularSuitComponent>(suit, out var suitComp) || suitComp.Wearer == null)
            return;

        var wearer = suitComp.Wearer.Value;
        if (module.Comp.HandItem != null)
        {
            var item = module.Comp.HandItem.Value;

            _toggle.TryDeactivate(item);
            RemComp<UnremoveableComponent>(item);
            var container = _container.GetContainer(module.Owner, module.Comp.ContainerId);
            _container.Insert(item, container);

            module.Comp.HandItem = null;
        }

        _hands.RemoveHand(wearer, module.Comp.HandId);
    }
}
