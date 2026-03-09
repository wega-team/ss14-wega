using Content.Server.NPC.Components;
using Content.Server.NPC.HTN;
using Content.Shared.Damage.Systems;
using Content.Shared.NPC;
using Robust.Shared.Player;

namespace Content.Server.NPC.Systems;

public sealed class NPCAggressionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NPCAggressionComponent, DamageChangedEvent>(OnDamageChanged, after: [typeof(NPCOptimizationSystem)]);
    }

    private void OnDamageChanged(EntityUid uid, NPCAggressionComponent component, DamageChangedEvent args)
    {
        if (args.Origin == null || !TryComp<HTNComponent>(uid, out var htn) || HasComp<ActorComponent>(uid))
            return;

        if (!HasComp<ActiveNPCComponent>(uid))
            return;

        if (htn.Blackboard.TryGetValue<EntityUid>(component.TargetKey, out var target, EntityManager) && Exists(target))
            return;

        htn.Blackboard.SetValue(component.TargetKey, args.Origin.Value);
    }
}
