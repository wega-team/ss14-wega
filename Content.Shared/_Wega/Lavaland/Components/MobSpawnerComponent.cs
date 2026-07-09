using Robust.Shared.Prototypes;

namespace Content.Shared.Lavaland.Components;

[RegisterComponent]
public sealed partial class MobSpawnerComponent : Component
{
    [DataField(required: true)]
    public Dictionary<EntProtoId, float> SpawnWeights = new();

    [DataField]
    public HashSet<EntityUid> SpawnedMobs = new();

    [DataField]
    public int MaxSpawns = 3;

    [DataField]
    public float ActivationRadius = 15f;

    [DataField]
    public float SpawnRadius = 3f;

    [DataField]
    public TimeSpan SpawnInterval = TimeSpan.FromSeconds(45);

    [ViewVariables]
    public TimeSpan NextSpawnTime = TimeSpan.Zero;

    [ViewVariables]
    public bool IsActive = false;
}
