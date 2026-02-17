namespace Content.Shared.WormInfected;

[RegisterComponent, NetworkedComponent]
public sealed partial class WormInfectedComponent : Component
{
    /// <summary>
    ///     UID Владельца борера
    /// </summary>
    [ViewVariables]
    public Entity<WormInfectedComponent> Owner = null;

    /// <summary>
    ///     Количество очков эволюции. В отличии от химических, появляются за счёт репродукции, требуются для улучшения.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField, DataField]
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
    public bool Reproduce = False;
}