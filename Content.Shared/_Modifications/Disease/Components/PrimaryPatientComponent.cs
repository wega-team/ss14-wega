// Developed by Nox project.
// Author: KloopRe

using Content.Shared._Modifications.TimeWindow;
using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Modifications.Disease.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class PrimaryPatientComponent : Component
{
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public string StrainId = "";

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? SentientDisease = default!;

    /// <summary>
    ///     Радиус распространения вируса.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public float RangeInfect = 2f;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public TimedWindow UpdateWindow = new TimedWindow(TimeSpan.FromSeconds(1f), TimeSpan.FromSeconds(5f));

    public PrimaryPatientComponent(EntityUid sentientDisease, string strainId)
    {
        StrainId = strainId;
        SentientDisease = sentientDisease;
    }

    public PrimaryPatientComponent(string strainId)
    {
        StrainId = strainId;
    }

    public PrimaryPatientComponent()
    { }

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "PrimaryPatientFaction";
}

