using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Damage;

public sealed class DamageResistSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageResistComponent, DamageChangedEvent>(OnDamageChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DamageResistComponent>();
        while (query.MoveNext(out var uid, out var resist))
        {
            var toRemove = new List<DamageTypePrototype>();
            foreach (var (type, (_, endTime)) in resist.Resistances)
            {
                if (_gameTiming.CurTime >= endTime)
                    toRemove.Add(type);
            }

            foreach (var type in toRemove)
            {
                resist.Resistances.Remove(type);
            }

            if (resist.Resistances.Count == 0)
                RemComp<DamageResistComponent>(uid);
            else
                Dirty(uid, resist);
        }
    }

    private void OnDamageChanged(Entity<DamageResistComponent> ent, ref DamageChangedEvent args)
    {
        if (args.DamageDelta is null || IsHealing(args.DamageDelta))
            return;

        var healing = new DamageSpecifier();
        foreach (var (type, delta) in args.DamageDelta.DamageDict)
        {
            if (!_prototype.TryIndex<DamageTypePrototype>(type, out var damageProto))
                continue;

            if (ent.Comp.Resistances.TryGetValue(damageProto, out var resist))
            {
                var healAmount = delta * resist.ResistFactor;
                healing.DamageDict.Add(damageProto.ID, -healAmount);
            }
        }

        if (healing.DamageDict.Count > 0)
            _damageable.TryChangeDamage(ent, healing, true);
    }

    private bool IsHealing(DamageSpecifier damage)
    {
        foreach (var (_, delta) in damage.DamageDict)
        {
            if (delta > 0)
                return false;
        }
        return true;
    }
}