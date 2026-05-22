using Robust.Shared.GameStates;
using Robust.Shared.Maths;

namespace Content.Shared._Wega.Weapons.Ranged.Components;

/// <summary>
/// Изменяет точность стрельбы прикрепленных объектов, удерживаемых или вооруженных огнестрельным оружием.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PlayerAccuracyModifierComponent : Component
{
    /// <summary>
    /// минимальный/максимальный углы наклона орудия на эту величину.
    /// </summary>
    [DataField]
    public float SpreadMultiplier = 1f;

    /// <summary>
    /// Максимальный угол в градусах, в пределах которого объект может стрелять.
    /// После применения множителя разброса этот ограничитель может предотвратить стрельбу объекта за его спину.
    /// </summary>
    [DataField]
    public float MaxSpreadAngle = 180f;
}
