using Robust.Shared.GameStates;

namespace Content.Shared.Feroxi;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BloodSenseActiveComponent : Component
{
    [AutoNetworkedField]
    public Dictionary<EntityUid, float> Targets = new();

    public TimeSpan NextUpdate;
}
