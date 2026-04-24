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
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server._Wega.Aliens.Facehugger;

public sealed class FacehuggerSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ThrownItemSystem _thrown = default!;

    private static readonly TimeSpan SleepDuration = TimeSpan.FromSeconds(4);

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FacehuggerComponent, ThrowDoHitEvent>(OnThrowHit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<FacehuggerComponent>();
        while (query.MoveNext(out var uid, out var hugger))
        {
            if (!IsHuggerAlive(uid))
            {
                hugger.CurrentTarget = null;
                hugger.AttachTime = null;
                continue;
            }

            if (_container.IsEntityInContainer(uid))
            {
                hugger.CurrentTarget = null;
                hugger.AttachTime = null;
                continue;
            }

            if (HasComp<ThrownItemComponent>(uid))
            {
                hugger.CurrentTarget = null;
                hugger.AttachTime = null;
                continue;
            }

            var coords = Transform(uid).Coordinates;
            EntityUid? bestTarget = null;

            foreach (var (target, _) in _lookup.GetEntitiesInRange<InventoryComponent>(coords, hugger.ProximityRange))
            {
                if (target == uid)
                    continue;
                if (HasComp<FacehuggerComponent>(target))
                    continue;
                if (HasComp<GhostComponent>(target))
                    continue;
                if (HasSealedHelmet(target))
                    continue;
                if (!_interaction.InRangeUnobstructed(uid, target, hugger.ProximityRange))
                    continue;

                bestTarget = target;
                break;
            }

            if (bestTarget == null)
            {
                hugger.CurrentTarget = null;
                hugger.AttachTime = null;
                continue;
            }

            if (hugger.CurrentTarget != bestTarget.Value)
            {
                hugger.CurrentTarget = bestTarget.Value;
                hugger.AttachTime = _timing.CurTime + hugger.AttachDelay;
            }

            if (_timing.CurTime >= hugger.AttachTime)
            {
                TryAttach(uid, hugger.CurrentTarget.Value);
                hugger.CurrentTarget = null;
                hugger.AttachTime = null;
            }
        }
    }

    private void OnThrowHit(Entity<FacehuggerComponent> ent, ref ThrowDoHitEvent args)
    {
        if (!IsHuggerAlive(ent.Owner))
            return;

        if (!HasComp<InventoryComponent>(args.Target))
            return;

        if (HasComp<GhostComponent>(args.Target))
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
        _inventory.TryUnequip(target, "mask", force: true, silent: true);

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
