using Content.Shared.Inventory;

namespace Content.Shared.Clothing;

public sealed class ClothingFrictionIgnoreSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ClothingFrictionIgnoreComponent, InventoryRelayedEvent<ClothingFrictionModifierEvent>>(OnGetModifier);
    }

    private void OnGetModifier(Entity<ClothingFrictionIgnoreComponent> ent, ref InventoryRelayedEvent<ClothingFrictionModifierEvent> args)
    {
        var comp = ent.Comp;
        if (comp.IgnoreFriction)
        {
            args.Args.FrictionModifier = comp.OverrideFriction ?? 1f;
            args.Args.FrictionNoInputModifier = comp.OverrideFriction ?? 1f;
        }

        if (comp.IgnoreAcceleration)
        {
            args.Args.AccelerationModifier = comp.OverrideAcceleration ?? 1f;
        }
    }
}
