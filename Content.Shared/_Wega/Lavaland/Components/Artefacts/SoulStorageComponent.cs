namespace Content.Shared.Lavaland.Artefacts.Components;

[RegisterComponent]
public sealed partial class SoulStorageComponent : Component
{
    [DataField]
    public float MaxDamageModifier = 76f;

    [DataField]
    public float ModifierPerCount = 4f;

    [ViewVariables(VVAccess.ReadOnly)]
    public HashSet<EntityUid> CurrentStolen = new();
}
