using Content.Shared.Alert;
using Content.Shared.Body.Prototypes;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.StatusIcon;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Vampire.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class VampireComponent : Component
{
    public static readonly ProtoId<MetabolizerTypePrototype> MetabolizerVampire = "Vampire";

    public static readonly DamageSpecifier HolyDamage = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>() { { "Heat", 10 } }
    };

    public static readonly DamageSpecifier SpaceDamage = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>() { { "Heat", 2.5 } }
    };

    public static readonly EntProtoId DrinkActionPrototype = "ActionDrinkBlood";
    public static readonly EntProtoId SelectClassActionPrototype = "ActionVampireSelectClass";
    public static readonly EntProtoId RejuvenateActionPrototype = "ActionVampireRejuvenate";
    public static readonly EntProtoId GlareActionPrototype = "ActionVampireGlare";

    public readonly SoundSpecifier BloodDrainSound = new SoundPathSpecifier(
        "/Audio/Items/drink.ogg",
        new AudioParams() { Volume = -3f, MaxDistance = 3f }
    );

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public string? CurrentEvolution { get; set; }

    /// <summary>
    /// The current amount of blood in the vampire's account.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public FixedPoint2 CurrentBlood = 0;

    [DataField("vampireStatusIcon")]
    public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "VampireFaction";

    [DataField]
    public ProtoId<AlertPrototype> BloodAlert = "BloodAlert";

    /// <summary>
    /// Fields for counting the total amount of blood consumed after the end of the round
    /// </summary>
    public float TotalBloodDrank = 0;

    public float NextSpaceDamageTick { get; set; }

    public float NextNullDamageTick { get; set; }

    public bool TruePowerActive = false;

    public bool PowerActive = false;

    public bool IsDamageSharingActive = false;

    [DataField]
    public FixedPoint2 NullDamage = 0;

    [DataField]
    public int ThrallCount = 0;

    [DataField]
    public int MaxThrallCount = 1;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public List<EntityUid> ThrallOwned = new();

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public List<string> AcquiredSkills = new List<string>();
}

/// <summary>
/// Marks an entity as taking damage when hit by a bible, rather than being healed
/// </summary>
[RegisterComponent]
public sealed partial class UnholyComponent : Component;

[RegisterComponent]
public sealed partial class BeaconSoulComponent : Component
{
    [DataField]
    public EntityUid VampireOwner = EntityUid.Invalid;
}

/// <summary>
/// A component for testing vampire arson near holy sites.
/// </summary>
[RegisterComponent]
public sealed partial class HolyPointComponent : Component
{
    [DataField]
    public float Range = 6f;

    public float NextTimeTick { get; set; }
}

[Serializable, NetSerializable]
public enum VampireVisualLayers : byte
{
    Digit1,
    Digit2,
    Digit3
}
