using Content.Shared._Modifications.TimeWindow;

namespace Content.Shared._Modifications.Disease.Components;

[RegisterComponent]
public sealed partial class DiseaseInfectionDetectorUserComponent : Component
{
    [ViewVariables]
    public int Count;
}