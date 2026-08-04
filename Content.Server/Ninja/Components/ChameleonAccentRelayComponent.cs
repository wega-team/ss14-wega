namespace Content.Server.Ninja.Components;

/// <summary>
/// Added to a ninja while they are disguised.
/// Intercepts AccentGetEvent and re-raises it on the scanned target so the ninja
/// sounds exactly like them (accent, speech mannerisms).
/// </summary>
[RegisterComponent]
public sealed partial class ChameleonAccentRelayComponent : Component
{
    /// <summary>The humanoid whose accent to mimic.</summary>
    public EntityUid AccentTarget;
}
