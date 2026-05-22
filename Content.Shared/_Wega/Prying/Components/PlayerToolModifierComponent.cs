using Robust.Shared.GameStates;

namespace Content.Shared._Wega.Prying.Components;

/// <summary>
/// измененяет скорость использования инструментов
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PlayerToolModifierComponent : Component
{
    /// <summary>
    /// Умножает время, необходимое для выполнения операции вскрытия таких объектов, как

    /// шлюзы и двери.
    /// </summary>
    [DataField]
    public float PryTimeMultiplier = 1.0f;
}
