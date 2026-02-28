using Robust.Shared.GameStates;

namespace Content.Shared.Visuals;

[RegisterComponent, NetworkedComponent]
public sealed partial class FadeComponent : Component
{
    [DataField]
    public float FadeDuration = 5f;

    [DataField]
    public Color StartColor = Color.White;

    [DataField]
    public Color TargetColor = Color.White.WithAlpha(0f);

    public TimeSpan StartTime;
}
