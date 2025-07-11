using Content.Shared.Damage.Prototypes;
using Robust.Shared.GameStates;

namespace Content.Shared.Damage.Components
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class DamageResistComponent : Component
    {
        [DataField, ViewVariables(VVAccess.ReadOnly)]
        public Dictionary<DamageTypePrototype, (float ResistFactor, TimeSpan EndTime)> Resistances = new();
    }
}