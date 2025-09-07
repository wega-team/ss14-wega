using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.DirtVisuals;

[RegisterComponent, NetworkedComponent]
public sealed partial class DirtableComponent : Component
{
    [DataField("threshold")]
    public FixedPoint2 Threshold { get; set; } = 20;

    [DataField("dirtSprite")]
    public string DirtSpritePath { get; set; } = "_Wega/Mobs/Effects/dirt_overlay.rsi";

    [DataField("dirtState")]
    public string DirtState { get; set; } = "jumpsuit";

    [DataField("foldingDirtState")]
    public string? FoldingDirtState { get; set; }

    [DataField("equippedDirtState")]
    public string EquippedDirtState { get; set; } = "equipped-jumpsuit";

    [ViewVariables]
    public FixedPoint2 CurrentDirtLevel { get; set; }

    [ViewVariables]
    public Color DirtColor { get; set; } = Color.White;

    [ViewVariables]
    public bool IsDirty => CurrentDirtLevel >= Threshold;
}

[Serializable, NetSerializable]
public sealed class DirtableComponentState : ComponentState
{
    public FixedPoint2 CurrentDirtLevel;
    public Color DirtColor;
    public bool IsDirty;

    public DirtableComponentState(FixedPoint2 currentDirtLevel, Color dirtColor, bool isDirty)
    {
        CurrentDirtLevel = currentDirtLevel;
        DirtColor = dirtColor;
        IsDirty = isDirty;
    }
}
