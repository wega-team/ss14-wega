using Robust.Shared.Prototypes;

namespace Content.Shared.Mind;

[Prototype("mindChannel")]
public sealed partial class MindChannelPrototype : IPrototype
{
    /// <summary>
    /// Human-readable name for the channel.
    /// </summary>
    [DataField("name")]
    public LocId Name { get; private set; } = string.Empty;

    [ViewVariables(VVAccess.ReadOnly)]
    public string LocalizedName => Loc.GetString(Name);

    /// <summary>
    /// Single-character prefix to determine what channel a message should be sent to.
    /// </summary>
    [DataField("keycode")]
    public char KeyCode { get; private set; } = '\0';

    [DataField("color")]
    public Color Color { get; private set; } = Color.Peru;

    [IdDataField, ViewVariables]
    public string ID { get; private set; } = default!;
}
