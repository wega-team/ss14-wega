using Content.Shared.Flash.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes; // Corvax-Wega-Phantom-Start

namespace Content.Shared.Flash;

public sealed class DamagedByFlashingSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;

    // Corvax-Wega-Phantom-Start
    [ValidatePrototypeId<DamageTypePrototype>]
    private const string Damage = "Heat";
    // Corvax-Wega-Phantom-End

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamagedByFlashingComponent, FlashAttemptEvent>(OnFlashAttempt);
    }

    // TODO: Attempt events should not be doing state changes. But using AfterFlashedEvent does not work because this entity cannot get the status effect.
    // Best wait for Ed's status effect system rewrite.

    private void OnFlashAttempt(Entity<DamagedByFlashingComponent> ent, ref FlashAttemptEvent args)
    {
        // Corvax-Wega-Phantom-Start Rewrite flash damage
        if (!ent.Comp.UseAdvancedFlashDamage)
        {
            _damageable.TryChangeDamage(ent, ent.Comp.FlashDamage);
            return;
        }

        float multiplier = ent.Comp.Multiplier;
        if (TryComp<FlashImmunityComponent>(ent, out var flashImmunity) && flashImmunity.Enabled)
            multiplier *= 0.2f;

        var damage = new DamageSpecifier { DamageDict = { { Damage, (float)args.FlashDuration.TotalSeconds * 2 * multiplier } } };
        _damageable.TryChangeDamage(ent, damage);
        // Corvax-Wega-Phantom-End
    }
}
