using Content.Shared.DoAfter;
using Content.Shared.Paper;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Botany.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PlantAnalyzerComponent : Component
{
    /// <summary>
    /// When will the analyzer be ready to print again?
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan PrintReadyAt = TimeSpan.Zero;

    /// <summary>
    /// How often can the analyzer print?
    /// </summary>
    [DataField]
    public TimeSpan PrintCooldown = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The sound that's played when the analyzer prints off a report.
    /// </summary>
    [DataField]
    public SoundSpecifier SoundPrint = new SoundPathSpecifier("/Audio/Machines/short_print_and_rip.ogg");

    /// <summary>
    /// What the machine will print.
    /// </summary>
    [DataField]
    public EntProtoId<PaperComponent> MachineOutput = "PlantAnalyzerReportPaper";

    /// <summary>
    /// Delay for scanning
    /// </summary>
    [DataField]
    public float ScanDelay = 0.5f;

    /// <summary>
    /// Sound played when scanning starts
    /// </summary>
    [DataField]
    public SoundSpecifier? ScanningBeginSound;

    /// <summary>
    /// Sound played when scanning ends
    /// </summary>
    [DataField]
    public SoundSpecifier? ScanningEndSound;

    [DataField]
    public bool Silent;

    [ViewVariables]
    public EntityUid? ScannedEntity;

    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(2.5);

    [DataField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    [DataField]
    public float MaxScanRange = 2f;
}

[Serializable, NetSerializable]
public sealed partial class PlantAnalyzerDoAfterEvent : SimpleDoAfterEvent;
