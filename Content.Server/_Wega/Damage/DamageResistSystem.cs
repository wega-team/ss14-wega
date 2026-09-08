using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Robust.Shared.Timing;

namespace Content.Server.Damage;

public sealed partial class DamageResistSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageResistComponent, DamageDealtEvent>(OnDamageChanged);
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

    private void OnDamageChanged(Entity<DamageResistComponent> ent, ref DamageDealtEvent args)
    {
        if (args.Damage.GetTotal() <= 0)
            return;

        var healing = new DamageSpecifier();
        foreach (var (type, amount) in args.Damage.DamageDict)
        {
            if (!ProtoMan.TryIndex(type, out var damageProto))
                continue;

            if (ent.Comp.Resistances.TryGetValue(damageProto, out var resist))
            {
                var healAmount = amount * resist.ResistFactor;
                healing.DamageDict.Add(damageProto.ID, -healAmount);
            }
        }

        if (healing.DamageDict.Count > 0)
            _damageable.TryChangeDamage(ent.Owner, healing, true, false);
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
