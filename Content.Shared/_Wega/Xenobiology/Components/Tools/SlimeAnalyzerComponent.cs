using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Xenobiology.Components.Tools;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class SlimeAnalyzerComponent : Component
{
    [DataField("scanDelay")]
    public TimeSpan ScanDelay = TimeSpan.FromSeconds(0.8);

    [DataField("updateInterval")]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    [DataField("maxScanRange")]
    public float MaxScanRange = 2.5f;

    [DataField("scanningBeginSound")]
    public SoundSpecifier? ScanningBeginSound;

    [DataField("scanningEndSound")]
    public SoundSpecifier ScanningEndSound = new SoundPathSpecifier("/Audio/Items/Medical/healthscanner.ogg");

    [DataField("nextUpdate", customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    [ViewVariables]
    public EntityUid? ScannedEntity;
}
