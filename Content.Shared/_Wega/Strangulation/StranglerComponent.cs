using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared.Strangulation
{
    [RegisterComponent]
    public sealed partial class StranglerComponent : Component
    {
        [DataField]
        public EntityUid? Target;

        [DataField("freeHandsRequired")]
        public int FreeHandsRequired = 2;
    }
}
