using Content.Shared.Xenobiology;

namespace Content.Server.Xenobiology;

[RegisterComponent, Access(typeof(SlimeExtractorSystem))]
public sealed partial class SlimeExtractorComponent : Component
{
    [DataField("processingTimePerUnitMass")]
    public float ProcessingTimePerUnitMass = 1.5f;

    [DataField("randomMessInterval")]
    public float RandomMessInterval = 4f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float ProcessingTimer;

    [ViewVariables(VVAccess.ReadWrite)]
    public float RandomMessTimer;

    [ViewVariables]
    public bool IsActive;

    [ViewVariables]
    public string? SlimeType;

    [ViewVariables]
    public SlimeStage? SlimeStage;

    public bool Reinforced = false;

    [ViewVariables]
    public string? BloodReagent;
}
