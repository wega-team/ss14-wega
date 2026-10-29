using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.Lavaland.Components;

[RegisterComponent]
[Access(typeof(TrophyHunterSystem))]
public sealed partial class TrophyHuntingToolComponent : Component;

[RegisterComponent]
[Access(typeof(TrophyHunterSystem))]
public sealed partial class TrophyHunterComponent : Component
{
    [DataField(required: true)]
    public EntProtoId Trophy;

    [DataField]
    public float DropChance = 0.25f;

    [DataField]
    public float RequiredThreshold = 0.6f;

    /// <summary>
    /// Determines whether a trophy collection attempt was made to limit it to 1 attempt per mob.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public bool Collected;

    /// <summary>
    /// Contains the entire damage received from <see cref="TrophyHuntingToolComponent"/>.
    /// It compares the limit with the <see cref="RequiredThreshold"/>, and if it is greater than or equal to the threshold,
    /// You can get the trophy.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public float CurrentDamage = 0f;

    [DataField]
    public SoundSpecifier CollectSound = new SoundPathSpecifier("/Audio/Effects/gib3.ogg");
}
