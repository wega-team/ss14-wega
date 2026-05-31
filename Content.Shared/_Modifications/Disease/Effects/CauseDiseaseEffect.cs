// Developed by Nox project.
// Author: KloopRe

using Content.Shared._Modifications.Disease.Components;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._Modifications.Disease.Effects;

public sealed partial class CauseDiseaseEffect : EntityEffectBase<CauseDiseaseEffect>
{
    [DataField]
    public DiseaseData Data = new();

    [DataField]
    public string Solution = "bloodstream";

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-cause-disease");
}
