using System.Linq;
using Content.Shared.Friendly.Faction;
using Content.Shared.Mobs.Components;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Server.Friendly.Faction
{
    public sealed partial class FriendlyFactionSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<FriendlyFactionComponent, MeleeHitEvent>(OnMeleeHit);
        }

        private void OnMeleeHit(EntityUid uid, FriendlyFactionComponent component, MeleeHitEvent args)
        {
            if (!TryComp<FriendlyFactionComponent>(args.User, out _))
                return;

            if (!args.HitEntities.Any())
                return;

            foreach (var entity in args.HitEntities)
            {
                if (args.User == entity)
                    continue;

                if (!TryComp<MobStateComponent>(entity, out _))
                    continue;

                if (TryComp<FriendlyFactionComponent>(entity, out var friendlyFaction)
                    && friendlyFaction.Faction == component.Faction)
                {
                    args.BonusDamage = -args.BaseDamage;
                }
            }
        }
    }
}
