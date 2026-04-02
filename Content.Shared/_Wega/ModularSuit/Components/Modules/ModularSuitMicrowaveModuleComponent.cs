using Robust.Shared.Audio;

namespace Content.Shared.Modular.Suit;

[RegisterComponent]
public sealed partial class ModularSuitMicrowaveModuleComponent : SharedItemModuleComponent
{
    [DataField]
    public float CookDelay = 3.0f;

    [DataField]
    public float HeatAmount = 1500f;

    [DataField]
    public SoundSpecifier StartSound = new SoundPathSpecifier("/Audio/Machines/microwave_start_beep.ogg");

    [DataField]
    public SoundSpecifier CompleteSound = new SoundPathSpecifier("/Audio/Machines/microwave_done_beep.ogg");

    [DataField]
    public SoundSpecifier LoopingSound = new SoundPathSpecifier("/Audio/Machines/microwave_loop.ogg");
    public EntityUid? AudioStream;
}
