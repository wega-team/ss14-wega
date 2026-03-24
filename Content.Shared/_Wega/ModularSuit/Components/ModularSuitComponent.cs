using Content.Shared.Actions.Components;
using Content.Shared.Tools;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Modular.Suit;

[Access(typeof(SharedModularSuitSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class ModularSuitComponent : Component
{
    public EntProtoId<InstantActionComponent> ToggleUiAction = "ToggleModularSuitUiAction";

    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public EntityUid? Wearer;

    [DataField, AutoNetworkedField]
    public EntityUid? ToggleUiActionEntity;

    [DataField]
    public float BasePowerDraw = 0.5f;

    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    [ViewVariables]
    public TimeSpan NextUpdate;

    [DataField, AutoNetworkedField]
    public bool Assembled;

    [DataField, AutoNetworkedField]
    public bool Active;

    [DataField("activeComponents")]
    public Dictionary<string, ComponentRegistry>? SlotActiveComponents { get; set; }

    [DataField("blacklist")]
    public EntityWhitelist? BlacklistModules { get; set; }

    [DataField]
    public ProtoId<ToolQualityPrototype> Tool = "Screwing";

    [DataField]
    public SoundSpecifier UiOpenSound = new SoundPathSpecifier("/Audio/Effects/newplayerping.ogg");

    [DataField]
    public SoundSpecifier EjectSound = new SoundPathSpecifier("/Audio/Weapons/Guns/MagOut/revolver_magout.ogg");

    [DataField]
    public SoundSpecifier InsertSound = new SoundPathSpecifier("/Audio/Weapons/Guns/MagIn/revolver_magin.ogg");

    [DataField]
    public SoundSpecifier LowPowerSound = new SoundPathSpecifier("/Audio/_Wega/Effects/Modsuit/lowpower.ogg");

    [DataField]
    public SoundSpecifier CriticalDamageSound = new SoundPathSpecifier("/Audio/_Wega/Effects/Modsuit/critnano.ogg");

    [DataField]
    public SoundSpecifier CriticalDestroySound = new SoundPathSpecifier("/Audio/_Wega/Effects/Modsuit/critdestr.ogg");

    [DataField]
    public SoundSpecifier NominalSound = new SoundPathSpecifier("/Audio/_Wega/Effects/Modsuit/nominal.ogg");

    [DataField]
    public TimeSpan LowPowerCooldown = TimeSpan.FromSeconds(30);

    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextLowPowerSound;
}
