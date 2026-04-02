using Content.Shared.Chemistry;
using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.Modular.Suit;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class AffectedModuleSpringlockComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public bool Locked = false;

    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public bool Triggered = false;

    [DataField]
    public float SpeedMultiplier = 3f;

    [DataField]
    public ReactionMethod LockMethod = ReactionMethod.Touch;

    [DataField]
    public string TargetReagent = "Water";

    [DataField]
    public DamageSpecifier LockDamage = new DamageSpecifier
    {
        DamageDict =
        {
            { "Blunt", 20 },
            { "Slash", 40 },
            { "Piercing", 60 }
        }
    };
}
