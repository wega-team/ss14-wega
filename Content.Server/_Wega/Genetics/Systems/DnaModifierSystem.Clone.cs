using Content.Shared.DetailExaminable;
using Content.Shared.Genetics;
using Content.Shared.Humanoid;

namespace Content.Server.Genetics.System;

public sealed partial class DnaModifierSystem
{
    public bool TryCloneHumanoid(Entity<DnaModifierComponent> entity, Entity<DnaModifierComponent> target)
    {
        if (target.Comp.UniqueIdentifiers == null)
            return false;

        CloneHumanoid(entity, target);

        return true;
    }

    private void CloneHumanoid(Entity<DnaModifierComponent> entity, Entity<DnaModifierComponent> target,
        HumanoidAppearanceComponent? humanoid = null, HumanoidAppearanceComponent? targetHumanoid = null)
    {
        if (!Resolve(entity, ref humanoid) || !Resolve(target, ref targetHumanoid))
            return;

        if (target.Comp.UniqueIdentifiers == null)
            return;

        humanoid.Species = targetHumanoid.Species;
        entity.Comp.UniqueIdentifiers = CloneUniqueIdentifiers(target.Comp.UniqueIdentifiers);
        if (TryComp<DetailExaminableComponent>(entity, out var detail))
        {
            detail.Content = "";
            if (TryComp<DetailExaminableComponent>(target, out var targetDetail))
                detail.Content = targetDetail.Content;
        }

        Dirty(entity, entity.Comp);

        TryChangeUniqueIdentifiers(entity);
    }
}
