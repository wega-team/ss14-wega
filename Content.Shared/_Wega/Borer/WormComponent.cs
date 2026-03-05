using Robust.Shared.Prototypes;
using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Content.Shared.Borer.BorerInfectedEr;
using Robust.Shared.Serialization;
using Content.Shared.Actions.Components;

namespace Content.Shared.Borer.Wormer;

[RegisterComponent, NetworkedComponent]
public sealed partial class BorerComponent : Component
{
    /// <summary>
    ///     UID Владельца борера
    /// </summary>
    [ViewVariables]
    public Entity<BorerInfectedComponent> Owner = new();

    /// <summary>
    ///     Количество очков эволюции. В отличии от химических, появляются за счёт репродукции, требуются для улучшения.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public int EvolutionPoints = 0;

    /// <summary>
    ///     Количество очков химикатов. Требуются для активации способностей, впрыскивающих химикаты.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float ChemicalPoints = 50f;

    /// <summary>
    ///     Максимальное количество очков химикатов.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float MaxChemicalPoints = 250f;

    /// <summary>
    ///     Приток химических очков, когда борер в носителе.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public int ChemicalPointsPerSecond = 0;

    /// <summary>
    ///     Забирает возможность размножаться, если допустим борер достиг пика эволюции.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public bool Reproduce = false;
}
