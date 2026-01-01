using Content.Server.Xenobiology;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Increases friendship level with slimes.
/// The friendship increase is equal to <see cref="ChemIncreaseFriendship.FriendshipIncrease"/> modified by scale.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class ChemIncreaseFriendshipEntityEffectSystem : EntityEffectSystem<SlimeSocialComponent, ChemIncreaseFriendship>
{
    protected override void Effect(Entity<SlimeSocialComponent> entity, ref EntityEffectEvent<ChemIncreaseFriendship> args)
    {
        var friendshipIncrease = args.Effect.FriendshipIncrease * args.Scale;
        var maxFriendship = args.Effect.MaxFriendship;

        entity.Comp.FriendshipLevel = Math.Min(entity.Comp.FriendshipLevel + friendshipIncrease, maxFriendship);

        entity.Comp.AngryUntil = null;
        entity.Comp.RebellionCooldownEnd = null;

        Dirty(entity);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class ChemIncreaseFriendship : EntityEffectBase<ChemIncreaseFriendship>
{
    /// <summary>
    ///     Amount to increase friendship by.
    /// </summary>
    [DataField]
    public float FriendshipIncrease = 10f;

    /// <summary>
    ///     Maximum friendship level.
    /// </summary>
    [DataField]
    public float MaxFriendship = 100f;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-increase-friendship",
            ("increase", (int)FriendshipIncrease),
            ("max", (int)MaxFriendship));
}
