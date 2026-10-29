using Robust.Shared.Audio;

namespace Content.Shared.Lavaland.Components;

[RegisterComponent]
public sealed partial class BossMusicComponent : Component
{
    [DataField(required: true)]
    public SoundSpecifier Music = default!;

    [DataField]
    public float Volume = -4f;
}

[RegisterComponent]
public sealed partial class BossMusicTrackerComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid Boss;

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? AudioEntity;

    [ViewVariables(VVAccess.ReadOnly)]
    public float CurrentVolume;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsFadingOut;
}
