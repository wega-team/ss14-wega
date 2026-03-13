using Content.Shared.Emp;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Hitscan.Events;
using Content.Shared.Mobs.Components;

namespace Content.Shared.Weapons.Hitscan.Systems;

public sealed class HitscanEMPSystem : EntitySystem
{
    [Dependency] private readonly SharedEmpSystem _emp = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HitscanEMPComponent, HitscanRaycastFiredEvent>(OnHitscanHit);
    }

    //The hitscan has hit the target, rolls a chance to ignite and ignite if it succeeds.
    private void OnHitscanHit(Entity<HitscanEMPComponent> ent, ref HitscanRaycastFiredEvent args)
    {
        //If the roll succeeds, the target is set on fire.
        var target = args.Data.HitEntity;
        if (target == null)
            return;

        if (HasComp<MobStateComponent>(target.Value))
			_emp.EmpPulse(Transform(target.Value).Coordinates, ent.Comp.Range, ent.Comp.EnergyConsumption, ent.Comp.DisableDuration);
    }
}