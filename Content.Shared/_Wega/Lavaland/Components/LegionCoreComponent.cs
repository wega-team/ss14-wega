using Content.Shared.Damage;
using Content.Shared.Surgery;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Lavaland.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class LegionCoreComponent : Component
{
    [DataField] public bool Active = true;
    [DataField] public bool AlwaysActive;
    [DataField(required: true)] public DamageSpecifier HealAmount;
    [DataField("internals", required: true)]
    public List<ProtoId<InternalDamagePrototype>> HealInternals;

    [DataField] public TimeSpan ActiveInterval = TimeSpan.FromSeconds(150);
    [ViewVariables] public TimeSpan ActiveEndTime;
}
