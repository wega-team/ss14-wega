using Content.Shared.Actions;
using Content.Shared.Ninja.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Ninja.Components;

/// <summary>
/// Component for ninja suit abilities and power consumption.
/// As an implementation detail, dashing with katana is a suit action which isn't ideal.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedNinjaSuitSystem))]
public sealed partial class NinjaSuitComponent : Component
{
    /// <summary>
    /// Sound played when a ninja is hit while cloaked.
    /// </summary>
    [DataField]
    public SoundSpecifier RevealSound = new SoundPathSpecifier("/Audio/Effects/chime.ogg");

    /// <summary>
    /// ID of the use delay to disable all ninja abilities.
    /// </summary>
    [DataField]
    public string DisableDelayId = "suit_powers";

    /// <summary>
    /// The action id for recalling a bound energy katana
    /// </summary>
    [DataField]
    public EntProtoId RecallKatanaAction = "ActionRecallKatana";

    [DataField, AutoNetworkedField]
    public EntityUid? RecallKatanaActionEntity;

    /// <summary>
    /// Battery charge used per tile the katana teleported.
    /// Uses 1% of a default battery per tile.
    /// </summary>
    [DataField]
    public float RecallCharge = 3.6f;

    /// <summary>
    /// The action id for creating an EMP burst
    /// </summary>
    [DataField]
    public EntProtoId EmpAction = "ActionNinjaEmp";

    [DataField, AutoNetworkedField]
    public EntityUid? EmpActionEntity;

    /// <summary>
    /// Battery charge used to create an EMP burst. Can do it 2 times on a small-capacity power cell.
    /// </summary>
    [DataField]
    public float EmpCharge = 180f;

    /// <summary>
    /// The action id for toggling Spirit Form (wall phasing).
    /// </summary>
    [DataField]
    public EntProtoId SpiritFormAction = "ActionToggleSpiritForm";

    [DataField, AutoNetworkedField]
    public EntityUid? SpiritFormActionEntity;

    /// <summary>
    /// The action id for throwing a chain kunai.
    /// </summary>
    [DataField]
    public EntProtoId ChainKunaiAction = "ActionChainKunai";

    [DataField, AutoNetworkedField]
    public EntityUid? ChainKunaiActionEntity;

    /// <summary>
    /// The action id for deploying a smoke screen.
    /// </summary>
    [DataField]
    public EntProtoId SmokeScreenAction = "ActionNinjaSmokeScreen";

    [DataField, AutoNetworkedField]
    public EntityUid? SmokeScreenActionEntity;

    /// <summary>
    /// The action id for summoning two energy clones.
    /// </summary>
    [DataField]
    public EntProtoId EnergyClonesAction = "ActionNinjaEnergyClones";

    [DataField, AutoNetworkedField]
    public EntityUid? EnergyClonesActionEntity;

    /// <summary>
    /// The action id for emergency teleportation (usable while sleeping).
    /// </summary>
    [DataField]
    public EntProtoId EmergencyTeleportAction = "ActionNinjaEmergencyTeleport";

    [DataField, AutoNetworkedField]
    public EntityUid? EmergencyTeleportActionEntity;

    /// <summary>
    /// The action id for dashing with the energy katana.
    /// </summary>
    [DataField]
    public EntProtoId DashAction = "ActionEnergyKatanaDash";

    [DataField, AutoNetworkedField]
    public EntityUid? DashActionEntity;

    /// <summary>
    /// The action id for the adrenaline burst (injects hyperzine + uranium from the suit's uranium tank).
    /// </summary>
    [DataField]
    public EntProtoId AdrenalineBurstAction = "ActionNinjaAdrenalineBurst";

    [DataField, AutoNetworkedField]
    public EntityUid? AdrenalineBurstActionEntity;

    /// <summary>
    /// The action id for the healing cocktail (injects Chiurizin from the suit's uranium tank).
    /// </summary>
    [DataField]
    public EntProtoId HealingCocktailAction = "ActionNinjaHealingCocktail";

    [DataField, AutoNetworkedField]
    public EntityUid? HealingCocktailActionEntity;

    /// <summary>
    /// The action id for throwing an energy net that immobilises a target.
    /// </summary>
    [DataField]
    public EntProtoId EnergyNetAction = "ActionNinjaEnergyNet";

    [DataField, AutoNetworkedField]
    public EntityUid? EnergyNetActionEntity;

    /// <summary>
    /// The action id for getting a chameleon scanner (spawns the scanner item).
    /// </summary>
    [DataField]
    public EntProtoId ChameleonScannerAction = "ActionNinjaGetChameleonScanner";

    [DataField, AutoNetworkedField]
    public EntityUid? ChameleonScannerActionEntity;

    /// <summary>
    /// The action id for deploying electro-caltrop traps behind the ninja.
    /// </summary>
    [DataField]
    public EntProtoId CaltropAction = "ActionNinjaCaltrop";

    [DataField, AutoNetworkedField]
    public EntityUid? CaltropActionEntity;


    // TODO: EmpOnTrigger bruh
    /// <summary>
    /// Range of the EMP in tiles.
    /// </summary>
    [DataField]
    public float EmpRange = 6f;

    /// <summary>
    /// Power consumed from batteries by the EMP
    /// </summary>
    [DataField]
    public float EmpConsumption = 100000f;

    /// <summary>
    /// How long the EMP effects last for
    /// </summary>
    [DataField]
    public TimeSpan EmpDuration = TimeSpan.FromSeconds(60);
}

public sealed partial class RecallKatanaEvent : InstantActionEvent;

public sealed partial class NinjaEmpEvent : InstantActionEvent;

public sealed partial class ToggleSpiritFormEvent : InstantActionEvent;

public sealed partial class ChainKunaiEvent : WorldTargetActionEvent;

public sealed partial class NinjaSmokeScreenEvent : InstantActionEvent;

public sealed partial class NinjaEnergyClonesEvent : InstantActionEvent;

public sealed partial class NinjaEmergencyTeleportEvent : InstantActionEvent;

public sealed partial class NinjaAdrenalineBurstEvent : InstantActionEvent;

public sealed partial class NinjaHealingCocktailEvent : InstantActionEvent;

public sealed partial class NinjaEnergyNetEvent : WorldTargetActionEvent;

public sealed partial class NinjaGetChameleonScannerEvent : InstantActionEvent;

public sealed partial class NinjaCaltropEvent : InstantActionEvent;
