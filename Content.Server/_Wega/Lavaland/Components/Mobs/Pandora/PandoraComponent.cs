using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Lavaland.Mobs.Components;

[RegisterComponent, Access(typeof(PandoraSystem))]
public sealed partial class PandoraComponent : Component
{
    [DataField]
    public EntProtoId SquarePrototype = "EffectHierophantSquare";

    [DataField]
    public float PassiveMoveInterval = 3f;
    public TimeSpan NextPassiveMoveTime;

    [DataField] public SoundSpecifier BlinkSound = new SoundPathSpecifier("/Audio/Magic/blink.ogg");

    /// <summary>
    /// HTN blackboard key for the target entity.
    /// </summary>
    public string TargetKey = "Target";
}
