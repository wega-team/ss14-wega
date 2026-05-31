// Developed by Nox project.
// Author: KloopRe

using Content.Server.Chat.Systems;
using Content.Shared._Modifications.Disease.Components;
using Content.Server._Modifications.Disease.Systems;
using Content.Shared.Chat.Prototypes;
using Content.Shared._Modifications.TimeWindow;
using Robust.Shared.Prototypes;
using Content.Shared.Chat;
using Content.Shared._Modifications.Disease.Symptoms;
using Robust.Shared.Physics;

namespace Content.Server._Modifications.Disease.Symptoms;

[DiseaseSymptom("CoughSymptom")]
public sealed partial class CoughSymptom : DiseaseSymptomBase
{
    [Dependency] private EntityManager _entityManager = default!;
    private static readonly ProtoId<EmotePrototype> CoughEmote = "Cough";

    public CoughSymptom(TimedWindow effectTimedWindow) : base(effectTimedWindow)
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
        var chatSystem = _entityManager.System<ChatSystem>();
        var diseaseInfectionCloudSystem = _entityManager.System<DiseaseInfectionCloudSystem>();

        // Почему-то проигрывается вместе со звуком
        chatSystem.TryEmoteWithChat(host,
                            CoughEmote,
                            ChatTransmitRange.HideChat,
                            ignoreActionBlocker: true);

        diseaseInfectionCloudSystem.TrySpawnCloud((host, disease), out _);
    }

    public override void ApplyDataEffect(DiseaseData data, bool add)
    {
        base.ApplyDataEffect(data, add);
    }

    public override IDiseaseSymptom Clone()
    {
        return new CoughSymptom(EffectTimedWindow.Clone());
    }
}
