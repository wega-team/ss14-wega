using Content.Server.NPC.Components;
using Content.Server.NPC.HTN;
using Content.Shared.Actions;
using Content.Shared.Mobs.Systems; // Corvax-Wega-Add
using Robust.Shared.Timing;

namespace Content.Server.NPC.Systems;

public sealed class NPCUseActionOnTargetSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!; // Corvax-Wega-Add
    [Dependency] private readonly NPCSystem _npc = default!; // Corvax-Wega-Add

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NPCUseActionOnTargetComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<NPCUseActionOnTargetComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.ActionEnt = _actions.AddAction(ent, ent.Comp.ActionId);
    }

    public bool TryUseTentacleAttack(Entity<NPCUseActionOnTargetComponent?> user, EntityUid target)
    {
        if (!Resolve(user, ref user.Comp, false))
            return false;

        if (_actions.GetAction(user.Comp.ActionEnt) is not {} action)
            return false;

        if (!_actions.ValidAction(action))
            return false;

        _actions.SetEventTarget(action, target);

        // NPC is serverside, no prediction :(
        _actions.PerformAction(user.Owner, action, predicted: false);
        return true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Tries to use the attack on the current target.
        var query = EntityQueryEnumerator<NPCUseActionOnTargetComponent, HTNComponent>();
        while (query.MoveNext(out var uid, out var comp, out var htn))
        {
            if (!htn.Blackboard.TryGetValue<EntityUid>(comp.TargetKey, out var target, EntityManager))
                continue;

            // Corvax-Wega-Add-start
            if (htn.Plan == null)
                continue;

            if (_mobState.IsIncapacitated(uid))
            {
                _npc.SleepNPC(uid, htn);
                continue;
            }
            // Corvax-Wega-Add-end

            TryUseTentacleAttack((uid, comp), target);
        }
    }
}
