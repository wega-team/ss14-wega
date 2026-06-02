using Content.Shared.Blood.Cult;
using Content.Shared.Blood.Cult.Components;
using Content.Shared.Veil.Cult;
using Content.Shared.Veil.Cult.Components;
using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Deconverts forcibly recruited cultists.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class HolyPurificationEntityEffectSystem : EntityEffectSystem<HumanoidProfileComponent, HolyPurification>
{
    [Dependency] private SharedBloodCultSystem _bloodCult = default!;
    [Dependency] private SharedVeilCultSystem _veilCult = default!;

    protected override void Effect(Entity<HumanoidProfileComponent> entity, ref EntityEffectEvent<HolyPurification> args)
    {
        if (HasComp<BloodCultistComponent>(entity.Owner))
            _bloodCult.CultistDeconvertation(entity);

        if (HasComp<VeilCultistComponent>(entity.Owner))
            _veilCult.CultistDeconvertation(entity);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class HolyPurification : EntityEffectBase<HolyPurification>
{
    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-holy-purification");
}
