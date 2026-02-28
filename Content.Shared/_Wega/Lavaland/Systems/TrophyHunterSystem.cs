using Content.Shared.Damage.Components;
using Content.Shared.Lavaland.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Misc.Upgrades;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared.Lavaland;

public sealed partial class TrophyHunterSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly MobThresholdSystem _threshold = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TrophyHuntingToolComponent, MeleeHitEvent>(OnMeleeHit, after: [typeof(CrusherUpgradeEffectsSystem)]);
    }

    private void OnMeleeHit(Entity<TrophyHuntingToolComponent> ent, ref MeleeHitEvent args)
    {
        if (!_net.IsServer)
            return;

        if (args.HitEntities.Count == 0)
            return;

        foreach (var hitEnt in args.HitEntities)
        {
            if (!TryComp<TrophyHunterComponent>(hitEnt, out var trophyComp) || trophyComp.Collected)
                continue;

            if (!TryComp<DamageableComponent>(hitEnt, out var damageable) || damageable.TotalDamage <= 0)
                return;

            if (_threshold.TryGetThresholdForState(hitEnt, MobState.Dead, out var threshold))
            {
                var currentDamage = damageable.TotalDamage.Float();
                var baseDamage = args.BaseDamage.GetTotal().Float();
                var bonusDamage = args.BonusDamage.GetTotal().Float();

                var totalDamage = currentDamage + baseDamage + bonusDamage;

                if (totalDamage < threshold)
                    return;

                trophyComp.Collected = true;
                if (!_random.Prob(trophyComp.DropChance))
                    return;

                var trophy = Spawn(trophyComp.Trophy, Transform(hitEnt).Coordinates);
                _throwing.TryThrow(trophy, _random.NextVector2());
                _audio.PlayPvs(trophyComp.CollectSound, hitEnt);
            }
        }
    }
}
