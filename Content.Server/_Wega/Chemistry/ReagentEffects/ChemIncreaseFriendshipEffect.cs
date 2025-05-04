using Content.Server.Xenobiology;
using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityEffects.Effects
{
    [UsedImplicitly]
    public sealed partial class ChemIncreaseFriendshipEffect : EntityEffect
    {
        [DataField]
        public float FriendshipIncrease = 10f;

        [DataField]
        public float MaxFriendship = 100f;

        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
            => Loc.GetString("reagent-effect-guidebook-increase-friendship",
                ("increase", (int)FriendshipIncrease),
                ("max", (int)MaxFriendship));

        public override void Effect(EntityEffectBaseArgs args)
        {
            if (!args.EntityManager.TryGetComponent<SlimeSocialComponent>(args.TargetEntity, out var social))
                return;

            social.FriendshipLevel = Math.Min(social.FriendshipLevel + FriendshipIncrease, MaxFriendship);

            social.AngryUntil = null;
            social.RebellionCooldownEnd = null;

            args.EntityManager.Dirty(args.TargetEntity, social);
        }
    }
}