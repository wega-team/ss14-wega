using Robust.Shared.GameStates;

namespace Content.Shared._Wega.Aliens.Facehugger;

[RegisterComponent, NetworkedComponent]
public sealed partial class FacehuggerComponent : Component
{
    [DataField]
    public float ProximityRange = 1.5f;

    [DataField]
    public TimeSpan AttachDelay = TimeSpan.FromSeconds(1);

    [ViewVariables]
    public EntityUid? CurrentTarget;

    [ViewVariables]
    public TimeSpan? AttachTime;
}
