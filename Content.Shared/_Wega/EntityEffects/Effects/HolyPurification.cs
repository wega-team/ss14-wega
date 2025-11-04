using Content.Shared.Blood.Cult;
using Content.Shared.Blood.Cult.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Deconverts forcibly recruited cultists.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class HolyPurificationEntityEffectSystem : EntityEffectSystem<BloodCultistComponent, HolyPurification>
{
    [Dependency] private readonly SharedBloodCultSystem _bloodCult = default!;

    protected override void Effect(Entity<BloodCultistComponent> entity, ref EntityEffectEvent<HolyPurification> args)
    {
        _bloodCult.CultistDeconvertation(entity);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class HolyPurification : EntityEffectBase<HolyPurification>
{
    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-holy-purification");
}
