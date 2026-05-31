// Developed by Nox project.
// Author: KloopRe

using Content.Shared._Modifications.Disease.Components;
using Content.Shared._Modifications.TimeWindow;
using Content.Shared._Modifications.Disease.Symptoms;

namespace Content.Server._Modifications.Disease.Symptoms;

[DiseaseSymptom("AnimalInfestationSymptom")]
public sealed class AnimalInfestationSymptom : DiseaseSymptomBase
{
    public AnimalInfestationSymptom(TimedWindow effectTimedWindow) : base(effectTimedWindow)
    { }

    public override void OnAdded(EntityUid host, DiseaseComponent disease)
    {
        base.OnAdded(host, disease);
    }

    public override void OnRemoved(EntityUid host, DiseaseComponent disease)
    {
        base.OnRemoved(host, disease);
    }

    public override void OnUpdate(EntityUid host, DiseaseComponent disease)
    {
        base.OnUpdate(host, disease);
    }

    public override void DoEffect(EntityUid host, DiseaseComponent disease)
    { }

    public override void ApplyDataEffect(DiseaseData data, bool add)
    {
        base.ApplyDataEffect(data, add);
    }

    public override IDiseaseSymptom Clone()
    {
        return new AnimalInfestationSymptom(EffectTimedWindow.Clone());
    }
}
