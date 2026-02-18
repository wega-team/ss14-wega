using Robust.Shared.Prototypes;
using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Content.Shared.Borer.Wormer;
using Robust.Shared.Containers;

namespace Content.Shared.Borer.BorerInfectedEr;

[RegisterComponent, NetworkedComponent]
public sealed partial class BorerInfectedComponent : Component
{
    /// <summary>
    ///     UID и компонент владельца
    /// </summary>
    [ViewVariables]
    public Entity<BorerComponent> Borer = new();

    /// <summary>
    ///     Контейнер для борера
    /// </summary>
    [ViewVariables]
    public Container BorerContainer = new();
}