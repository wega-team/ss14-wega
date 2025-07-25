using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Clothing.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ToggleableSpriteClothingComponent : Component
{
    [DataField("defaultSuffix")]
    public string DefaultSuffix = string.Empty;

    [DataField("activeSuffix")]
    public string ActiveSuffix = string.Empty;

    [ViewVariables]
    public bool IsToggled => !string.IsNullOrEmpty(ActiveSuffix);

    [DataField]
    public float DoAfterTime = 0.75f;

    public SoundPathSpecifier Sound = new SoundPathSpecifier("/Audio/Items/jumpsuit_equip.ogg");
}

[Serializable, NetSerializable]
public sealed class ToggleableSpriteClothingComponentState : ComponentState
{
    public string ActiveSuffix;

    public ToggleableSpriteClothingComponentState(string activeSuffix)
    {
        ActiveSuffix = activeSuffix;
    }
}
