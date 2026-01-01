using System.Diagnostics.CodeAnalysis;
using Content.Server.Objectives.Components;
using Content.Shared.Mind;

namespace Content.Server.Objectives.Systems;

public sealed class BloodBrotherSharedConditionSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public bool CheckBaseConditions(EntityUid mindId, BloodBrotherSharedConditionComponent comp, MindComponent? mind = null)
    {
        if (!Resolve(mindId, ref mind))
            return false;

        if (comp.RequireBothAlive && _mind.IsCharacterDeadIc(mind))
            return false;

        if (comp.RequireBothAlive && comp.BrotherMind.HasValue)
        {
            if (!TryComp<MindComponent>(comp.BrotherMind.Value, out var brotherMind) ||
                _mind.IsCharacterDeadIc(brotherMind))
                return false;
        }

        return true;
    }

    public bool TryGetSharedCondition(EntityUid objectiveUid, EntityUid mindId, [NotNullWhen(true)] out BloodBrotherSharedConditionComponent? sharedCondition)
    {
        sharedCondition = null;
        return TryComp(objectiveUid, out sharedCondition);
    }

    public void CopySharedConditionData(EntityUid sourceObjective, EntityUid targetObjective, EntityUid mindId1, EntityUid mindId2)
    {
        if (TryComp<BloodBrotherSharedConditionComponent>(sourceObjective, out var sourceCondition)
            && TryComp<BloodBrotherSharedConditionComponent>(targetObjective, out var targetCondition))
        {
            targetCondition.BrotherMind = mindId1;
            targetCondition.RequireBothAlive = sourceCondition.RequireBothAlive;

            sourceCondition.BrotherMind = mindId2;
        }
    }
}
