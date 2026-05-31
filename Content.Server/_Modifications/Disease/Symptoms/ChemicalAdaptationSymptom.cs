// Developed by Nox project.
// Author: KloopRe

using Content.Shared._Modifications.Disease.Components;
using Content.Shared._Modifications.TimeWindow;
using Content.Shared._Modifications.Disease.Symptoms;

namespace Content.Server._Modifications.Disease.Symptoms;

[DiseaseSymptom("ChemicalAdaptationSymptom")]
public sealed class ChemicalAdaptationSymptom : DiseaseSymptomBase
{
    private float _addDefaultMedicineResistance = 0.2f;

    public ChemicalAdaptationSymptom(TimedWindow effectTimedWindow) : base(effectTimedWindow)
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

    public override IDiseaseSymptom Clone()
    {
        return new ChemicalAdaptationSymptom(EffectTimedWindow.Clone());
    }

    public override void ApplyDataEffect(DiseaseData data, bool add)
    {
        base.ApplyDataEffect(data, add);
        if (add)
            data.DefaultMedicineResistance += _addDefaultMedicineResistance;
        else
            data.DefaultMedicineResistance -= _addDefaultMedicineResistance;
    }
}
