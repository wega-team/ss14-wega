using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Shared.VentCraw.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class VentCrawHolderComponent : Component
{
    public Container Container = null!;

    [ViewVariables]
    public float StartingTime { get; set; }

    [ViewVariables]
    public float TimeLeft { get; set; }

    public bool IsMoving = false;

    [ViewVariables]
    public EntityUid? PreviousTube { get; set; }

    [ViewVariables]
    public EntityUid? NextTube { get; set; }

    [ViewVariables]
    public Direction PreviousDirection { get; set; } = Direction.Invalid;

    [ViewVariables]
    public int PreviousPipeLayer { get; set; } = 0;

    [ViewVariables]
    public EntityUid? CurrentTube { get; set; }

    [ViewVariables]
    public bool FirstEntry { get; set; }

    [ViewVariables]
    public Direction CurrentDirection { get; set; } = Direction.Invalid;

    [ViewVariables]
    public bool IsExitingVentCraws { get; set; }

    public TimeSpan LastCrawl;

    [DataField("crawlSound")]
    public SoundCollectionSpecifier CrawlSound { get; set; } = new("VentClaw", AudioParams.Default.WithVolume(5f));

    [DataField("speed")]
    public float Speed = 0.15f;
}
