using Robust.Shared.Audio;

namespace Content.Server.Lavaland.Mobs.Components;

[RegisterComponent]
public sealed partial class MegafaunaComponent : Component
{
    [ViewVariables]
    public bool IsActive = true;

    [DataField("bossMusic")]
    public SoundSpecifier? BossMusic;

    [DataField("aggroSound")]
    public SoundSpecifier? AggroSound;

    [DataField]
    public string TargetKey = "Target";
}
