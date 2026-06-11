using Content.Shared.Achievements;
using Content.Shared.FixedPoint;

namespace Content.Shared.Lavaland.Components;

[RegisterComponent]
public sealed partial class MegafaunaDamageContributorComponent : Component
{
    [DataField("achievement", required: true)]
    public AchievementsEnum AchievementId = default!;

    [DataField]
    public float Threshold = 0.3f;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool AchievementsGranted = false;

    [ViewVariables(VVAccess.ReadOnly)]
    public FixedPoint2 TotalDamageReceived = 0f;

    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<EntityUid, FixedPoint2> Contributors = new();
}
