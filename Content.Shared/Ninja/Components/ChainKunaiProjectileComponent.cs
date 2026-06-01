using Robust.Shared.GameStates;

namespace Content.Shared.Ninja.Components;

/// <summary>
/// Marks a projectile as a chain kunai. On hit, pulls the victim to the ninja and knocks them down.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ChainKunaiProjectileComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Ninja;

    [DataField]
    public float PullSpeed = 35f;

    [DataField]
    public TimeSpan KnockdownDuration = TimeSpan.FromSeconds(2f);
}
