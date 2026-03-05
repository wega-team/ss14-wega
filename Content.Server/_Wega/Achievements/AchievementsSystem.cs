using System.Linq;
using Content.Server.Database;
using Content.Shared.Achievements;

namespace Content.Server.Achievements;

public sealed partial class AchievementsSystem : SharedAchievementsSystem
{
    [Dependency] private readonly IServerDbManager _db = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<RequestAchievementsEvent>(OnRequestAchievements);

        SubscribeLocalEvent<GetAchievementStateRequestEvent>(OnGetAchievementStateRequest);
        SubscribeLocalEvent<GetAchievementsRequestEvent>(OnGetAchievementsRequest);
        SubscribeLocalEvent<AddAchievementRequestEvent>(OnAddAchievementRequest);
    }

    private async void OnRequestAchievements(RequestAchievementsEvent ev, EntitySessionEventArgs args)
    {
        var session = args.SenderSession;
        var achievements = await GetAchievements(session.UserId);
        RaiseNetworkEvent(new ResponseAchievementsEvent(achievements), session);
    }

    private async void OnGetAchievementStateRequest(GetAchievementStateRequestEvent ev)
    {
        var hasAchievement = await _db.HasAchievementAsync(ev.UserId, ev.AchievementKey);
        ev.CompletionSource.SetResult(hasAchievement);
    }

    private async void OnGetAchievementsRequest(GetAchievementsRequestEvent ev)
    {
        var records = await _db.GetAchievementsAsync(ev.UserId);
        var achievements = records.Select(r => (AchievementsEnum)r.AchievementKey).ToList();
        ev.CompletionSource.SetResult(achievements);
    }

    private async void OnAddAchievementRequest(AddAchievementRequestEvent ev)
    {
        var alreadyHas = await _db.HasAchievementAsync(ev.UserId, ev.AchievementKey);
        if (alreadyHas)
        {
            ev.CompletionSource.SetResult(false);
            return;
        }

        var result = await _db.AddAchievementAsync(ev.UserId, ev.AchievementKey);
        ev.CompletionSource.SetResult(result);
    }
}
