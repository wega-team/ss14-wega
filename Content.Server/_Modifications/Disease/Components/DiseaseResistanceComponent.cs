// Developed by Nox project.
// Author: KloopRe

namespace Content.Server._Modifications.Disease.Components;

/// <summary>
///     Логика резистов инфекции.
/// </summary>
[RegisterComponent]
public sealed partial class DiseaseResistanceComponent : Component
{
    [DataField("coef")]
    [ViewVariables(VVAccess.ReadOnly)]
    public float DiseaseResistanceCoefficient = 0.1f;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public LocId Examine = "disease-resistance-coefficient-value";
}
