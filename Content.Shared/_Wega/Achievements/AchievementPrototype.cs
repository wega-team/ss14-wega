using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Achievements;

[Prototype]
public sealed partial class AchievementPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("icon", required: true)]
    public SpriteSpecifier AchievementIcon { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    [DataField]
    public LocId? Description;

    [DataField]
    public AchievementsEnum Key = AchievementsEnum.Default;
}
