using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Xenobiology.Components.Machines;

[RegisterComponent, NetworkedComponent]
public sealed partial class CellMutagenicInjectorComponent : Component
{
    [DataField]
    public string DishSlot = "dishSlot";

    [DataField]
    public string EntitySlot = "entity_storage";

    [DataField]
    public bool Enabled = false;

    [DataField]
    public EntityUid? Cell;

    [DataField]
    public EntityUid? Target;

    public EntityUid? PlayingStream;

    [DataField]
    public TimeSpan MaxTime = TimeSpan.FromMinutes(4);

    [DataField]
    public TimeSpan MinTime = TimeSpan.FromMinutes(1.5);

    public TimeSpan ActivateTime = TimeSpan.Zero;

    public string FailedMob = "MobAbomination";

    [DataField("loopingSound")]
    public SoundSpecifier LoopingSound = new SoundPathSpecifier("/Audio/Machines/microwave_loop.ogg");
}
