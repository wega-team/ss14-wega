using Robust.Shared.GameStates;

namespace Content.Shared._Wega.Damage.Components;

/// <summary>
/// доп урон выносливости в бою
/// и атака оружием.
/// <see cref="Shared.Damage.Events.StaminaMeleeHitEvent"/>
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BonusStaminaDamageComponent : Component
{
    /// <summary>
    ///Увеличивает урон по выносливости
    /// </summary>
    [DataField]
    public float Multiplier = 1.25f;
}
