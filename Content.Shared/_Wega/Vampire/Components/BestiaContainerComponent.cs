using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Shared.Vampire.Components;

[Access(typeof(SharedVampireSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class BestiaContainerComponent : Component
{
    public const string ContainerId = "organs_container";

    [ViewVariables]
    public Container OrgansContainer = default!;

    // Mew Mew Mew Mew Mew Mew Mew Mew~!
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public Dictionary<EntityUid, int> OrgansExtractedFromVictim = new();

    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public int MaxRegularOrgans = 10;

    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public int MaxCriticalOrgans = 2;

    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public int MaxOrgansPerVictim = 1;

    [ViewVariables(VVAccess.ReadOnly)]
    public List<float> UnlockedCriticalThresholds = new();

    [ViewVariables(VVAccess.ReadOnly)]
    public List<float> UnlockedVictimThresholds = new();

    [DataField("criticalThresholds")]
    public Dictionary<float, int> CriticalOrganThresholds { get; set; } = new()
    {
        { 600f, 2 },
        { 1000f, 2 }
    };

    [DataField("organsThresholds")]
    public Dictionary<float, int> OrgansPerVictimThresholds { get; set; } = new()
    {
        { 600f, 1 },
        { 1000f, 1 }
    };
}
