// Developed by Nox project.
// Author: KloopRe

using Content.Shared._Modifications.Disease.Components;

namespace Content.Server._Modifications.Disease.Components;

[RegisterComponent]
public sealed partial class EnsureDiseaseIntoSolutionComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public bool ReagentAdded = false;

    [ViewVariables(VVAccess.ReadOnly)]
    [DataField]
    public DiseaseData? Data = null;
}
