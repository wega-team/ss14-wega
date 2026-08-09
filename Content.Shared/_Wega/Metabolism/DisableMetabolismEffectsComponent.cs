using Content.Shared.EntityEffects;

namespace Content.Shared._Wega.Metabolism;

[RegisterComponent]
public sealed partial class DisableMetabolismEffectsComponent : Component
{
    [DataField("allowed", required: true)]
    public List<EntityEffect> AllowedEffects = default!;
}
