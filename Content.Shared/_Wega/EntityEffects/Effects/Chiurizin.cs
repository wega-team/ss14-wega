using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Main Chiurizin metabolism effect. Handles cycle-based healing, stasis traps, position saving, and reagent purging.
/// Requires at least 25 units in the solution to activate (enforced by ReagentCondition in YAML).
/// </summary>
public sealed partial class Chiurizin : EntityEffectBase<Chiurizin>
{
    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => null;
}

/// <summary>
/// Chiurizin overdose effect. Applied when the reagent quantity exceeds 30 units.
/// Handles aging, knockdown, teleportation, and extreme effects at age 100+.
/// </summary>
public sealed partial class ChiurizinOverdose : EntityEffectBase<ChiurizinOverdose>
{
    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => null;
}
