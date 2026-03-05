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

    /// <summary>
    /// Determines whether a trophy collection attempt was made to limit it to 1 attempt per mob.
    /// </summary>
    [ViewVariables]
    public bool Collected;

    [DataField]
    public SoundSpecifier CollectSound = new SoundPathSpecifier("/Audio/Effects/gib3.ogg");
}
