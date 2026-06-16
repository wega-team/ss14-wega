using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Genetics;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Vampire.Components;

namespace Content.Server.Genetics.System;

public sealed partial class SpeedUpGenSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private MovementSpeedModifierSystem _speed = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpeedUpGenComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SpeedUpGenComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SpeedUpGenComponent, DamageChangedEvent>(OnDamageChanged);
    }

    private void OnInit(Entity<SpeedUpGenComponent> ent, ref ComponentInit args)
    {
        if (HasComp<VampireComponent>(ent))
            return;

        if (TryComp<MovementSpeedModifierComponent>(ent, out var speed))
        {
            var originalWalkSpeed = speed.BaseWalkSpeed;
            var originalSprintSpeed = speed.BaseSprintSpeed;
            _speed.ChangeBaseSpeed(ent, originalWalkSpeed * ent.Comp.SpeedModifier, originalSprintSpeed * ent.Comp.SpeedModifier, speed.Acceleration, speed);
        }
    }

    private void OnShutdown(Entity<SpeedUpGenComponent> ent, ref ComponentShutdown args)
    {
        if (HasComp<VampireComponent>(ent))
            return;

        if (TryComp<MovementSpeedModifierComponent>(ent, out var speed))
        {
            var originalWalkSpeed = speed.BaseWalkSpeed;
            var originalSprintSpeed = speed.BaseSprintSpeed;
            _speed.ChangeBaseSpeed(ent, originalWalkSpeed / ent.Comp.SpeedModifier, originalSprintSpeed / ent.Comp.SpeedModifier, speed.Acceleration, speed);
        }
    }

    private void OnDamageChanged(Entity<SpeedUpGenComponent> ent, ref DamageChangedEvent args)
    {
		if (ent.Comp.DamageBooster)		
		{
			if (args.DamageDelta is null || IsNegativeDamage(args.DamageDelta))
				return;

			var bonusDamage = args.DamageDelta * 0.2f;
			_damageable.TryChangeDamage(ent.Owner, bonusDamage, true);
		}
    }

    private bool IsNegativeDamage(DamageSpecifier damage)
    {
        foreach (var type in damage.DamageDict)
        {
            if (type.Value > 0)
                return false;
        }
        return true;
    }
}
