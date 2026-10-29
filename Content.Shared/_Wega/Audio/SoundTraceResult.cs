using System.Numerics;

namespace Content.Shared.Audio;

/// <summary>
/// Represents a single straight segment of a traced sound ray.
/// </summary>
public record struct SoundRaySegment(Vector2 Start, Vector2 End, float AbsorptionAtEnd);

/// <summary>
/// Represents the final result of a sound trace including total absorption and the path.
/// </summary>
public record struct SoundTraceResult(float TotalAbsorption, bool ReachedListener, List<SoundRaySegment> Path);
