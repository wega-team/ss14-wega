using Content.Shared._Wega.Implants.Components;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Robust.Shared.Containers;

namespace Content.Server._Wega.Implants;

public sealed partial class ComponentsImplantAddSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ComponentsImplantAddComponent, ImplantImplantedEvent>(OnImplanted);
        SubscribeLocalEvent<ComponentsImplantAddComponent, ImplantRemovedEvent>(OnRemoved);
    }

    private void OnImplanted(Entity<ComponentsImplantAddComponent> ent, ref ImplantImplantedEvent args)
    {
        // В этой версии args.Implanted — это обычный EntityUid
        EntityManager.AddComponents(args.Implanted, ent.Comp.Components);
    }

    private void OnRemoved(Entity<ComponentsImplantAddComponent> ent, ref ImplantRemovedEvent args)
    {
        // У ImplantRemovedEvent носитель хранится в args.Implanted
        var wearer = args.Implanted;

        if (!HasImplantWithKey(wearer, ent.Comp.ImplantKey))
        {
            EntityManager.RemoveComponents(wearer, ent.Comp.Components);
        }
    }

    private bool HasImplantWithKey(EntityUid wearerUid, string? key)
    {
        if (key == null)
            return false;

        if (!_container.TryGetContainer(wearerUid, ImplanterComponent.ImplantSlotId, out var container))
            return false;

        foreach (var implant in container.ContainedEntities)
        {
            if (TryComp<ComponentsImplantAddComponent>(implant, out var comp) && comp.ImplantKey == key)
                return true;
        }

        return false;
    }
}
