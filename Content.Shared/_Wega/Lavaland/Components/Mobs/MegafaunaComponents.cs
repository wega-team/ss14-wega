using Robust.Shared.Audio;

namespace Content.Shared.Lavaland.Components;

[RegisterComponent]
public sealed partial class MegafaunaComponent : Component
{
    [ViewVariables]
    public bool IsActive = true;

    [DataField("aggroSound")]
    public SoundSpecifier? AggroSound;

    [DataField]
    public string TargetKey = "Target";
}
