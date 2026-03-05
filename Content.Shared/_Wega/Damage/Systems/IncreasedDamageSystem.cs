using Content.Shared.Damage.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Damage.Systems;

public sealed class IncreasedDamageSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IncreasedDamageComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<IncreasedDamageComponent, DamageChangedEvent>(OnDamageChanged);
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

    private void OnDamageChanged(Entity<IncreasedDamageComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased || args.DamageDelta == null)
            return;

        _damage.TryChangeDamage(ent.Owner, args.DamageDelta * ent.Comp.DamageModifier, true);
    }
}
