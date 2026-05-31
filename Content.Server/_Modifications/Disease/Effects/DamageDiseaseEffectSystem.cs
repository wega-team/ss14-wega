// Developed by Nox project.
// Author: KloopRe

using Content.Server._Modifications.Disease.Systems;
using Content.Shared._Modifications.Disease.Components;
using Content.Shared._Modifications.Disease.Effects;
using Content.Shared.EntityEffects;

namespace Content.Server._Modifications.Disease.Effects;

/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class DamageDiseaseEntityEffectsSystem : EntityEffectSystem<DiseaseComponent, DamageDiseaseEffect>
{
    [Dependency] private DiseaseSystem _diseaseSystem = default!;
    protected override void Effect(Entity<DiseaseComponent> entity, ref EntityEffectEvent<DamageDiseaseEffect> args)
    {
        // Масштабируем урон и рост резиста
        var finalDamage = args.Effect.BaseDamage * args.Scale;
        var resistanceIncrease = args.Effect.ResistanceIncrease * args.Scale;

        _diseaseSystem.ApplyMedicineDamage(
            (entity, entity.Comp),
            args.Effect.Key,
            finalDamage,
            resistanceIncrease
        );
    }
}
