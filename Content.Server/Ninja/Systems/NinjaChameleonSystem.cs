using Content.Server.Ninja.Components;
using Content.Server.Speech;
using Content.Server.Speech.EntitySystems;
using Content.Shared.Chat.TypingIndicator;
using Content.Shared.Corvax.TTS;
using Content.Shared.FixedPoint;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Body;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.DetailExaminable;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.Ninja.Components;
using Content.Shared.Ninja.Systems;
using Content.Shared.PDA;
using Content.Shared.Popups;
using Content.Shared.Preferences;
using Content.Shared.Speech;
using Content.Shared.Speech.Components;
using Content.Shared.VoiceMask;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.Ninja.Systems;

/// <summary>
/// Manages the Chameleon ninja ability:
/// – gives the ninja a scanner item on demand
/// – scans a target humanoid (2 s) and stores their data on the scanner
/// – self-use of a loaded scanner (3 s) copies name, voice, appearance and examine description
/// – spawns a fragile holographic ID card in the ninja's ID slot
/// – applies a small passive power drain while the module is active
/// – disguise is broken by any incoming damage
/// </summary>
public sealed partial class NinjaChameleonSystem : EntitySystem
{
    [Dependency] private SpaceNinjaSystem              _ninja             = default!;
    [Dependency] private SharedHandsSystem             _hands             = default!;
    [Dependency] private SharedPopupSystem             _popup             = default!;
    [Dependency] private SharedDoAfterSystem           _doAfter           = default!;
    [Dependency] private MetaDataSystem                _metaData          = default!;
    [Dependency] private IdentitySystem                _identity          = default!;
    [Dependency] private InventorySystem               _inventory         = default!;
    [Dependency] private HumanoidProfileSystem         _humanoidProfile   = default!;
    [Dependency] private SharedVisualBodySystem        _visualBody        = default!;
    [Dependency] private SharedChameleonClothingSystem _chameleonClothing = default!;
    [Dependency] private SharedContainerSystem         _containerSystem   = default!;
    [Dependency] private VocalSystem                   _vocal             = default!;
    [Dependency] private SharedTypingIndicatorSystem   _typingIndicator   = default!;
    [Dependency] private SharedJobStatusSystem          _jobStatus         = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NinjaChameleonComponent, NinjaGetChameleonScannerEvent>(OnGetScanner);
        SubscribeLocalEvent<NinjaChameleonComponent, ClothingGotEquippedEvent>(OnSuitEquipped);
        SubscribeLocalEvent<NinjaChameleonComponent, ClothingGotUnequippedEvent>(OnSuitUnequipped);

        SubscribeLocalEvent<ChameleonScannerComponent, AfterInteractEvent>(OnScannerInteract);
        SubscribeLocalEvent<ChameleonScannerComponent, ChameleonScanDoAfterEvent>(OnScanDoAfter);
        SubscribeLocalEvent<ChameleonScannerComponent, ChameleonTransformDoAfterEvent>(OnTransformDoAfter);

        SubscribeLocalEvent<ChameleonScannerComponent, ComponentShutdown>(OnScannerShutdown);
        SubscribeLocalEvent<HolographicNinjaIdCardComponent, ComponentShutdown>(OnFakeIdShutdown);

        // Accent relay: re-raise AccentGetEvent on the scanned target so the ninja sounds like them.
        SubscribeLocalEvent<ChameleonAccentRelayComponent, AccentGetEvent>(OnAccentRelay, after: [typeof(AccentSystem)]);

        SubscribeLocalEvent<SpaceNinjaComponent, DamageChangedEvent>(OnNinjaDamaged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<NinjaChameleonComponent>();
        while (query.MoveNext(out var suitUid, out var chameleon))
        {
            if (chameleon.WearerEntity is not { } wearer)
                continue;

            _ninja.TryUseCharge(wearer, chameleon.PassiveDrainRate * frameTime);
        }
    }

    // ── Scanner spawn ─────────────────────────────────────────────────────────

    private void OnGetScanner(Entity<NinjaChameleonComponent> ent, ref NinjaGetChameleonScannerEvent args)
    {
        // Toggle: delete scanner if already held
        if (ent.Comp.ScannerEntity is { } existing && Exists(existing))
        {
            QueueDel(existing);
            return;
        }

        var user = args.Performer;

        if (!_hands.TryGetEmptyHand(user, out _))
        {
            _popup.PopupEntity(Loc.GetString("ninja-hands-full"), user, user);
            return;
        }

        args.Handled = true;

        var scanner = Spawn(ent.Comp.ScannerPrototype, Transform(user).Coordinates);

        if (TryComp<ChameleonScannerComponent>(scanner, out var scannerComp))
        {
            scannerComp.SuitEntity = ent.Owner;
            // Restore previously scanned target so the ninja can re-disguise immediately.
            if (ent.Comp.StoredScanTarget is { } stored && Exists(stored))
                scannerComp.ScannedTarget = stored;
        }

        EnsureComp<UnremoveableComponent>(scanner);

        ent.Comp.ScannerEntity = scanner;

        _hands.TryPickupAnyHand(user, scanner);
    }

