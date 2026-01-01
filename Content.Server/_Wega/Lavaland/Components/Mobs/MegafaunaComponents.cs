using Robust.Shared.Audio;

namespace Content.Server.Lavaland.Mobs.Components;

[RegisterComponent]
public sealed partial class MegafaunaComponent : Component
{
    [ViewVariables]
    public bool IsActive = false;

    [DataField("bossMusic")]
    public SoundSpecifier? BossMusic;

    [DataField("aggroSound")]
    public SoundSpecifier? AggroSound;

    [ViewVariables]
    public EntityUid? PlayedSound;
}

[RegisterComponent]
public sealed partial class MegafaunaAwarenessComponent : Component
{
    [ViewVariables]
    public List<EntityUid> Aggressors = new();

    [DataField("aggroRange")]
    public float AggroRange = 15f;

    [DataField("autoAggro")]
    public bool AutoAggro = true;
}

[RegisterComponent]
public sealed partial class MegafaunaAttacksComponent : Component
{
    [ViewVariables]
    public TimeSpan NextAttackTime = TimeSpan.Zero;

    [DataField("baseAttackCooldown")]
    public float BaseAttackCooldown = 3f;

    [DataField("attackRange")]
    public float AttackRange = 15f;
}
