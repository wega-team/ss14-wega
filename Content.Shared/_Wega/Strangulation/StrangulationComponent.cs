using Content.Shared.Damage;

namespace Content.Shared.Strangulation
{
    [RegisterComponent]
    public sealed partial class StrangulationComponent : Component
    {
        [DataField]
        public TimeSpan GaspEmoteCooldown = TimeSpan.FromSeconds(6);

        [ViewVariables]
        public TimeSpan LastGaspEmoteTime;

        [DataField]
        public bool IsStrangledGarrotte = false;

        [DataField(required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = new DamageSpecifier { DamageDict = { { "Asphyxiation", 2 } } };
    }
}
