using System.Diagnostics.CodeAnalysis;
using Content.Shared.Preferences;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Roles;

/// <summary>
/// Requires that at least one of the listed requirements is met.
/// </summary>
[UsedImplicitly]
[Serializable, NetSerializable]
public sealed partial class AnyRequirement : JobRequirement
{
    [DataField(required: true)]
    public List<JobRequirement> Requirements = new();

    public override bool Check(
        IEntityManager entManager,
        IPrototypeManager protoManager,
        HumanoidCharacterProfile? profile,
        IReadOnlyDictionary<string, TimeSpan> playTimes,
        [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = null;
        var anyPassed = false;
        var failedReasons = new List<FormattedMessage>();

        foreach (var requirement in Requirements)
        {
            if (requirement.Check(entManager, protoManager, profile, playTimes, out var reqReason))
            {
                anyPassed = true;
                if (!Inverted)
                    break;
            }
            else if (reqReason != null)
            {
                failedReasons.Add(reqReason);
            }
        }

        // Normal mode: need at least one to pass
        if (!Inverted)
        {
            if (anyPassed)
                return true;

            reason = BuildCombinedFailureMessage(failedReasons, false);
            return false;
        }

        // Inverted mode: need ALL to fail
        if (!anyPassed)
            return true;

        reason = BuildCombinedFailureMessage(failedReasons, true);
        return false;
    }

    private FormattedMessage BuildCombinedFailureMessage(List<FormattedMessage> failedReasons, bool inverted)
    {
        var message = new FormattedMessage();
        if (failedReasons.Count == 0)
        {
            message.AddMarkupPermissive(Loc.GetString(inverted
                ? "role-any-requirement-inverted-failed"
                : "role-any-requirement-failed"));
            return message;
        }

        if (inverted)
        {
            message.AddMarkupPermissive(Loc.GetString("role-any-requirement-inverted-need-all") + "\n");
        }
        else
        {
            message.AddMarkupPermissive(Loc.GetString("role-any-requirement-need-one") + "\n");
        }

        for (var i = 0; i < failedReasons.Count; i++)
        {
            message.AddMarkupPermissive($"- ");
            message.AddMarkupPermissive(failedReasons[i].ToMarkup());
            if (i < failedReasons.Count - 1)
                message.AddMarkupPermissive("\n");
        }

        return message;
    }
}
