namespace Content.Server.Grab.Components;

[RegisterComponent]
public sealed partial class GrabThrownComponent : Component
{
    public TimeSpan ExpiresAt;
}
