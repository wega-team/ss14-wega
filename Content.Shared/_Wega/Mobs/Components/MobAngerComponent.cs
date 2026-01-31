namespace Content.Shared.Mobs.Anger.Components;

[RegisterComponent, Access(typeof(MobAngerSystem))]
public sealed partial class MobAngerComponent : Component
{
    [DataField] public bool Anger;
    [DataField] public float AngerThreshold = 0.33f;

    // Buffs
    [DataField] public float SpeedModifier = 1f;
    [DataField] public float DamageModifier = 1f;
}
