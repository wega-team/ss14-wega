namespace Content.Shared.Ninja.Components;

/// <summary>
/// Marks the shuttle grid that belongs to the space ninja.
/// Used by SpiderOSSystem to locate and remotely control it.
/// </summary>
[RegisterComponent]
public sealed partial class NinjaShuttleComponent : Component { }

/// <summary>
/// Marks the main facility grid on the ninja planet.
/// Used by SpiderOSSystem as the "return home" FTL destination.
/// </summary>
[RegisterComponent]
public sealed partial class NinjaFacilityComponent : Component { }
