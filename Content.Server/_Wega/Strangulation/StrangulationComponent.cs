using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// пока не используется
namespace Content.Server.Strangulation
{
    public sealed partial class StrangulationComponent : Component
    {
        [DataField]
        public bool IsStrangled = false;
    }
}
