using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;

namespace Content.Shared.Strangulation
{
    [RegisterComponent]
    public sealed partial class StrangulationComponent : Component
    {
        [DataField]
        public EntityUid? Strangler;

        [DataField]
        public DoAfterId? DoAfterId;

        [DataField]
        public DoAfterId? BreakFreeDoAfterId;

        [DataField]
        public bool Cancelled = false;

        [DataField]
        public bool IsStrangledGarrotte = false;

        [DataField(required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = new DamageSpecifier { DamageDict = { { "Asphyxiation", 2 } } };

        [DataField]
        public ProtoId<AlertPrototype> StrangledAlert = "StrangledAlert";
    }
}
