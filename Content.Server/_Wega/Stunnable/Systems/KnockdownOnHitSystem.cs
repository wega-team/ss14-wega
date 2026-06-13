using Content.Server.Stunnable.Components;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Silicons.Borgs.Components;

namespace Content.Server.Stunnable.Systems;
    
public sealed partial class KnockdownOnHitSystem : EntitySystem
{
    [Dependency] private SharedStunSystem _stunSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<KnockdownOnHitComponent, MeleeHitEvent>(OnMeleeHit);
    }
    
    private void OnMeleeHit(EntityUid uid, KnockdownOnHitComponent component, MeleeHitEvent args)
    {
        if (args.HitEntities == null)
            return;
        
        foreach (var target in args.HitEntities)
        {
            if (!component.KnockdownBorgs && HasComp<BorgChassisComponent>(target))
                continue;
            
            _stunSystem.TryKnockdown(target, component.Time, component.Refresh, component.AutoStand, component.DropItems, true);
        }
        
    }
}