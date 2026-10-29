using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared.Feroxi;

public sealed partial class FeroxiVaporHealEntityEffectSystem : EntityEffectSystem<DamageableComponent, FeroxiVaporHeal>
{
    [Dependency] private DamageableSystem _damageable = default!;

    protected override void Effect(Entity<DamageableComponent> entity, ref EntityEffectEvent<FeroxiVaporHeal> args)
    {
        var effect = args.Effect;
        var absorbed = effect.Saturation * (1f - MathF.Exp(-args.Scale / effect.Saturation));

        _damageable.HealEvenly(entity.AsNullable(), -effect.Heal * absorbed, effect.Group);
    }
}

public sealed partial class FeroxiVaporHeal : EntityEffectBase<FeroxiVaporHeal>
{
    [DataField]
    public ProtoId<DamageGroupPrototype> Group = "Brute";

    [DataField]
    public float Heal = 0.15f;

    [DataField]
    public float Saturation = 5f;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("entity-effect-guidebook-feroxi-vapor-heal",
            ("chance", Probability),
            ("group", prototype.Index(Group).LocalizedName),
            ("max", MathF.Round(Heal * Saturation, 2)));
    }
}
