using Robust.Shared.Random;

namespace Content.Shared.Interaction;

/// <summary>
/// An effect that applies a set of other effects with a given probability.
/// Supports target (user/target/both), additional effects on failure,
/// and the "roll each" flag for independent rolls for each effect.
/// </summary>
[Serializable]
[DataDefinition]
public sealed partial class ProbEffect : InteractionEffect
{
    /// <summary>
    /// Probability (0..1) of a successful throw.
    /// </summary>
    [DataField("prob", required: true)]
    public float Probability { get; private set; } = 1f;

    /// <summary>
    /// A list of effects that apply when a throw is successful.
    /// </summary>
    [DataField(required: true)]
    public List<InteractionEffect> Effects { get; private set; }

    /// <summary>
    /// Where to apply nested effects: to the user, to the target, or to both. <see cref="EffectTarget"/>
    /// </summary>
    [DataField("target")]
    public EffectTarget EffectTarget { get; private set; } = EffectTarget.Target;

    /// <summary>
    /// If true, a separate roll is made for each effect.
    /// Otherwise, a single roll is made for the entire list of Effects.
    /// </summary>
    [DataField]
    public bool RollEach { get; private set; }

    /// <summary>
    /// Effects that are applied when a throw fails.
    /// </summary>
    [DataField]
    public List<InteractionEffect>? FailEffects { get; private set; }

    public override void Apply(EntityUid user, EntityUid target, IEntityManager entityManager)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        var effectTargets = GetEffectTargets(user, target, EffectTarget);

        if (RollEach)
        {
            foreach (var effect in Effects)
            {
                if (random.Prob(Probability))
                {
                    ApplyEffectToTargets(effect, user, effectTargets, entityManager);
                }
                else if (FailEffects != null)
                {
                    foreach (var failEffect in FailEffects)
                    {
                        ApplyEffectToTargets(failEffect, user, effectTargets, entityManager);
                    }
                }
            }
        }
        else
        {
            if (random.Prob(Probability))
            {
                foreach (var effect in Effects)
                {
                    ApplyEffectToTargets(effect, user, effectTargets, entityManager);
                }
            }
            else if (FailEffects != null)
            {
                foreach (var failEffect in FailEffects)
                {
                    ApplyEffectToTargets(failEffect, user, effectTargets, entityManager);
                }
            }
        }
    }

    private List<EntityUid> GetEffectTargets(EntityUid user, EntityUid target, EffectTarget effectTarget)
    {
        var targets = new List<EntityUid>();

        switch (effectTarget)
        {
            case EffectTarget.User:
                targets.Add(user);
                break;
            case EffectTarget.Target:
                targets.Add(target);
                break;
            case EffectTarget.Both:
                targets.Add(user);
                targets.Add(target);
                break;
        }

        return targets;
    }

    private void ApplyEffectToTargets(InteractionEffect effect, EntityUid user, List<EntityUid> targets, IEntityManager entityManager)
    {
        foreach (var effectTarget in targets)
        {
            effect.Apply(user, effectTarget, entityManager);
        }
    }
}
