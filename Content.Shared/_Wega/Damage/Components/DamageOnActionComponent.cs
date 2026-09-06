using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.Actions;

namespace Content.Shared.Damage.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DamageOnActionComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public DamageSpecifier Damage = default!;

    [DataField("flyAction")]
    public EntProtoId? Action = "InstantRegeneration";

    [DataField("ActionEntity")] public EntityUid? ActionEntity;

    [DataField]
    public float HungerPerUse = 35f;

    [DataField("cooldown")]
    public double Cooldown = 80.0;
    public TimeSpan Delay => TimeSpan.FromSeconds(Cooldown);
}

public sealed partial class DamageOnActionEvent : InstantActionEvent;
