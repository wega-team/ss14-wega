using System.Linq;
using Content.Server.NPC.Components;
using Content.Server.NPC.HTN;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.NPC.Systems;

public sealed class NPCUseActionsOnTargetSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly NPCSystem _npc = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NPCUseActionsOnTargetComponent, MapInitEvent>(OnMapInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<NPCUseActionsOnTargetComponent, HTNComponent>();
        while (query.MoveNext(out var uid, out var comp, out var htn))
        {
            if (!htn.Blackboard.TryGetValue<EntityUid>(comp.TargetKey, out var target, EntityManager) || !Exists(target))
                continue;

            if (_mobState.IsIncapacitated(uid))
            {
                _npc.SleepNPC(uid, htn);
                continue;
            }

            TryUseRandomAction((uid, comp), target);
        }
    }

    private void OnMapInit(Entity<NPCUseActionsOnTargetComponent> ent, ref MapInitEvent args)
    {
        foreach (var action in ent.Comp.ActionIds)
        {
            ent.Comp.ActionEnts[action] = _actions.AddAction(ent, action);
        }
    }

    public void SetActions(EntityUid uid,
        List<EntProtoId<TargetActionComponent>> actionIds,
        Dictionary<EntProtoId<TargetActionComponent>, float> chances,
        NPCUseActionsOnTargetComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        ClearAllActions(uid, comp);

        comp.ActionIds = actionIds?.ToList() ?? new();
        comp.ActionChances = chances?.ToDictionary(x => x.Key, x => x.Value) ?? new();

        InitializeActions(uid, comp);
    }

    public void ClearAllActions(EntityUid uid, NPCUseActionsOnTargetComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        foreach (var (_, actionEnt) in comp.ActionEnts)
        {
            if (actionEnt != null && Exists(actionEnt.Value))
            {
                _actions.RemoveAction(uid, actionEnt.Value);
            }
        }

        comp.ActionIds.Clear();
        comp.ActionEnts.Clear();
        comp.ActionChances.Clear();
    }

    public void InitializeActions(EntityUid uid, NPCUseActionsOnTargetComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        foreach (var actionId in comp.ActionIds)
        {
            if (!comp.ActionEnts.ContainsKey(actionId))
            {
                var actionEnt = _actions.AddAction(uid, actionId);
                if (actionEnt != null)
                {
                    comp.ActionEnts[actionId] = actionEnt;
                }
            }
        }
    }

    public void SetActionChance(EntityUid uid, EntProtoId<TargetActionComponent> actionId,
        float chance, NPCUseActionsOnTargetComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        comp.ActionChances[actionId] = Math.Clamp(chance, 0.01f, 1.0f);
    }

    public void RemoveAction(EntityUid uid, EntProtoId<TargetActionComponent> actionId,
        NPCUseActionsOnTargetComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        if (comp.ActionEnts.TryGetValue(actionId, out var actionEnt) && actionEnt != null)
        {
            _actions.RemoveAction(uid, actionEnt.Value);
        }

        comp.ActionIds.Remove(actionId);
        comp.ActionEnts.Remove(actionId);
        comp.ActionChances.Remove(actionId);
    }

    public void AddAction(EntityUid uid, EntProtoId<TargetActionComponent> actionId,
        float? chance = null, NPCUseActionsOnTargetComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        if (!comp.ActionIds.Contains(actionId))
        {
            comp.ActionIds.Add(actionId);
            var actionEnt = _actions.AddAction(uid, actionId);
            if (actionEnt != null)
            {
                comp.ActionEnts[actionId] = actionEnt;
            }
        }

        if (chance.HasValue)
        {
            comp.ActionChances[actionId] = Math.Clamp(chance.Value, 0.01f, 1.0f);
        }
    }

    public bool SetDelaySpeed(EntityUid uid, float delayModifier, NPCUseActionsOnTargetComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return false;

        comp.DelayModifier = Math.Clamp(delayModifier, 0.01f, 1.0f);
        return true;
    }

    public bool TryUseRandomAction(Entity<NPCUseActionsOnTargetComponent?> user, EntityUid target)
    {
        if (!Resolve(user, ref user.Comp, false))
            return false;

        if (_timing.CurTime < user.Comp.NextUseTime)
            return false;

        var availableActions = new List<(EntityUid action, float chance)>();
        foreach (var (actionId, actionEnt) in user.Comp.ActionEnts)
        {
            if (actionEnt == null || !TryComp<ActionComponent>(actionEnt, out var actionComp))
                continue;

            var chance = user.Comp.ActionChances.TryGetValue(actionId, out var customChance)
                ? Math.Clamp(customChance, 0.01f, 1.0f) : Math.Clamp(user.Comp.DefaultChance, 0.01f, 1.0f);

            if (!_actions.ValidAction((actionEnt.Value, actionComp)))
                continue;

            availableActions.Add((actionEnt.Value, chance));
        }

        if (availableActions.Count == 0)
            return false;

        var totalWeight = availableActions.Sum(a => a.chance);
        if (totalWeight <= 0f)
            return false;

        _random.Shuffle(availableActions);

        var selectedAction = GetSelectedAction(availableActions, totalWeight);
        if (!TryComp<ActionComponent>(selectedAction, out var selectedComp))
            return false;

        _actions.SetEventTarget(selectedAction.Value, target);
        _actions.PerformAction(user.Owner, (selectedAction.Value, selectedComp), predicted: false);

        if (selectedComp.UseDelay.HasValue)
        {
            var delay = selectedComp.UseDelay.Value * user.Comp.DelayModifier;
            user.Comp.NextUseTime = _timing.CurTime + delay;
        }
        else
        {
            user.Comp.NextUseTime = _timing.CurTime + TimeSpan.FromSeconds(user.Comp.DelayModifier);
        }

        return true; ///🎉 What a piece of shit this NPC Code.
    }

    private EntityUid? GetSelectedAction(List<(EntityUid action, float chance)> values, float totalWeight)
    {
        var accumulated = 0f;
        var randomValue = _random.NextFloat(0f, totalWeight);

        EntityUid? selectedAction = null;
        foreach (var (action, chance) in values)
        {
            accumulated += chance;
            if (randomValue <= accumulated)
            {
                selectedAction = action;
                break;
            }
        }

        return selectedAction;
    }
}
