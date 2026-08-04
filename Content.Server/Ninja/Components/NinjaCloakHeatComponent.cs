namespace Content.Server.Ninja.Components;

/// <summary>
/// Tracks phase-cloak heat on the ninja suit.
/// Heat builds while cloaked (causing sparks) and passively cools when uncloaked.
/// </summary>
[RegisterComponent]
public sealed partial class NinjaCloakHeatComponent : Component
{
    public const float MinChance          = 0.02f;  // 2% — starting spark probability
    public const float MaxChance          = 0.50f;  // 50% — cap
    public const float HeatRate           = 0.002f; // probability gained per second while cloaked (~4 min to max)
    public const float CoolRate           = 0.005f; // probability lost per second while uncloaked (~1.5 min to cool)
    public const float SparkCheckInterval = 3f;     // seconds between spark roll attempts

    [DataField]
    public float SparkChance = MinChance;

    [DataField]
    public bool IsCloaked;

    public float SparkTimer;
}
