using Robust.Shared.GameStates;

namespace Content.Server.Xenobiology;

[RegisterComponent, NetworkedComponent]
public sealed partial class SlimeSocialComponent : Component
{
    [ViewVariables]
    public EntityUid? Leader;

    [ViewVariables]
    public readonly HashSet<EntityUid> Friends = new();

    [ViewVariables]
    public float FriendshipLevel = 0f;

    [DataField("baseFriendshipDecay")]
    public float FriendshipDecayRate = 0.1f;

    [DataField("feedFriendshipBonus")]
    public float FeedFriendshipBonus = 10f;

    [DataField("listenRange")]
    public float ListenRange = 6f;

    [DataField]
    public TimeSpan LastCommandTime;

    [DataField("totalFeedings")]
    public int TotalFeedings = 0;

    [DataField("angerDuration")]
    public float AngerDuration = 30f;

    [DataField("friendshipLossOnAttack")]
    public float FriendshipLossOnAttack = 25f;

    [DataField("minFriendshipToBetray")]
    public float MinFriendshipToBetray = 30f;

    [DataField]
    public EntityUid? LastAttackEntity { get; set; } = null;

    public TimeSpan? AngryUntil;

    public TimeSpan? RebellionCooldownEnd;
}

[RegisterComponent]
public sealed partial class SlimeRebellionComponent : Component
{
    [DataField("joinChance")]
    public float BaseJoinChance = 0.7f;

    [DataField("spreadRadius")]
    public float SpreadRadius = 5f;

    [DataField("friendshipInfluence")]
    public float FriendshipInfluence = 0.1f;

    [ViewVariables]
    public EntityUid? Leader;

    [ViewVariables]
    public TimeSpan EndTime;
}
