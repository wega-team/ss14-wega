using Content.Shared.Damage.Prototypes;
using Content.Shared.Metabolism;
using Robust.Shared.Prototypes;

namespace Content.Shared.Vampire.Components;

/// <summary>
/// Stores the creature's original data before it becomes a vampire.
/// Used for restoration when the vampire component is removed.
/// </summary>
[RegisterComponent, Access(typeof(SharedVampireSystem))]
public sealed partial class VampireOriginalStateComponent : Component
{
    [DataField]
    public HashSet<Type> RemovedComponents = new();

    [DataField]
    public Dictionary<EntityUid, HashSet<ProtoId<MetabolizerTypePrototype>>> OriginalMetabolizerTypes = new();

    [DataField]
    public float? OriginalColdDamageThreshold;

    [DataField]
    public ProtoId<DamageModifierSetPrototype>? OriginalDamageModifierSetId;

    [DataField]
    public Color? OriginalEyeColor;
}
