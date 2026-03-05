using Robust.Shared.Serialization;

namespace Content.Shared.Achievements;

/// <summary>
/// IMPORTANT: These values are stored in the database and are used for synchronization between the client and the server.
///
/// Rules for adding new achievements:
/// 1. NEVER change the existing values (Default = 0, FirstBoss = 1, HierophantBoss = 2)
/// 2. Add new achievements to the end of the list with new values (3, 4, 5, ...)
/// 3. If you delete an achievement, simply leave it in the enum and mark it as obsolete [Obsolete]
/// 4. Changing the order breaks data synchronization between versions and causes loss of information about achievements
/// 5. If you're using this elsewhere, use the most distant values for you, such as 32, 64, and so on
/// </summary>
[Serializable, NetSerializable]
public enum AchievementsEnum : byte
{
    Default = 0,
    FirstBoss = 1,
    HierophantBoss = 2,
    MinerBoss = 3,
    LegionBoss = 4,
    ColossusBoss = 5,
    AshDrakeBoss = 6,
    BubblegumBoss = 7,
    BloodCult = 11,
}
