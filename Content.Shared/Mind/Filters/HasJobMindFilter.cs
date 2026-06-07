using Content.Shared.Roles.Jobs;

namespace Content.Shared.Mind.Filters;

/// <summary>
/// Requires minds to have a station job role, i.e. be actual station crew. Removes jobless minds
/// such as nuclear operatives, wizards, survivors and ghost roles.
/// </summary>
public sealed partial class HasJobMindFilter : MindFilter
{
    protected override bool ShouldRemove(Entity<MindComponent> mind, EntityUid? exclude, IEntityManager entMan)
    {
        var jobSys = entMan.System<SharedJobSystem>();
        return !jobSys.MindTryGetJob(mind.Owner, out _);
    }
}
