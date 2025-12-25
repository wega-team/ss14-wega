using Robust.Shared.GameStates;

namespace Content.Shared.Height;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SmallHeightComponent : Component
{
    [DataField, AutoNetworkedField]
    public float LastHeight = default!;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BigHeightComponent : Component
{
    [DataField, AutoNetworkedField]
    public float LastHeight = default!;
}
