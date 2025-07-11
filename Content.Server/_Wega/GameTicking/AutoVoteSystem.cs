using System.Linq;
using Content.Server.Voting.Managers;
using Content.Shared.GameTicking;
using Content.Shared.Voting;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Player;

namespace Content.Server.GameTicking
{
    public sealed class AutoVoteSystem : EntitySystem
    {
        [Dependency] private readonly IVoteManager _voteManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<VoteRoundEndEvent>(OnRoundEnd);
        }

        private void OnRoundEnd(VoteRoundEndEvent ev)
        {
            ICommonSession? initiator = null;
            var sessions = _playerManager.Sessions.Where(s => s.Status == SessionStatus.InGame).ToList();
            if (sessions.Count > 0)
                initiator = sessions[0];

            _voteManager.CreateStandardVote(null, StandardVoteType.Preset);
        }
    }
}
