using Robust.Shared.GameStates;

namespace Content.Shared.Instruments;

[RegisterComponent, NetworkedComponent]
public sealed partial class PrivateListeningComponent : Component
{
    [DataField("privateListening"), ViewVariables(VVAccess.ReadWrite)]
    public bool PrivateListening { get; set; } = true;
}
