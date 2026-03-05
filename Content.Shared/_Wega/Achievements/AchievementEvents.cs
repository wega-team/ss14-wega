using System.Threading.Tasks;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Achievements;

[Serializable, NetSerializable]
public sealed class AchievementUnlockedEvent : EntityEventArgs
{
    public NetEntity User { get; }
    public AchievementsEnum Achievement { get; }

    public AchievementUnlockedEvent(NetEntity user, AchievementsEnum achievement)
    {
        User = user;
        Achievement = achievement;
    }
}

[Serializable, NetSerializable]
public sealed class RequestAchievementsEvent : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed class ResponseAchievementsEvent : EntityEventArgs
{
    public List<AchievementsEnum> Achievements { get; }

    public ResponseAchievementsEvent(List<AchievementsEnum> achievements)
    {
        Achievements = achievements;
    }
}

// Server
public sealed class GetAchievementStateRequestEvent : EntityEventArgs
{
    public NetUserId UserId { get; }
    public byte AchievementKey { get; }
    public TaskCompletionSource<bool> CompletionSource { get; }

    public GetAchievementStateRequestEvent(NetUserId userId, byte achievementKey)
    {
        UserId = userId;
        AchievementKey = achievementKey;
        CompletionSource = new TaskCompletionSource<bool>();
    }
}

public sealed class GetAchievementsRequestEvent : EntityEventArgs
{
    public NetUserId UserId { get; }
    public TaskCompletionSource<List<AchievementsEnum>> CompletionSource { get; }

    public GetAchievementsRequestEvent(NetUserId userId)
    {
        UserId = userId;
        CompletionSource = new TaskCompletionSource<List<AchievementsEnum>>();
    }
}

public sealed class AddAchievementRequestEvent : EntityEventArgs
{
    public NetUserId UserId { get; }
    public byte AchievementKey { get; }
    public TaskCompletionSource<bool> CompletionSource { get; }

    public AddAchievementRequestEvent(NetUserId userId, byte achievementKey)
    {
        UserId = userId;
        AchievementKey = achievementKey;
        CompletionSource = new TaskCompletionSource<bool>();
    }
}
