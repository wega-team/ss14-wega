using Content.Server.NPC.Components;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Projectiles;
using Robust.Shared.Timing;

namespace Content.Server.Projectiles;

public sealed class ProjectileTimerResetsSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ProjectileTimerResetsComponent, ProjectileHitEvent>(OnProjectileHit);
    }

    private void OnProjectileHit(EntityUid entity, ProjectileTimerResetsComponent component, ref ProjectileHitEvent ev)
    {
        if (TryComp<NPCUseActionOnTargetComponent>(ev.Target, out var npc))
        {
            if (TryComp<ActionComponent>(npc.ActionEnt, out var actionComp))
            {
                var remaining = actionComp.Cooldown?.End - _gameTiming.CurTime ?? TimeSpan.Zero;
                var newCooldown = remaining + TimeSpan.FromSeconds(component.ResetsTime);

                _action.SetCooldown(npc.ActionEnt, newCooldown);
            }
        }

        if (TryComp<NPCUseActionsOnTargetComponent>(ev.Target, out var npcs))
        {
            foreach (var (_, actionEntity) in npcs.ActionEnts)
            {
                if (actionEntity == null)
                    continue;

                if (TryComp<ActionComponent>(actionEntity.Value, out var actionComp))
                {
                    var remaining = actionComp.Cooldown?.End - _gameTiming.CurTime ?? TimeSpan.Zero;
                    var newCooldown = remaining + TimeSpan.FromSeconds(component.ResetsTime);

                    _action.SetCooldown(actionEntity.Value, newCooldown);
                }
            }
        }
    }
}
