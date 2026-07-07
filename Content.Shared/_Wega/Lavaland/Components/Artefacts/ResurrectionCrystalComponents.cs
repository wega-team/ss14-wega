using Robust.Shared.Audio;

namespace Content.Shared.Lavaland.Artefacts.Components;

[RegisterComponent]
public sealed partial class ResurrectionCrystalComponent : Component
{
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(5);

    [DataField]
    public SoundSpecifier UseSound = new SoundPathSpecifier("/Audio/Effects/guardian_inject.ogg");
}

[RegisterComponent]
public sealed partial class ResurrectionCrystalAffectedComponent : Component
{
    [DataField]
    public float MinDistance = 27f;
}
