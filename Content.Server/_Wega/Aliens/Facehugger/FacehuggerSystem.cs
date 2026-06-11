using Content.Server.Atmos.Components;
using Content.Shared._Wega.Aliens.Facehugger;
using Content.Shared.Bed.Sleep;
using Content.Shared.Ghost;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.StatusEffectNew;
using Content.Shared.Throwing;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server._Wega.Aliens.Facehugger;

public sealed partial class FacehuggerSystem : EntitySystem
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ThrownItemSystem _thrown = default!;

    private static readonly TimeSpan SleepDuration = TimeSpan.FromSeconds(4);

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FacehuggerComponent, ThrowDoHitEvent>(OnThrowHit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;

        var query = EntityQueryEnumerator<FacehuggerComponent>();
        while (query.MoveNext(out var uid, out var hugger))
        {
            if (!IsHuggerAlive(uid))
                continue;

            if (_container.IsEntityInContainer(uid) || HasComp<ThrownItemComponent>(uid))
                continue;

            if (hugger.CurrentTarget != null)
            {
                if (curTime >= hugger.AttachTime)
                {
                    TryAttach(uid, hugger.CurrentTarget.Value);
                    hugger.CurrentTarget = null;
                    hugger.AttachTime = null;
                }
                continue;
            }

            FindNewTarget(uid, hugger, curTime);
        }
    }

    private void FindNewTarget(EntityUid uid, FacehuggerComponent hugger, TimeSpan curTime)
    {
        var coords = Transform(uid).Coordinates;
        foreach (var (target, _) in _lookup.GetEntitiesInRange<InventoryComponent>(coords, hugger.ProximityRange))
        {
            if (target == uid)
                continue;

            if (HasComp<FacehuggerComponent>(target) || HasComp<GhostComponent>(target))
                continue;

            if (HasSealedHelmet(target))
                continue;

            if (!_interaction.InRangeUnobstructed(uid, target, hugger.ProximityRange))
                continue;

            hugger.CurrentTarget = target;
            hugger.AttachTime = curTime + hugger.AttachDelay;
            return;
        }
    }

    private void OnThrowHit(Entity<FacehuggerComponent> ent, ref ThrowDoHitEvent args)
    {
        if (!IsHuggerAlive(ent.Owner))
            return;

        if (!HasComp<InventoryComponent>(args.Target) || HasComp<GhostComponent>(args.Target))
            return;

        if (HasSealedHelmet(args.Target))
            return;

        if (!_interaction.InRangeUnobstructed(ent.Owner, args.Target, ent.Comp.ProximityRange))
            return;

        if (!TryAttach(ent.Owner, args.Target))
            return;

        _thrown.StopThrow(ent.Owner, args.Component);
    }

    private bool TryAttach(EntityUid hugger, EntityUid target)
    {
        if (_inventory.TryGetSlotEntity(target, "mask", out var mask) && HasComp<FacehuggerComponent>(mask))
            return false;

        if (!_inventory.TryEquip(target, target, hugger, "mask", force: true, silent: true))
            return false;

        _statusEffects.TryAddStatusEffectDuration(target, SleepingSystem.StatusEffectForcedSleeping, SleepDuration);

        return true;
    }

    private bool IsHuggerAlive(EntityUid uid)
    {
        if (!TryComp<MobStateComponent>(uid, out var mobState))
            return true;

        return _mobState.IsAlive(uid, mobState);
    }

    private bool HasSealedHelmet(EntityUid target)
    {
        return _inventory.TryGetSlotEntity(target, "head", out var helmet)
               && HasComp<PressureProtectionComponent>(helmet.Value);
    }
}
