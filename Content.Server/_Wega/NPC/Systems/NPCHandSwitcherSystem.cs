using System.Linq;
using Content.Server.NPC.Components;
using Content.Server.NPC.HTN;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Timing;

namespace Content.Server.NPC.Systems;

/// <summary>
/// It didn't work out properly, so we're using heavy artillery.
/// </summary>
public sealed partial class NPCHandSwitcherSystem : EntitySystem
{
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private NPCSystem _npc = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<NPCHandSwitcherComponent, HTNComponent, HandsComponent>();
        while (query.MoveNext(out var uid, out var comp, out var htn, out var hands))
        {
            if (!htn.Blackboard.TryGetValue<EntityUid>(comp.TargetKey, out _, EntityManager))
                continue;

            if (_mobState.IsDead(uid))
            {
                _npc.SleepNPC(uid, htn);
                continue;
            }

            if (_timing.CurTime < comp.NextSwitch)
                continue;

            comp.NextSwitch = _timing.CurTime + comp.SwitchInterval;
            var currentHand = _hands.GetActiveHand((uid, hands));
            if (currentHand == null)
                continue;

            var currentIndex = hands.SortedHands.ToList().IndexOf(currentHand);
            if (currentIndex < 0)
                continue;

            var nextIndex = (currentIndex + 1) % hands.SortedHands.Count;
            var nextHand = hands.SortedHands[nextIndex];

            _hands.SetActiveHand((uid, hands), nextHand);
        }
    }
}
