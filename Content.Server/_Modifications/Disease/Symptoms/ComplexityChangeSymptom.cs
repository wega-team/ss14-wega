// Developed by Nox project.
// Author: KloopRe

using Content.Shared._Modifications.Disease.Components;
using Content.Shared._Modifications.TimeWindow;
using Content.Shared._Modifications.Disease.Symptoms;

namespace Content.Server._Modifications.Disease.Symptoms;

[DiseaseSymptom("ComplexityChangeSymptom")]
public sealed class ComplexityChangeSymptom : DiseaseSymptomBase
{
    private int _addMultiPriceDeleteSymptom = 2;

    public ComplexityChangeSymptom(TimedWindow effectTimedWindow) : base(effectTimedWindow)
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
    {

    }

    public override IDiseaseSymptom Clone()
    {
        return new ComplexityChangeSymptom(EffectTimedWindow.Clone());
    }

    public override void ApplyDataEffect(DiseaseData data, bool add)
    {
        base.ApplyDataEffect(data, add);
        if (add)
            data.MultiPriceDeleteSymptom += _addMultiPriceDeleteSymptom;
        else
            data.MultiPriceDeleteSymptom -= _addMultiPriceDeleteSymptom;
    }
}
