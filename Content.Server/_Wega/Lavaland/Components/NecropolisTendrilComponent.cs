using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Lavaland.Components;

[RegisterComponent]
public sealed partial class NecropolisTendrilComponent : Component
{
    [DataField(required: true)]
    public Dictionary<EntProtoId, float> SpawnWeights = new();

    [DataField]
    public int MaxSpawns = 3;

    [DataField]
    public HashSet<EntityUid> SpawnedMobs = new();

    [DataField]
    public float ActivationRadius = 15f;

    [DataField]
    public float SpawnRadius = 3f;

    [DataField]
    public TimeSpan SpawnInterval = TimeSpan.FromSeconds(45);

    [DataField]
    public TimeSpan NextSpawnTime = TimeSpan.Zero;

    [DataField]
    public bool IsActive = false;

    [DataField]
    public EntProtoId ChasmPrototype = "FloorChasmEntity";

    [DataField]
    public float ChasmDelay = 10f;

    [DataField] public SoundSpecifier ChasmSound = new SoundPathSpecifier("/Audio/_Wega/Effects/Planet/rumble.ogg", AudioParams.Default.WithVolume(6));
}
