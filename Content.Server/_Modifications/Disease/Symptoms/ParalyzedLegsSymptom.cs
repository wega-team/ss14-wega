// Developed by Nox project.
// Author: KloopRe

using Content.Shared._Modifications.Disease.Components;
using Content.Shared._Modifications.TimeWindow;
using Content.Shared.Movement.Components;
using Content.Shared._Modifications.Disease.Symptoms;

namespace Content.Server._Modifications.Disease.Symptoms;

[DiseaseSymptom("ParalyzedLegsSymptom")]
public sealed partial class ParalyzedLegsSymptom : DiseaseSymptomBase
{
    [Dependency] private EntityManager _entityManager = default!;
    private bool _hasComp = false;

    public ParalyzedLegsSymptom(TimedWindow effectTimedWindow) : base(effectTimedWindow)
    { }

    public override void OnAdded(EntityUid host, DiseaseComponent disease)
    {
        base.OnAdded(host, disease);

        if (_entityManager.HasComponent<WormComponent>(host))
            _hasComp = true;
        else
            _entityManager.AddComponent<WormComponent>(host);
    }

    public override void OnRemoved(EntityUid host, DiseaseComponent disease)
    {
        base.OnRemoved(host, disease);

        if (!_hasComp && _entityManager.HasComponent<WormComponent>(host))
            _entityManager.RemoveComponent<WormComponent>(host);
    }

    public override void OnUpdate(EntityUid host, DiseaseComponent disease)
    {
        base.OnUpdate(host, disease);
    }

    public override void DoEffect(EntityUid host, DiseaseComponent disease)
    {

    }

    public override void ApplyDataEffect(DiseaseData data, bool add)
    {
        base.ApplyDataEffect(data, add);
    }

    public override IDiseaseSymptom Clone()
    {
        return new ParalyzedLegsSymptom(EffectTimedWindow.Clone());
    }
}
