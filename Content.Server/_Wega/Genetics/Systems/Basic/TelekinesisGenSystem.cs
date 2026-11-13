using Content.Server.Hands.Systems;
using Content.Shared.Genetics;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction.Components;

namespace Content.Server.Genetics.System;

public sealed partial class TelekinesisGenSystem : EntitySystem
{
    [Dependency] private HandsSystem _hands = default!;

    [SubscribeLocalEvent]
    private void OnTelekinesisStartup(EntityUid uid, TelekinesisGenComponent component, ComponentStartup args)
    {
        if (!HasComp<HandsComponent>(uid))
            return;

        _hands.AddHand(uid, component.HandId, component.HandPos);

        var coords = Transform(uid).Coordinates;
        var item = Spawn(component.ItemPrototype, coords);
        component.TelekinesisItem = item;

        if (_hands.TryPickup(uid, item, component.HandId, checkActionBlocker: false))
            EnsureComp<UnremoveableComponent>(item);
    }

    [SubscribeLocalEvent]
    private void OnTelekinesisShutdown(Entity<TelekinesisGenComponent> entity, ref ComponentShutdown args)
    {
        if (entity.Comp.TelekinesisItem is { Valid: true } item)
            QueueDel(item);

        _hands.RemoveHand(entity.Owner, entity.Comp.HandId);
    }
}
