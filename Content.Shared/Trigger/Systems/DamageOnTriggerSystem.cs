using Content.Shared.Damage;
using Content.Shared.Trigger.Components.Effects;
using Content.Shared.Whitelist; // Corvax-Wega-Lavaland

namespace Content.Shared.Trigger.Systems;

public sealed class DamageOnTriggerSystem : XOnTriggerSystem<DamageOnTriggerComponent>
{
    [Dependency] private readonly Damage.Systems.DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!; // Corvax-Wega-Lavaland

    protected override void OnTrigger(Entity<DamageOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        // Corvax-Wega-Lavaland-start
        if (_entityWhitelist.IsWhitelistPass(ent.Comp.Blacklist, target))
            return;
        // Corvax-Wega-Lavaland-end

        var damage = new DamageSpecifier(ent.Comp.Damage);
        var ev = new BeforeDamageOnTriggerEvent(damage, target);
        RaiseLocalEvent(ent.Owner, ref ev);

        args.Handled |= _damageableSystem.TryChangeDamage(target, ev.Damage, ent.Comp.IgnoreResistances, origin: ent.Owner);
    }
}

/// <summary>
/// Raised on an entity before it deals damage using DamageOnTriggerComponent.
/// Used to modify the damage that will be dealt.
/// </summary>
[ByRefEvent]
public record struct BeforeDamageOnTriggerEvent(DamageSpecifier Damage, EntityUid Tripper);
