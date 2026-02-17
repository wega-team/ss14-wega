using Robust.Shared.Containers;

namespace Content.Shared.WormInfected;

[RegisterComponent, NetworkedComponent]
public sealed partial class WormInfectedComponent : Component
{
    /// <summary>
    ///     UID и компонент владельца
    /// </summary>
    [ViewVariables]
    public Entity<WormComponent> Borer = new();

    /// <summary>
    ///     Контейнер для борера
    /// </summary>
    [ViewVariables]
    public Container BorerContainer = new();
}