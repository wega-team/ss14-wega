using Content.Shared.Veil.Cult;
using Content.Shared.Veil.Cult.Components;
using Content.Shared.Blood.Cult;
using Content.Shared.Blood.Cult.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Deconverts forcibly recruited cultists.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class HolyPurificationEntityEffectSystem : EntityEffectSystem<HolyPurification>
{
    [Dependency] private readonly SharedBloodCultSystem _bloodCult = default!;
    [Dependency] private readonly SharedVeilCultSystem _veilCult = default!;

    protected override void Effect(EntityUid entity, ref EntityEffectEvent<HolyPurification> args)
    {
        if (HasComp<BloodCultistComponent>(entity))
            _bloodCult.CultistDeconvertation(entity);
            
        if (HasComp<VeilCultistComponent>(entity))
            _veilCult.CultistDeconvertation(entity);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class HolyPurification : EntityEffectBase<HolyPurification>
{
    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-holy-purification");
}
