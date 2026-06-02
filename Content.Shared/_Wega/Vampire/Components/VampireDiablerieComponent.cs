using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Vampire.Components;

[Access(typeof(SharedVampireSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class VampireDiablerieComponent : Component
{
    public static readonly EntProtoId SacramentInitiationActionPrototype = "ActionVampireSacramentInitiation";

    public EntityUid? SacramentInitiationActionEntity;

    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public int DiablerieLevel = 0;

    [DataField]
    public int MaxDiablerieLevel = 4;

    [DataField]
    public float SuckingBonusPerLevel = 5f;
}
