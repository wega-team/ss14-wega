// Developed by Nox project.
// Author: KloopRe

using Content.Shared._Modifications.Disease;

namespace Content.Server._Modifications.Disease.Components;

[RegisterComponent]
public sealed partial class DiseaseHealingBedComponent : Component
{
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public DiseaseHealingBedType RegenerationType = DiseaseHealingBedType.Normal;
}
