using Content.Shared.Clothing;
using Content.Shared.Disease.Components;

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
        if (args.Mask.Comp is not { } maskComp)
            return;

        ent.Comp.IsActive = maskComp.IsToggled;
    }
}
