using Content.Shared.EntityEffects;

namespace Content.Shared._Wega.Metabolism;

[RegisterComponent]
public sealed partial class DisableMetabolismEffectsComponent : Component
{
    [DataField("allowed")]
    public List<EntityEffect> AllowedEffects = new();
}
