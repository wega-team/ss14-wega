using Content.Server.Disease.Components;
using Content.Shared.Clothing;

namespace Content.Server.Nutrition.EntitySystems;

public sealed class DiseaseProtectionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DiseaseProtectionComponent, ItemMaskToggledEvent>(OnMaskToggled);
    }

    private void OnMaskToggled(Entity<DiseaseProtectionComponent> ent, ref ItemMaskToggledEvent args)
    {
        ent.Comp.IsActive = !args.IsToggled;
    }
}
