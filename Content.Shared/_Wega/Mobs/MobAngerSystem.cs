using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Anger.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared.Mobs.Anger;

public sealed class MobAngerSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speed = default!;
    [Dependency] private readonly MobThresholdSystem _threshold = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MobAngerComponent, AttackedEvent>(OnAttacked);
        SubscribeLocalEvent<MobAngerComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnAttacked(EntityUid uid, MobAngerComponent comp, AttackedEvent args)
    {
        if (comp.Anger)
            return;

        if (!TryComp<DamageableComponent>(uid, out var damageable))
            return;

        var totalDamage = _damageable.GetTotalDamage((uid, damageable));
        if (_threshold.TryGetThresholdForState(uid, MobState.Dead, out var threshold) && totalDamage > 0)
        {
            if (totalDamage >= threshold - threshold * comp.AngerThreshold)
            {
                comp.Anger = true;
                if (TryComp(uid, out MovementSpeedModifierComponent? speed))
                {
                    _speed.ChangeBaseSpeed(uid, speed.BaseWalkSpeed * comp.SpeedModifier, speed.BaseSprintSpeed * comp.SpeedModifier,
                        speed.Acceleration);
                }
            }
        }
    }

    private void OnMeleeHit(Entity<MobAngerComponent> ent, ref MeleeHitEvent args)
    {
        if (!ent.Comp.Anger)
            return;

        args.BonusDamage = args.BaseDamage * ent.Comp.DamageModifier;
    }
}
