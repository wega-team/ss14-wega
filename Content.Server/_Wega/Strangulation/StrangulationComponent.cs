using Content.Shared.Damage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server.Strangulation
{
    [RegisterComponent]
    public sealed partial class StrangulationComponent : Component
    {
        [DataField]
        public bool IsStrangled = false;

        [DataField]
        public TimeSpan GaspEmoteCooldown = TimeSpan.FromSeconds(6);

        [ViewVariables]
        public TimeSpan LastGaspEmoteTime;

        [DataField(required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = new DamageSpecifier { DamageDict = { { "Asphyxiation", 2 } } };

        [DataField(required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Garrota_Damage = default!;
    }
}
