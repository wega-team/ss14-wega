using Content.Shared.Damage.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Damage.Systems;

public sealed partial class IncreasedDamageSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IncreasedDamageComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<IncreasedDamageComponent, DamageDealtEvent>(OnDamageChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<IncreasedDamageComponent>();
        while (query.MoveNext(out var uid, out var increased))
        {
            if (_timing.CurTime < increased.EndTime)
                continue;

            RemCompDeferred<IncreasedDamageComponent>(uid);
        }
    }

    private void OnMapInit(Entity<IncreasedDamageComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.EndTime = _timing.CurTime + ent.Comp.ActiveInterval;
    }

    private void OnDamageChanged(Entity<IncreasedDamageComponent> ent, ref DamageDealtEvent args)
    {
        if (args.Damage.GetTotal() <= 0)
            return;

        var modifiedDamage = args.Damage * ent.Comp.DamageModifier;
        _damage.TryChangeDamage(ent.Owner, modifiedDamage, true);
    }
}
