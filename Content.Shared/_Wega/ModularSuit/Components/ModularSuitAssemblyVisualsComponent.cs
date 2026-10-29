using Robust.Shared.Serialization;

namespace Content.Shared.Modular.Suit;

[RegisterComponent]
public sealed partial class ModularSuitAssemblyVisualsComponent : Component
{
    /// <summary>
    /// The prefix that is followed by the number which
    /// denotes the current state to use.
    /// </summary>
    [DataField("statePrefix", required: true)]
    public string StatePrefix = string.Empty;
}

[Serializable, NetSerializable]
public enum ModilarSuitAssemblyVisuals : byte
{
    State
}
