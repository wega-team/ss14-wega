using Content.Shared.Body;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Chat.TypingIndicator;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Preferences;
using Content.Shared.Speech;
using Content.Shared.Speech.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Ninja.Components;

/// <summary>
/// Snapshot of a DetailExaminableComponent state, stored for later restoration.
/// </summary>
[DataDefinition]
public sealed partial class DetailExaminableSnapshot
{
    [DataField] public string Content = string.Empty;
    [DataField] public string CharContent = string.Empty;
    [DataField] public string OOCContent = string.Empty;
    [DataField] public string TagsContent = string.Empty;
    [DataField] public string LinksContent = string.Empty;
    [DataField] public string GreenContent = string.Empty;
    [DataField] public string YellowContent = string.Empty;
    [DataField] public string RedContent = string.Empty;
    [DataField] public string NSFWContent = string.Empty;
}

/// <summary>
/// Placed on the ninja suit when the Chameleon ability is chosen.
/// Tracks disguise state and the associated fake ID card.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NinjaChameleonComponent : Component
{
    /// <summary>Whether a disguise is currently applied.</summary>
    [DataField, AutoNetworkedField]
    public bool IsDisguised;

    /// <summary>Reference to the spawned holographic ID card (if any).</summary>
    [DataField, AutoNetworkedField]
    public EntityUid? FakeIdCard;

    /// <summary>The original item that was in the ninja's id slot before the holo PDA was inserted.</summary>
    [DataField]
    public EntityUid? OriginalIdCard;

    /// <summary>Tracks the entity currently wearing the suit.</summary>
    [DataField]
    public EntityUid? WearerEntity;

    /// <summary>The scanner item currently held by the ninja, if any. Prevents spawning duplicates.</summary>
    [DataField]
    public EntityUid? ScannerEntity;

    /// <summary>Last successfully scanned humanoid. Persists across scanner toggles so the ninja can re-disguise without rescanning.</summary>
    [DataField]
    public EntityUid? StoredScanTarget;

    /// <summary>Ninja's own clothing items that had their visuals modified for the disguise. Restored from prototype on removal.</summary>
    [DataField]
    public List<EntityUid> ModifiedClothing = new();

    /// <summary>Passive charge drain per second while this module is active.</summary>
    [DataField]
    public float PassiveDrainRate = 0.2f;

    [DataField]
    public EntProtoId ScannerPrototype = "NinjaChameleonScanner";

    [DataField]
    public EntProtoId FakeIdPrototype = "HolographicNinjaIdCard";

    [DataField]
    public EntProtoId ChameleonScannerAction = "ActionNinjaGetChameleonScanner";

    [DataField, AutoNetworkedField]
    public EntityUid? ChameleonScannerActionEntity;

    // ── Original state for restoration ───────────────────────────────────────

    /// <summary>Ninja's original entity name, captured on first disguise. Used when prefs are unavailable.</summary>
    [DataField]
    public string? OriginalName;

    /// <summary>Snapshot of the ninja's DetailExaminableComponent before first disguise. Null if they had none.</summary>
    [DataField]
    public DetailExaminableSnapshot? OriginalDetailExaminable;

    /// <summary>Snapshot of the ninja's humanoid profile (species, age, sex, height) before first disguise.</summary>
    public HumanoidCharacterProfile? OriginalProfile;

    // ── Voice/speech originals ────────────────────────────────────────────────

    /// <summary>Ninja's original speech sounds.</summary>
    public ProtoId<SpeechSoundsPrototype>? OriginalSpeechSounds;

    /// <summary>Ninja's original TTS voice id.</summary>
    public string? OriginalTTSVoice;

    /// <summary>Ninja's original emote sounds per sex, from VocalComponent.</summary>
    public Dictionary<Sex, ProtoId<EmoteSoundsPrototype>>? OriginalVocalSounds;

    /// <summary>Ninja's original scream emote id.</summary>
    public string? OriginalScreamId;

    /// <summary>Ninja's original typing indicator prototype (the typing bubble).</summary>
    public ProtoId<TypingIndicatorPrototype>? OriginalTypingIndicator;

    /// <summary>Whether a typing indicator override is currently applied.</summary>
    public bool TypingIndicatorOverridden;

    // ── Body visual snapshot (server-side, for PVS-safe revert) ───────────────

    /// <summary>Per-organ sprite (RSI) data captured before the first disguise.</summary>
    public Dictionary<EntityUid, PrototypeLayerData>? OriginalOrganAppearances;

    /// <summary>Per-organ profile (skin/eye colour, sex) captured before the first disguise.</summary>
    public Dictionary<EntityUid, OrganProfileData>? OriginalOrganProfiles;

    /// <summary>Per-organ markings captured before the first disguise.</summary>
    public Dictionary<EntityUid, Dictionary<HumanoidVisualLayers, List<Marking>>>? OriginalOrganMarkings;

    // ── Clothing disguise (server-side) ───────────────────────────────────────

    /// <summary>
    /// Holographic clothing items spawned into the ninja's empty slots to mimic the target's
    /// outfit. Removed and deleted on revert.
    /// </summary>
    public List<EntityUid> SpawnedDisguiseClothing = new();

    /// <summary>
    /// The ninja's own pre-existing worn items whose visuals were overridden via the chameleon
    /// system to look like the target's items. Restored to their real visuals on revert.
    /// </summary>
    public List<EntityUid> DisguisedOwnClothing = new();

    /// <summary>
    /// The ninja's own worn items whose on-body visual was hidden (target's slot was empty).
    /// The items stay equipped (abilities preserved); their visuals are restored on revert.
    /// </summary>
    public List<EntityUid> HiddenOwnClothing = new();
}

/// <summary>Marker placed on the holographic ID card so it knows which suit to notify when destroyed.</summary>
[RegisterComponent]
public sealed partial class HolographicNinjaIdCardComponent : Component
{
    [DataField]
    public EntityUid SuitEntity;
}

/// <summary>
/// Placed on a ninja's clothing item whose visuals were overridden by the Chameleon disguise.
/// When the item is unequipped by the ninja, the disguise is removed.
/// </summary>
[RegisterComponent]
public sealed partial class NinjaModifiedClothingComponent : Component
{
    /// <summary>The ninja suit entity that manages this disguise.</summary>
    [DataField]
    public EntityUid SuitEntity;
}

/// <summary>
/// Marks a worn item whose equipped (on-body) visual should be hidden, without unequipping it —
/// used by the chameleon disguise so the ninja can mimic a target whose corresponding slot is
/// empty, while keeping the item's gameplay components (e.g. ninja abilities) active.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NinjaHiddenClothingComponent : Component
{
}
