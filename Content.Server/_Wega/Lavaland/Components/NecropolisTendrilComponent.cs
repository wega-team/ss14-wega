using Robust.Shared.Prototypes;

namespace Content.Server.Lavaland.Components;

[RegisterComponent]
public sealed partial class NecropolisTendrilComponent : Component
{
    [DataField(required: true)]
    public Dictionary<EntProtoId, float> SpawnWeights = new();

    [DataField]
    public int MaxSpawns = 4;

    [DataField]
    public float ActivationRadius = 15f;

    [DataField]
    public float SpawnRadius = 3f;

    [DataField]
    public TimeSpan SpawnInterval = TimeSpan.FromSeconds(45);

    [DataField]
    public TimeSpan NextSpawnTime = TimeSpan.Zero;

    [DataField]
    public int SpawnedCount = 0;

    [DataField]
    public bool IsActive = false;

    [DataField]
    public EntProtoId ChasmPrototype = "FloorChasmEntity";

    [DataField]
    public float ChasmDelay = 10f;
}
