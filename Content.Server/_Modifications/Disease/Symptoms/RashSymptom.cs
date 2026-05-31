// Developed by Nox project.
// Author: KloopRe

using Content.Server.Chat.Systems;
using Content.Shared._Modifications.Disease.Components;
using Content.Shared._Modifications.TimeWindow;
using Content.Shared.Chat;
using Content.Shared._Modifications.Disease.Symptoms;
using Content.Server._Modifications.Disease.Systems;

namespace Content.Server._Modifications.Disease.Symptoms;

[DiseaseSymptom("RashSymptom")]
public sealed partial class RashSymptom : DiseaseSymptomBase
{
    [Dependency] private EntityManager _entityManager = default!;
    private const string RashEmote = "чешется";

    public RashSymptom(TimedWindow effectTimedWindow) : base(effectTimedWindow)
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

        chatSystem.TrySendInGameICMessage(host,
                            RashEmote,
                            InGameICChatType.Emote,
                            ChatTransmitRange.Normal);

        diseaseInfectionCloudSystem.TrySpawnCloud((host, disease), out _);
    }

    public override void ApplyDataEffect(DiseaseData data, bool add)
    {
        base.ApplyDataEffect(data, add);
    }

    public override IDiseaseSymptom Clone()
    {
        return new RashSymptom(EffectTimedWindow.Clone());
    }
}
