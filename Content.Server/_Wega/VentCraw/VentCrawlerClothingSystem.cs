using Content.Shared.Inventory.Events;
using Content.Shared.VentCraw;

namespace Content.Server.VentCraw;

public sealed partial class VentCrawlerClothingSystem : EntitySystem
{

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VentCrawlerClothingComponent, GotEquippedEvent>(OnDidEquip);
        SubscribeLocalEvent<VentCrawlerClothingComponent, GotUnequippedEvent>(OnDidUnequip);
    }
    
    private void OnDidEquip(Entity<VentCrawlerClothingComponent> ent, ref GotEquippedEvent args)
    {
        if (HasComp<VentCrawlerComponent>(args.EquipTarget))
        {
            ent.Comp.AlreadyHas = true;
            return;
        }

        ent.Comp.AlreadyHas = false;
        EnsureComp<VentCrawlerComponent>(args.EquipTarget);
    }
    
    private void OnDidUnequip(Entity<VentCrawlerClothingComponent> ent, ref GotUnequippedEvent args)
    {
        if (ent.Comp.AlreadyHas)
            return;

        RemComp<VentCrawlerComponent>(args.EquipTarget);
    }
}