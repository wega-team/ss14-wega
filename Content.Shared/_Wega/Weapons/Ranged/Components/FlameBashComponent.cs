using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Wega.Weapons.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class FlameBashComponent : Component
{
    [DataField, ViewVariables]
    public int RequiredAmmo = 10;

    [DataField, ViewVariables]
    public float FlameRange = 2f;

    [DataField, ViewVariables]
    public float FlameWidth = 2f;

    [DataField, ViewVariables]
    public float FlameOffset = 0.7f;

    [DataField, ViewVariables]
    public float FlameDamage = 25f;

    [DataField, ViewVariables]
    public float DamageFalloff = 0.8f;

    [DataField, ViewVariables]
    public float FireStacks = 6f;

    [DataField, ViewVariables]
    public SoundSpecifier? FlameSound;

    [DataField]
    public EntProtoId FlameEffect = "EffectFlameBash";

}