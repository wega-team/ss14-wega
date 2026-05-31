// Developed by Nox project.
// Author: KloopRe

using Content.Server._Modifications.Disease.Components;
using Content.Shared.Buckle.Components;
using Content.Shared._Modifications.Disease.Components;
using Content.Shared._Modifications.Disease;

namespace Content.Server._Modifications.Disease.Systems;

public sealed class DiseaseHealingBedSystem : EntitySystem
{

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DiseaseHealingBedComponent, StrappedEvent>(OnStrapped);
        SubscribeLocalEvent<DiseaseHealingBedComponent, UnstrappedEvent>(OnUnstrapped);
    }

    private void OnStrapped(Entity<DiseaseHealingBedComponent> bed, ref StrappedEvent args)
    {
        if (TryComp<DiseaseComponent>(args.Buckle, out var diseaseComponent))
            diseaseComponent.RegenerationType = bed.Comp.RegenerationType;
    }

    private void OnUnstrapped(Entity<DiseaseHealingBedComponent> bed, ref UnstrappedEvent args)
    {
        if (TryComp<DiseaseComponent>(args.Buckle, out var diseaseComponent))
            diseaseComponent.RegenerationType = DiseaseHealingBedType.None;
    }
}
