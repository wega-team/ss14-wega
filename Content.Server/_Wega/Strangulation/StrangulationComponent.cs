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
    }
}
