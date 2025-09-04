using Content.Shared.Shadow.Components;
using Content.Shared.Damage;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;

namespace Content.Shared.Shadow;

public sealed class ShadowWeaknessSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speed = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShadowWeaknessComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ShadowWeaknessComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ShadowWeaknessComponent, DamageChangedEvent>(OnDamageChanged);
    }

    private void OnInit(Entity<ShadowWeaknessComponent> ent, ref ComponentInit args)
    {

        if (TryComp<MovementSpeedModifierComponent>(ent, out var speed))
        {
            var originalWalkSpeed = speed.BaseWalkSpeed;
            var originalSprintSpeed = speed.BaseSprintSpeed;
            _speed.ChangeBaseSpeed(ent, originalWalkSpeed * ent.Comp.SpeedModifier, originalSprintSpeed * ent.Comp.SpeedModifier, speed.Acceleration, speed);
        }
    }

    private void OnShutdown(Entity<ShadowWeaknessComponent> ent, ref ComponentShutdown args)
    {
        if (TryComp<MovementSpeedModifierComponent>(ent, out var speed))
        {
            var originalWalkSpeed = speed.BaseWalkSpeed;
            var originalSprintSpeed = speed.BaseSprintSpeed;
            _speed.ChangeBaseSpeed(ent, originalWalkSpeed / ent.Comp.SpeedModifier, originalSprintSpeed / ent.Comp.SpeedModifier, speed.Acceleration, speed);
        }
    }

    private void OnDamageChanged(Entity<ShadowWeaknessComponent> ent, ref DamageChangedEvent args)
    {
        if (args.DamageDelta is null || IsNegativeDamage(args.DamageDelta))
            return;

        var bonusDamage = args.DamageDelta * ent.Comp.DamageModfier;
        _damageable.TryChangeDamage(ent, bonusDamage, true);
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
