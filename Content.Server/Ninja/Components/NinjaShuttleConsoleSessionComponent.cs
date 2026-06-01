namespace Content.Server.Ninja.Components;

/// <summary>
/// Added to a ninja while they have a remote shuttle console open.
/// Removed (along with IgnoreUIRangeComponent) once the console UI closes.
/// </summary>
[RegisterComponent]
public sealed partial class NinjaShuttleConsoleSessionComponent : Component
{
    public EntityUid ConsoleUid;
}
