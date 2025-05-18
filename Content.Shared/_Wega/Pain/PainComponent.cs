using System.Linq;
using Robust.Shared.Prototypes;

namespace Content.Shared.Pain.Components;

[RegisterComponent]
public sealed partial class PainComponent : Component
{
    [DataField("profile", required: true)]
    public ProtoId<PainProfilePrototype> Profile;

    [DataField("painLevels")]
    public Dictionary<string, PainLevel> PainLevels = new();

    [ViewVariables]
    public float TotalPain => PainLevels.Values.Sum(p => p.CurrentLevel);
}
