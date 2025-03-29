using Robust.Shared.GameStates;

namespace Content.Shared.April.Fools.Components;

/// <summary>
/// Компонент отвечающий за генерацию рандомных оповещений ххыхыххыхыхыхы
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RandomAlertGeneratorComponent : Component
{
    [DataField]
    public float NextTimeTick { get; set; } = 30f;

    [DataField]
    public List<string> Messages = new();
}
