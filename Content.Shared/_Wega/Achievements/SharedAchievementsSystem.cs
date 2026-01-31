using System.Threading.Tasks;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared.Achievements;

public abstract partial class SharedAchievementsSystem : EntitySystem
{
    protected virtual NetUserId? GetUserId(EntityUid user)
    {
        if (!TryComp(user, out ActorComponent? actor))
            return null;

        return actor.PlayerSession.UserId;
    }

    public virtual void QueueAchievement(EntityUid user, AchievementsEnum achievement)
    {
        _ = QueueAchievementInternal(user, achievement);
    }

    private async Task QueueAchievementInternal(EntityUid user, AchievementsEnum achievement)
        => await TryAddAchievement(user, achievement);

    public virtual async Task<bool> HasAchievement(EntityUid user, AchievementsEnum achievement)
    {
        var userId = GetUserId(user);
        if (userId == null)
            return false;

        return await HasAchievement(userId.Value, achievement);
    }

    public virtual async Task<bool> HasAchievement(NetUserId userId, AchievementsEnum achievement)
    {
        var ev = new GetAchievementStateRequestEvent(userId, (byte)achievement);
        RaiseLocalEvent(ev);
        return await ev.CompletionSource.Task;
    }

    public virtual async Task<List<AchievementsEnum>> GetAchievements(EntityUid user)
    {
        var userId = GetUserId(user);
        if (userId == null) return new List<AchievementsEnum>();

        return await GetAchievements(userId.Value);
    }

    public virtual async Task<List<AchievementsEnum>> GetAchievements(NetUserId userId)
    {
        var ev = new GetAchievementsRequestEvent(userId);
        RaiseLocalEvent(ev);
        return await ev.CompletionSource.Task;
    }

    public virtual async Task<bool> TryAddAchievement(EntityUid user, AchievementsEnum achievement)
    {
        var userId = GetUserId(user);
        if (userId == null)
            return false;

        var success = await TryAddAchievement(userId.Value, achievement);
        if (success)
        {
            AchievementUnlocked(user, achievement);
        }

        return success;
    }

    public virtual async Task<bool> TryAddAchievement(NetUserId userId, AchievementsEnum achievement)
    {
        if (await HasAchievement(userId, achievement))
            return false;

        var ev = new AddAchievementRequestEvent(userId, (byte)achievement);
        RaiseLocalEvent(ev);
        return await ev.CompletionSource.Task;
    }

    public virtual void AchievementUnlocked(EntityUid user, AchievementsEnum achievement)
    {
        var ev = new AchievementUnlockedEvent(GetNetEntity(user), achievement);
        RaiseNetworkEvent(ev);
    }

    public virtual List<AchievementsEnum> GetUnlockedAchievements()
    {
        return new List<AchievementsEnum>();
    }
}
