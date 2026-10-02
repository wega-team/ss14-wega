using Content.Server.Voting.Managers;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Voting;
using Robust.Shared.Configuration;

namespace Content.Server.GameTicking;

public sealed partial class AutoVoteSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IVoteManager _vote = default!;

    [SubscribeLocalEvent]
    private void OnRoundEnd(RoundRestartCleanupEvent ev)
    {
        if (!_cfg.GetCVar(WegaCVars.VoteRoundEndEnabled))
            return;

        _vote.CreateStandardVote(null, StandardVoteType.Preset);
    }
}
