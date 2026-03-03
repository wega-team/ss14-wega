using Content.Shared.Clothing.Components;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.Lavaland.Events;

namespace Content.Shared.Clothing;

public sealed class ClothingAshStormProtectionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClothingAshStormProtectionComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<ClothingAshStormProtectionComponent, InventoryRelayedEvent<AshProtectionAttemptEvent>>(OnGetModifier);
    }

    private void OnExamined(Entity<ClothingAshStormProtectionComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("clothing-ash-protection-examined", ("modifier", ent.Comp.Modifier * 100)));
    }

    private void OnGetModifier(Entity<ClothingAshStormProtectionComponent> ent, ref InventoryRelayedEvent<AshProtectionAttemptEvent> args)
    {
        if (ent.Comp.Modifier <= 0f)
            return;

        args.Args.Modifier += ent.Comp.Modifier;
    }
}
