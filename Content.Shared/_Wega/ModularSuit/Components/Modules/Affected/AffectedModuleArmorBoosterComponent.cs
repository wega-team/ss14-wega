using Content.Shared.Damage;

namespace Content.Shared.Modular.Suit;

[RegisterComponent]
public sealed partial class AffectedModuleArmorBoosterComponent : Component
{
    [DataField(required: true)]
    public DamageModifierSet Modifiers = new();
}