    // ── Suit equip / unequip ──────────────────────────────────────────────────

    private void OnSuitEquipped(Entity<NinjaChameleonComponent> ent, ref ClothingGotEquippedEvent args)
    {
        ent.Comp.WearerEntity = args.Wearer;
    }

    private void OnSuitUnequipped(Entity<NinjaChameleonComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        RemoveDisguise(ent.Owner, ent.Comp, args.Wearer);
        ent.Comp.WearerEntity = null;
    }

    // ── Scanner interaction ───────────────────────────────────────────────────

    private void OnScannerInteract(Entity<ChameleonScannerComponent> scanner, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target is not { Valid: true } target)
            return;

        if (!TryComp<NinjaChameleonComponent>(scanner.Comp.SuitEntity, out _))
            return;

        // Self-use: transform into the previously scanned target
        if (target == args.User)
        {
            if (!scanner.Comp.HasScannedTarget)
            {
                _popup.PopupEntity(Loc.GetString("chameleon-scanner-no-data"), args.User, args.User);
                return;
            }

            args.Handled = true;

            _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
                args.User, scanner.Comp.TransformTime,
                new ChameleonTransformDoAfterEvent(),
                scanner.Owner,
                target: args.User,
                used: scanner.Owner)
            {
                NeedHand     = true,
                BreakOnMove  = true,
                BreakOnDamage = true,
                Hidden       = true,
            });

            return;
        }

        // Scan a humanoid target — no range restriction (CanReach intentionally ignored)
        if (!HasComp<HumanoidProfileComponent>(target))
            return;

        args.Handled = true;
        StartScanDoAfter(scanner, args.User, target);
    }

    // On scan complete: store scanned entity, do NOT apply disguise yet
    private void OnScanDoAfter(Entity<ChameleonScannerComponent> scanner, ref ChameleonScanDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { Valid: true } target)
            return;

        args.Handled = true;

        scanner.Comp.ScannedTarget = target;

        // Also persist on the suit so data survives scanner toggles.
        if (TryComp<NinjaChameleonComponent>(scanner.Comp.SuitEntity, out var chameleonComp))
        {
            chameleonComp.StoredScanTarget = target;
            Dirty(scanner.Comp.SuitEntity, chameleonComp);
        }

        var targetName = MetaData(target).EntityName;
        _popup.PopupEntity(
            Loc.GetString("chameleon-scanner-scan-complete", ("target", targetName)),
            args.User, args.User);
    }

    // On transform complete: apply the stored appearance to the ninja
    private void OnTransformDoAfter(Entity<ChameleonScannerComponent> scanner, ref ChameleonTransformDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        var scannedTarget = scanner.Comp.ScannedTarget;
        if (scannedTarget == null || !Exists(scannedTarget.Value))
        {
            _popup.PopupEntity(Loc.GetString("chameleon-scanner-target-gone"), args.User, args.User);
            return;
        }

        var suitUid = scanner.Comp.SuitEntity;
        if (!TryComp<NinjaChameleonComponent>(suitUid, out var chameleon))
            return;

        if (chameleon.WearerEntity is not { } ninja)
            return;

        args.Handled = true;
        ApplyDisguise(suitUid, chameleon, ninja, scannedTarget.Value);
        // Scanner stays in hand — ninja can re-disguise after exposure without rescanning.
    }

    // ── Disguise application ──────────────────────────────────────────────────

    private void ApplyDisguise(EntityUid suitUid, NinjaChameleonComponent chameleon, EntityUid ninja, EntityUid target)
    {
        CleanupDisguise(suitUid, chameleon, ninja);

        // Capture original state once (before first disguise)
        chameleon.OriginalName ??= MetaData(ninja).EntityName;
        chameleon.OriginalDetailExaminable ??= TakeDetailSnapshot(ninja);

        var targetName = MetaData(target).EntityName;

        // Snapshot the ninja's profile (species/age/sex/height) as a revert fallback.
        if (chameleon.OriginalProfile == null && TryComp<HumanoidProfileComponent>(ninja, out var ninjaProfileSnap))
            chameleon.OriginalProfile = BuildSyntheticProfile(ninjaProfileSnap);

        // Snapshot the ninja's real body visuals (organ sprites + skin colour + markings) once,
        // so the disguise can be fully reverted. These organ fields are AutoNetworkedField, so
        // copying them SERVER-SIDE replicates to clients and survives the target leaving PVS —
        // unlike a client CopySprite which goes invisible once the target is out of range.
        chameleon.OriginalOrganAppearances ??= _visualBody.SaveOrganAppearances(ninja);
        chameleon.OriginalOrganProfiles   ??= _visualBody.SaveOrganProfiles(ninja);
        chameleon.OriginalOrganMarkings   ??= _visualBody.SaveOrganMarkings(ninja);

        // Copy the target's body appearance onto the ninja (RSI sprites, skin colour, markings)
        // and the humanoid profile (species/age/sex/height → examine description + emote-sound sex).
        _visualBody.CopyAppearanceFrom(target, ninja);
        _visualBody.CopyOrganProfilesFrom(target, ninja);
        if (TryComp<HumanoidProfileComponent>(target, out var targetProfile))
            _humanoidProfile.ApplyProfileTo(ninja, BuildSyntheticProfile(targetProfile));

        ApplyDetailExaminable(target, ninja);

        // The ninja's displayed name is only overridden when a holographic PDA is equipped.
        // That happens only when BOTH: the ninja's ID slot is empty, AND the target actually has
        // an ID card (in a PDA or bare). If the ninja keeps their own PDA/ID, or the target has no
        // ID at all, we don't touch the ninja's ID slot or name.
        var idSlotEmpty = !_inventory.TryGetSlotEntity(ninja, "id", out var existingId) || existingId == null;
        var makeFakeId = idSlotEmpty && TargetHasIdCard(target);

        // Voice mask on suit — overrides speech verb and allows the accent relay to run.
        // The name is only masked when we provide the fake ID (empty slot).
        var voiceMask = EnsureComp<VoiceMaskComponent>(suitUid);
        voiceMask.OverrideIdentity = makeFakeId;
        voiceMask.VoiceMaskName = makeFakeId ? targetName : null;
        voiceMask.Active = true;
        voiceMask.AccentHide = false; // must be off so our accent relay can transform speech
        // Copy target's speech verb so the bubble/chat verb matches (шипит, пищит, etc.)
        if (TryComp<SpeechComponent>(target, out var tgtSpeech))
            voiceMask.VoiceMaskSpeechVerb = tgtSpeech.SpeechVerb;
        else
            voiceMask.VoiceMaskSpeechVerb = null;
        Dirty(suitUid, voiceMask);

        if (makeFakeId)
            _metaData.SetEntityName(ninja, targetName);

        // ── Speech sounds (the sounds played on each message, e.g. chittering) ─
        if (TryComp<SpeechComponent>(target, out var targetSpeech) &&
            TryComp<SpeechComponent>(ninja, out var ninjaSpeech))
        {
            chameleon.OriginalSpeechSounds ??= ninjaSpeech.SpeechSounds;
            ninjaSpeech.SpeechSounds = targetSpeech.SpeechSounds;
            Dirty(ninja, ninjaSpeech);
        }

        // ── TTS ───────────────────────────────────────────────────────────────
        if (TryComp<TTSComponent>(target, out var targetTts) &&
            TryComp<TTSComponent>(ninja, out var ninjaTts))
        {
            chameleon.OriginalTTSVoice ??= ninjaTts.VoicePrototypeId;
            ninjaTts.VoicePrototypeId = targetTts.VoicePrototypeId;
            Dirty(ninja, ninjaTts);
        }

        // ── Emote sounds (racial) ─────────────────────────────────────────────
        if (TryComp<VocalComponent>(ninja, out var ninjaVocal))
        {
            chameleon.OriginalVocalSounds ??= ninjaVocal.Sounds;
            chameleon.OriginalScreamId ??= ninjaVocal.ScreamId;
        }
        _vocal.CopyComponent(target, ninja);

        // ── Typing indicator (the typing bubble shown over the head) ───────────
        if (TryComp<TypingIndicatorComponent>(target, out var targetTyping))
        {
            if (!chameleon.TypingIndicatorOverridden)
            {
                chameleon.OriginalTypingIndicator = TryComp<TypingIndicatorComponent>(ninja, out var ninjaTypingOrig)
                    ? ninjaTypingOrig.TypingIndicatorPrototype
                    : (ProtoId<TypingIndicatorPrototype>?)null;
                chameleon.TypingIndicatorOverridden = true;
            }

            _typingIndicator.SetTypingIndicatorPrototype(ninja, targetTyping.TypingIndicatorPrototype);
        }

        // ── Accent relay ──────────────────────────────────────────────────────
        var relay = EnsureComp<ChameleonAccentRelayComponent>(ninja);
        relay.AccentTarget = target;

        _identity.QueueIdentityUpdate(ninja);

        // Give the holographic PDA only when the ninja's ID slot is already empty.
        // If the ninja keeps their own PDA/ID, leave the slot completely untouched.
        if (makeFakeId)
            SpawnAndEquipFakeId(suitUid, chameleon, ninja, targetName, target);

        // Mimic the target's worn clothing (PVS-safe: chameleon visuals replicate via components).
        ApplyClothingDisguise(suitUid, chameleon, ninja, target);

        // Record the disguise target on the ninja so the strip menu shows the target's inventory.
        var disguiseVisual = EnsureComp<NinjaChameleonVisualComponent>(ninja);
        disguiseVisual.TargetUid = target;
        Dirty(ninja, disguiseVisual);

        chameleon.IsDisguised = true;
        Dirty(suitUid, chameleon);

        _popup.PopupEntity(Loc.GetString("chameleon-disguise-applied", ("target", targetName)), ninja, ninja);
    }

    private void RemoveDisguise(EntityUid suitUid, NinjaChameleonComponent chameleon, EntityUid ninja)
    {
        if (!chameleon.IsDisguised)
            return;

        CleanupDisguise(suitUid, chameleon, ninja);
        RemoveClothingDisguise(chameleon, ninja);

        // Stop showing the target's inventory in the strip menu.
        RemCompDeferred<NinjaChameleonVisualComponent>(ninja);

        // Restore the ninja's real body visuals from the snapshot (organ sprites, skin colour,
        // markings). Done server-side so it replicates and survives PVS just like the disguise.
        if (chameleon.OriginalOrganMarkings is { } savedMarkings)
            _visualBody.RestoreOrganMarkings(ninja, savedMarkings);
        if (chameleon.OriginalOrganProfiles is { } savedProfiles)
            _visualBody.RestoreOrganProfiles(savedProfiles);
        if (chameleon.OriginalOrganAppearances is { } savedAppearances)
            _visualBody.RestoreOrganAppearances(savedAppearances);
        chameleon.OriginalOrganMarkings = null;
        chameleon.OriginalOrganProfiles = null;
        chameleon.OriginalOrganAppearances = null;

        // Restore profile fields (species/age/sex/height → examine description + emote-sound sex).
        if (chameleon.OriginalProfile is { } origProfile)
            _humanoidProfile.ApplyProfileTo(ninja, origProfile);
        chameleon.OriginalProfile = null;

        RestoreNameFallback(ninja, chameleon);

        RestoreDetailExaminable(ninja, chameleon);

        // ── Restore speech sounds ─────────────────────────────────────────────
        if (TryComp<SpeechComponent>(ninja, out var ninjaSpeech))
        {
            ninjaSpeech.SpeechSounds = chameleon.OriginalSpeechSounds;
            Dirty(ninja, ninjaSpeech);
        }
        chameleon.OriginalSpeechSounds = null;

        // ── Restore TTS ───────────────────────────────────────────────────────
        if (chameleon.OriginalTTSVoice != null && TryComp<TTSComponent>(ninja, out var ninjaTts))
        {
            ninjaTts.VoicePrototypeId = chameleon.OriginalTTSVoice;
            Dirty(ninja, ninjaTts);
        }
        chameleon.OriginalTTSVoice = null;

        // ── Restore emote sounds ──────────────────────────────────────────────
        if (TryComp<VocalComponent>(ninja, out var ninjaVocal))
        {
            ninjaVocal.Sounds = chameleon.OriginalVocalSounds;
            if (chameleon.OriginalScreamId != null)
                ninjaVocal.ScreamId = chameleon.OriginalScreamId;
            // Re-resolve the actually-played EmoteSounds from the restored Sounds + sex.
            _vocal.ReloadEmoteSounds(ninja, ninjaVocal);
        }
        chameleon.OriginalVocalSounds = null;
        chameleon.OriginalScreamId = null;

        // ── Restore typing indicator ──────────────────────────────────────────
        if (chameleon.TypingIndicatorOverridden && chameleon.OriginalTypingIndicator is { } origTyping)
            _typingIndicator.SetTypingIndicatorPrototype(ninja, origTyping);
        chameleon.OriginalTypingIndicator = null;
        chameleon.TypingIndicatorOverridden = false;

        // ── Remove accent relay ───────────────────────────────────────────────
        RemCompDeferred<ChameleonAccentRelayComponent>(ninja);

        RemCompDeferred<VoiceMaskComponent>(suitUid);

        _identity.QueueIdentityUpdate(ninja);

        // Refresh the over-head job icon now the fake ID is gone / real ID is back.
        _jobStatus.UpdateStatus(ninja);

        chameleon.IsDisguised = false;
        Dirty(suitUid, chameleon);
    }

    private void RestoreNameFallback(EntityUid ninja, NinjaChameleonComponent chameleon)
    {
        if (chameleon.OriginalName is { } name)
            _metaData.SetEntityName(ninja, name);
    }

    /// <summary>Deletes fake PDA, restores original id card from stash, and restores modified clothing visuals.</summary>
    private void CleanupDisguise(EntityUid suitUid, NinjaChameleonComponent chameleon, EntityUid ninja)
    {
        // Remove the holographic ID without dropping it, then delete it.
        // Strip UnremoveableComponent first so the container forcible-remove succeeds.
        if (chameleon.FakeIdCard is { } fakeId && Exists(fakeId))
        {
            RemComp<UnremoveableComponent>(fakeId);
            if (_containerSystem.TryGetContainingContainer(fakeId, out var fakeContainer))
                _containerSystem.Remove(fakeId, fakeContainer, reparent: false, force: true);
            QueueDel(fakeId);
        }
        chameleon.FakeIdCard = null;

        // Restore the original ID card from the hidden stash
        if (chameleon.OriginalIdCard is { } origId && Exists(origId))
        {
            if (_containerSystem.TryGetContainingContainer(origId, out var stash))
                _containerSystem.Remove(origId, stash, reparent: false, force: true);
            _inventory.TryEquip(ninja, ninja, origId, "id", silent: true, force: true);
        }
        chameleon.OriginalIdCard = null;
    }

    /// <summary>Moves the ninja's current ID card into a hidden container on the suit without dropping it.</summary>
    private void StashIdCard(EntityUid suitUid, NinjaChameleonComponent chameleon, EntityUid ninja)
    {
        if (!_inventory.TryGetSlotEntity(ninja, "id", out var origId) || origId == null)
            return;

        var stash = _containerSystem.EnsureContainer<ContainerSlot>(suitUid, "chameleon_id_stash");
        if (_containerSystem.TryGetContainingContainer(origId.Value, out var srcContainer))
        {
            _containerSystem.Remove(origId.Value, srcContainer, reparent: false, force: true);
            _containerSystem.Insert(origId.Value, stash, force: true);
            chameleon.OriginalIdCard = origId;
        }
    }

    // ── Examine description helpers ───────────────────────────────────────────

    private DetailExaminableSnapshot? TakeDetailSnapshot(EntityUid uid)
    {
        if (!TryComp<DetailExaminableComponent>(uid, out var d))
            return null;

        return new DetailExaminableSnapshot
        {
            Content      = d.Content,
            CharContent  = d.CharacterContent,
            OOCContent   = d.OOCContent,
            TagsContent  = d.TagsContent,
            LinksContent = d.LinksContent,
            GreenContent = d.GreenContent,
            YellowContent = d.YellowContent,
            RedContent   = d.RedContent,
            NSFWContent  = d.NSFWContent,
        };
    }

    private void ApplyDetailExaminable(EntityUid source, EntityUid dest)
    {
        if (!TryComp<DetailExaminableComponent>(source, out var src))
            return;

        var dst = EnsureComp<DetailExaminableComponent>(dest);
        dst.Content          = src.Content;
        dst.CharacterContent = src.CharacterContent;
        dst.OOCContent       = src.OOCContent;
        dst.TagsContent      = src.TagsContent;
        dst.LinksContent     = src.LinksContent;
        dst.GreenContent     = src.GreenContent;
        dst.YellowContent    = src.YellowContent;
        dst.RedContent       = src.RedContent;
        dst.NSFWContent      = src.NSFWContent;
        Dirty(dest, dst);
    }

    private void RestoreDetailExaminable(EntityUid ninja, NinjaChameleonComponent chameleon)
    {
        if (chameleon.OriginalDetailExaminable == null)
        {
            // Ninja had no description before disguise — remove any component we added
            RemCompDeferred<DetailExaminableComponent>(ninja);
            return;
        }

        var snap = chameleon.OriginalDetailExaminable;

        if (TryComp<DetailExaminableComponent>(ninja, out var d))
        {
            d.Content          = snap.Content;
            d.CharacterContent = snap.CharContent;
            d.OOCContent       = snap.OOCContent;
            d.TagsContent      = snap.TagsContent;
            d.LinksContent     = snap.LinksContent;
            d.GreenContent     = snap.GreenContent;
            d.YellowContent    = snap.YellowContent;
            d.RedContent       = snap.RedContent;
            d.NSFWContent      = snap.NSFWContent;
            Dirty(ninja, d);
        }
    }

    // ── Fake ID spawning ──────────────────────────────────────────────────────

    private void SpawnAndEquipFakeId(EntityUid suitUid, NinjaChameleonComponent chameleon, EntityUid ninja, string targetName, EntityUid target)
    {
        var coords  = Transform(ninja).Coordinates;
        var fakePda = Spawn(chameleon.FakeIdPrototype, coords);

        var marker = EnsureComp<HolographicNinjaIdCardComponent>(fakePda);
        marker.SuitEntity = suitUid;

        // Find embedded ID in the fake PDA before equipping.
        EntityUid? embeddedId = null;
        if (TryComp<PdaComponent>(fakePda, out var pda))
            embeddedId = pda.ContainedId;
        if (embeddedId == null
            && _containerSystem.TryGetContainer(fakePda, PdaComponent.PdaIdSlotId, out var idContainer)
            && idContainer.ContainedEntities.Count > 0)
            embeddedId = idContainer.ContainedEntities[0];

        // Copy the target's PDA sprite; also copy the embedded ID card sprite and description.
        if (_inventory.TryGetSlotEntity(target, "id", out var targetId) && targetId is { } tId)
        {
            var targetPdaProtoId = MetaData(tId).EntityPrototype?.ID;
            if (targetPdaProtoId != null)
                _chameleonClothing.ForceApplyPrototype(fakePda, targetPdaProtoId, SlotFlags.IDCARD);

            // Find embedded ID card in the target's PDA for sprite and description.
            EntityUid? targetEmbeddedId = null;
            if (TryComp<PdaComponent>(tId, out var targetPdaComp))
                targetEmbeddedId = targetPdaComp.ContainedId;
            if (targetEmbeddedId == null
                && _containerSystem.TryGetContainer(tId, PdaComponent.PdaIdSlotId, out var targetIdCont)
                && targetIdCont.ContainedEntities.Count > 0)
                targetEmbeddedId = targetIdCont.ContainedEntities[0];

            if (targetEmbeddedId is { } targetEid && embeddedId is { } ourEid)
            {
                var targetIdProtoId = MetaData(targetEid).EntityPrototype?.ID;
                if (targetIdProtoId != null)
                    _chameleonClothing.ForceApplyPrototype(ourEid, targetIdProtoId, SlotFlags.IDCARD);

                _metaData.SetEntityDescription(ourEid, MetaData(targetEid).EntityDescription);
            }
        }

        if (embeddedId is { } eid && TryComp<IdCardComponent>(eid, out var embeddedCard))
        {
            embeddedCard.FullName = targetName;
            CopyJobInfoToCard(target, embeddedCard);
            Dirty(eid, embeddedCard);
            EnsureComp<UnremoveableComponent>(eid);
        }

        // Show the target's name as the PDA owner.
        if (TryComp<PdaComponent>(fakePda, out var fakePdaComp))
        {
            fakePdaComp.OwnerName = targetName;
            Dirty(fakePda, fakePdaComp);
        }

        EnsureComp<UnremoveableComponent>(fakePda);

        if (_inventory.TryEquip(ninja, ninja, fakePda, "id", silent: true, force: true))
        {
            chameleon.FakeIdCard = fakePda;
            // Refresh the over-head job icon now that the fake ID's job is set and it's equipped.
            _jobStatus.UpdateStatus(ninja);
        }
        else
        {
            QueueDel(fakePda);
        }
    }

    // ── Clothing disguise ─────────────────────────────────────────────────────

    /// <summary>
    /// Maps a clothing slot flag to the holographic chameleon prototype used to fill an empty
    /// ninja slot so the target's item in that slot can be mimicked.
    /// </summary>
    private static readonly (SlotFlags Flag, string Proto)[] ClothingSlotProtos =
    {
        (SlotFlags.INNERCLOTHING, "ClothingUniformJumpsuitChameleon"),
        (SlotFlags.OUTERCLOTHING, "ClothingOuterChameleon"),
        (SlotFlags.HEAD,          "ClothingHeadHatChameleon"),
        (SlotFlags.MASK,          "ClothingMaskGasChameleon"),
        (SlotFlags.EYES,          "ClothingEyesChameleon"),
        (SlotFlags.EARS,          "ClothingHeadsetChameleon"),
        (SlotFlags.GLOVES,        "ClothingHandsChameleon"),
        (SlotFlags.FEET,          "ClothingShoesChameleon"),
        (SlotFlags.NECK,          "ClothingNeckChameleon"),
        (SlotFlags.BACK,          "ClothingBackpackChameleon"),
    };

    private static string? GetChameleonProtoForSlot(SlotFlags slotFlags)
    {
        foreach (var (flag, proto) in ClothingSlotProtos)
        {
            if ((slotFlags & flag) != 0)
                return proto;
        }
        return null;
    }

    /// <summary>
    /// Mimics every clothing item the target is wearing. For each of the target's worn slots:
    /// if the ninja already has an item there, override its visuals; otherwise spawn a holographic
    /// chameleon stand-in, equip it, and override its visuals. All changes are reverted later.
    /// </summary>
    // Slots that hold the ninja's tools/weapons (katana in belt, suit storage) rather than
    // Slots whose item we actually repaint to mimic the target. The katana belt / suit storage
    // are excluded so the katana etc. keep their own appearance.
    private const SlotFlags DisguiseSlotMask =
        SlotFlags.OUTERCLOTHING | SlotFlags.INNERCLOTHING | SlotFlags.HEAD | SlotFlags.MASK |
        SlotFlags.EYES | SlotFlags.EARS | SlotFlags.GLOVES | SlotFlags.FEET | SlotFlags.NECK | SlotFlags.BACK;

    // Slots whose on-body visual we hide when the target's matching slot is empty. Wider than
    // the repaint mask: also covers underwear, socks and suit storage (e.g. a gas tank/balloon)
    // so the ninja fully matches a target that isn't wearing those.
    private const SlotFlags HideSlotMask =
        DisguiseSlotMask | SlotFlags.UNDERWEARTOP | SlotFlags.UNDERWEARBOTTOM | SlotFlags.SOCKS | SlotFlags.SUITSTORAGE;

    private void ApplyClothingDisguise(EntityUid suitUid, NinjaChameleonComponent chameleon, EntityUid ninja, EntityUid target)
    {
        var ninjaEnum = _inventory.GetSlotEnumerator(ninja, HideSlotMask);
        while (ninjaEnum.MoveNext(out _, out var slotDef))
        {
            // Skip anything outside the hide mask entirely.
            if ((slotDef.SlotFlags & HideSlotMask) == 0)
                continue;

            var targetHasItem = _inventory.TryGetSlotEntity(target, slotDef.Name, out var targetItem) && targetItem != null;
            var ninjaHasItem = _inventory.TryGetSlotEntity(ninja, slotDef.Name, out var ninjaItem) && ninjaItem != null;

            // Target's slot is empty: hide the ninja's own item visual here (without unequipping it,
            // so abilities stay active) so the disguise matches a target who wears nothing in this slot.
            if (!targetHasItem)
            {
                if (ninjaHasItem)
                {
                    EnsureComp<NinjaHiddenClothingComponent>(ninjaItem!.Value);
                    chameleon.HiddenOwnClothing.Add(ninjaItem.Value);
                }
                continue;
            }

            // Beyond this point we repaint to match the target — only for repaintable slots.
            if ((slotDef.SlotFlags & DisguiseSlotMask) == 0)
                continue;

            var targetProtoId = MetaData(targetItem!.Value).EntityPrototype?.ID;
            if (targetProtoId == null)
                continue;

            if (ninjaHasItem)
            {
                // Ninja already wears something here (suit, gloves, ...) — repaint it in place
                // so abilities tied to the worn item are preserved.
                _chameleonClothing.ForceApplyPrototype(ninjaItem!.Value, targetProtoId, slotDef.SlotFlags);
                chameleon.DisguisedOwnClothing.Add(ninjaItem.Value);
            }
            else
            {
                // Empty ninja slot — spawn a holographic stand-in and repaint it.
                var protoForSlot = GetChameleonProtoForSlot(slotDef.SlotFlags);
                if (protoForSlot == null)
                    continue;

                var holo = Spawn(protoForSlot, Transform(ninja).Coordinates);
                if (!_inventory.TryEquip(ninja, ninja, holo, slotDef.Name, silent: true, force: true))
                {
                    QueueDel(holo);
                    continue;
                }

                _chameleonClothing.ForceApplyPrototype(holo, targetProtoId, slotDef.SlotFlags);
                EnsureComp<UnremoveableComponent>(holo);
                var marker = EnsureComp<NinjaModifiedClothingComponent>(holo);
                marker.SuitEntity = suitUid;
                chameleon.SpawnedDisguiseClothing.Add(holo);
            }
        }

        // Force the client to re-render worn equipment so newly hidden items disappear.
        if (TryComp<InventoryComponent>(ninja, out var inv))
        {
            var ev = new InventoryTemplateUpdated();
            RaiseLocalEvent(ninja, ref ev);
        }
    }

    /// <summary>
    /// Reverts the clothing disguise: deletes spawned holographic items and restores the visuals
    /// of the ninja's own items that were repainted.
    /// </summary>
    private void RemoveClothingDisguise(NinjaChameleonComponent chameleon, EntityUid ninja)
    {
        foreach (var holo in chameleon.SpawnedDisguiseClothing)
        {
            if (!Exists(holo))
                continue;

            RemComp<UnremoveableComponent>(holo);
            if (_containerSystem.TryGetContainingContainer((holo, null), out var container))
                _containerSystem.Remove(holo, container, reparent: false, force: true);
            QueueDel(holo);
        }
        chameleon.SpawnedDisguiseClothing.Clear();

        foreach (var item in chameleon.DisguisedOwnClothing)
        {
            if (Exists(item))
                _chameleonClothing.ForceRemovePrototype(item);
        }
        chameleon.DisguisedOwnClothing.Clear();

        // Un-hide the ninja's own items that were visually suppressed for empty target slots.
        foreach (var item in chameleon.HiddenOwnClothing)
        {
            if (Exists(item))
                RemComp<NinjaHiddenClothingComponent>(item);
        }
        chameleon.HiddenOwnClothing.Clear();

        // Re-render worn equipment so restored items become visible again.
        if (TryComp<InventoryComponent>(ninja, out var inv))
        {
            var ev = new InventoryTemplateUpdated();
            RaiseLocalEvent(ninja, ref ev);
        }
    }

    // ── Damage reveal ─────────────────────────────────────────────────────────

    private void OnNinjaDamaged(Entity<SpaceNinjaComponent> ninja, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased)
            return;

        // Ignore minor environmental/reagent damage (puddles, etc.)
        if (args.DamageDelta != null && args.DamageDelta.GetTotal() < FixedPoint2.New(5))
            return;

        if (ninja.Comp.Suit is not { } suitUid)
            return;

        if (!TryComp<NinjaChameleonComponent>(suitUid, out var chameleon) || !chameleon.IsDisguised)
            return;

        RemoveDisguise(suitUid, chameleon, ninja);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static HumanoidCharacterProfile BuildSyntheticProfile(HumanoidProfileComponent target)
    {
        return new HumanoidCharacterProfile()
            .WithSpecies(target.Species)
            .WithSex(target.Sex)
            .WithGender(target.Gender)
            .WithAge(target.Age)
            .WithVoice(target.Voice)
            .WithBarkVoice(target.BarkVoice)
            .WithStatus(target.Status)
            .WithHeight(target.Height);
    }

    /// <summary>Returns true if the target has an actual ID card — either bare or inside a PDA.</summary>
    private bool TargetHasIdCard(EntityUid target)
    {
        if (!_inventory.TryGetSlotEntity(target, "id", out var idSlotItem) || idSlotItem == null)
            return false;

        var item = idSlotItem.Value;

        if (HasComp<IdCardComponent>(item))
            return true;

        if (TryComp<PdaComponent>(item, out var pda))
        {
            if (pda.ContainedId != null && HasComp<IdCardComponent>(pda.ContainedId))
                return true;

            if (_containerSystem.TryGetContainer(item, PdaComponent.PdaIdSlotId, out var idCont)
                && idCont.ContainedEntities.Count > 0
                && HasComp<IdCardComponent>(idCont.ContainedEntities[0]))
                return true;
        }

        return false;
    }

    private void CopyJobInfoToCard(EntityUid target, IdCardComponent dest)
    {
        if (!_inventory.TryGetSlotEntity(target, "id", out var idSlotItem) || idSlotItem == null)
            return;

        var pdaOrCard = idSlotItem.Value;

        // Resolve the target's actual ID card (embedded in a PDA, or a bare ID card).
        IdCardComponent? sourceCard = null;
        if (TryComp<PdaComponent>(pdaOrCard, out var pda))
        {
            EntityUid? embeddedId = pda.ContainedId;
            if (embeddedId == null
                && _containerSystem.TryGetContainer(pdaOrCard, PdaComponent.PdaIdSlotId, out var idCont)
                && idCont.ContainedEntities.Count > 0)
                embeddedId = idCont.ContainedEntities[0];

            if (embeddedId is { } sid)
                TryComp(sid, out sourceCard);
        }
        else
        {
            TryComp(pdaOrCard, out sourceCard);
        }

        if (sourceCard == null)
            return;

        // Copy both the localization id and the resolved/custom job title text, plus the icon
        // and job prototype, so the examine job suffix and over-head job icon match the target.
        dest.JobTitle = sourceCard.JobTitle;
        dest.LocalizedJobTitle = sourceCard.LocalizedJobTitle;
        dest.JobIcon = sourceCard.JobIcon;
        dest.JobPrototype = sourceCard.JobPrototype;
    }

    // ── Scanner helpers ───────────────────────────────────────────────────────

    private void StartScanDoAfter(Entity<ChameleonScannerComponent> scanner, EntityUid user, EntityUid target)
    {
        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            user, scanner.Comp.ScanTime,
            new ChameleonScanDoAfterEvent(),
            scanner.Owner,
            target: target,
            used: scanner.Owner)
        {
            NeedHand           = true,
            BreakOnMove        = true,
            BreakOnDamage      = true,
            Hidden             = true,
            DistanceThreshold  = null, // allow scanning at any distance
        });
    }

    // ── Scanner / Fake ID cleanup ─────────────────────────────────────────────

    private void OnScannerShutdown(Entity<ChameleonScannerComponent> scanner, ref ComponentShutdown args)
    {
        if (!TryComp<NinjaChameleonComponent>(scanner.Comp.SuitEntity, out var chameleon))
            return;

        if (chameleon.ScannerEntity == scanner.Owner)
            chameleon.ScannerEntity = null;
    }

    private void OnFakeIdShutdown(Entity<HolographicNinjaIdCardComponent> id, ref ComponentShutdown args)
    {
        if (!TryComp<NinjaChameleonComponent>(id.Comp.SuitEntity, out var chameleon))
            return;

        if (chameleon.FakeIdCard == id.Owner)
            chameleon.FakeIdCard = null;
    }

    // ── Accent relay ──────────────────────────────────────────────────────────

    /// <summary>
    /// Runs after all of the ninja's own accent systems so it can replace the message
    /// with what the target would actually say (fully mimicking their accent).
    /// </summary>
    private void OnAccentRelay(Entity<ChameleonAccentRelayComponent> ninja, ref AccentGetEvent args)
    {
        var target = ninja.Comp.AccentTarget;
        if (!Exists(target) || target == ninja.Owner)
            return;

        var targetAccent = new AccentGetEvent(target, args.Message);
        RaiseLocalEvent(target, targetAccent, true);
        args.Message = targetAccent.Message;
    }
}
