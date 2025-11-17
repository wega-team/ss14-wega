using Robust.Shared.Containers;
using Content.Shared.Mobs.Components;
using Robust.Shared.Timing;
using Robust.Server.Containers;
using Content.Shared.Whitelist;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage.Components;
using Content.Server.Damage.Components;


namespace Content.Server.Damage.Systems;

public sealed class DamageInContainerSystem : SharedDamageInContainerSystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageInContainerComponent, EntInsertedIntoContainerMessage>(OnInserted);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<ActiveDamageInContainerComponent, DamageInContainerComponent, ContainerManagerComponent>();
        while (query.MoveNext(out var uid, out _, out var comp, out var containerComp))
        {
            if (_gameTiming.CurTime < comp.NextTickTime)
                continue;

            comp.NextTickTime = _gameTiming.CurTime + TimeSpan.FromSeconds(comp.Interval);

            if (!_container.TryGetContainer(uid, comp.SlotId, out var container, containerComp) || container.ContainedEntities.Count == 0)
            {
                RemComp<ActiveDamageInContainerComponent>(uid);
                continue;
            }

            foreach (var contained in container.ContainedEntities)
            {
                if (!TryComp<DamageableComponent>(contained, out var damage))
                    continue;

                if (comp.DamageCap != 0 && damage.TotalDamage >= comp.DamageCap)
                    continue;

                if (_whitelistSystem.IsWhitelistFail(comp.Whitelist, contained))
                    continue;

                if (TryComp<MobStateComponent>(contained, out var mobState))
                {
                    foreach (var allowedState in comp.AllowedStates)
                    {
                        if (allowedState == mobState.CurrentState)
                            _damageable.TryChangeDamage(contained, comp.Damage, true, false);
                    }
                    continue;
                }

                _damageable.TryChangeDamage(contained, comp.Damage, true, false);
            }
        }
    }

    private void OnInserted(EntityUid uid, DamageInContainerComponent comp, EntInsertedIntoContainerMessage args)
    {
        if (!comp.Initialized)
            return;

        if (args.Container.ID != comp.SlotId)
            return;

        AddComp<ActiveDamageInContainerComponent>(uid);
    }
}

