using System.Linq;
using Content.Server._Wega.AutoDust;
using Content.Server.Ninja.Components;
using Content.Server.Preferences.Managers;
using Content.Shared._Wega.AutoDust;
using Content.Shared.Administration;
using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Ninja.Components;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Station;
using Robust.Server.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Ninja.Systems;

/// <summary>
/// Handles the "Second Chance / Cloning" ability for space ninjas.
/// On death: spawn a clone, immediately transfer the player's mind to it, lock it inside the
/// bound NinjaCloningCapsule structure for 30 seconds, then eject it.
/// </summary>
public sealed partial class NinjaCloningSystem : EntitySystem
{
    private static readonly (string Slot, string Name)[] NinjaSlots =
    [
        ("outerClothing", "Ninja Suit"),
        ("mask",          "Ninja Mask"),
        ("head",          "Ninja Helmet"),
        ("gloves",        "Ninja Gloves"),
        ("shoes",         "Ninja Boots"),
    ];

    private const string SyndicateFaction = "Syndicate";

    [Dependency] private ContainerSystem _container = default!;
    [Dependency] private HumanoidProfileSystem _humanoidProfile = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IServerPreferencesManager _prefs = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private NinjaSuitSystem _ninjaSuit = default!;
    [Dependency] private NpcFactionSystem _factions = default!;
    [Dependency] private SharedStationSpawningSystem _spawning = default!;
    [Dependency] private SharedVisualBodySystem _visualBody = default!;
    [Dependency] private SpiderOSSystem _spiderOS = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NinjaCloningCapsuleComponent, ComponentInit>(OnCapsuleInit);

        // Run before AutoDustSystem so mind transfer happens first; AutoDustSystem
        // then sees no mind on the original body and skips dusting it.
        SubscribeLocalEvent<NinjaCloningComponent, MobStateChangedEvent>(OnMobStateChanged,
            before: new[] { typeof(AutoDustSystem) });
    }

    private void OnCapsuleInit(Entity<NinjaCloningCapsuleComponent> ent, ref ComponentInit args)
    {
        ent.Comp.BodyContainer = _container.EnsureContainer<ContainerSlot>(ent.Owner, "ninja-cloning-body");
    }

    /// <summary>
    /// Binds the nearest unclaimed capsule on the same map to the ninja.
    /// Called from NinjaSuitSystem when the Cloning ability is chosen.
    /// </summary>
    public void BindCapsule(EntityUid user, NinjaCloningComponent comp)
    {
        var userMap = Transform(user).MapID;
        var query = EntityQueryEnumerator<NinjaCloningCapsuleComponent>();
        while (query.MoveNext(out var capsuleUid, out var capsuleComp))
        {
            if (Transform(capsuleUid).MapID != userMap)
                continue;
            if (capsuleComp.BoundNinja != null)
                continue;

            comp.CapsuleEntity = capsuleUid;
            capsuleComp.BoundNinja = user;
            _appearance.SetData(capsuleUid, NinjaCloningCapsuleVisuals.Active, true);
            break;
        }
    }

    private void OnMobStateChanged(Entity<NinjaCloningComponent> ent, ref MobStateChangedEvent args)
    {
        if (ent.Comp.Used)
            return;

        var shouldClone = args.NewMobState == MobState.Dead;

        // When OnCrit auto-dust is active the body will be dusted at Critical,
        // so trigger cloning immediately at that point instead of waiting for Dead.
        if (!shouldClone
            && args.NewMobState == MobState.Critical
            && TryComp<AutoDustComponent>(ent.Owner, out var dust)
            && dust.Trigger == AutoDustTrigger.OnCrit)
            shouldClone = true;

        if (!shouldClone)
            return;

        StartCloning(ent);
    }

    private void StartCloning(Entity<NinjaCloningComponent> ent)
    {
        var (uid, comp) = ent;
        comp.Used = true;

        // Save auto-dust trigger so the clone inherits the player's setting
        var savedDustTrigger = TryComp<AutoDustComponent>(uid, out var origDust)
            ? origDust.Trigger
            : AutoDustTrigger.OnDeath;

        if (!_mind.TryGetMind(uid, out var mindId, out var mindComp))
        {
            ResetCapsuleAppearance(comp);
            return;
        }

        if (comp.CapsuleEntity == null
            || !TryComp<NinjaCloningCapsuleComponent>(comp.CapsuleEntity.Value, out var capsuleComp))
            return;

        var capsuleUid = comp.CapsuleEntity.Value;
        var spawnCoords = Transform(capsuleUid).Coordinates;

        var profile = mindComp.UserId is { } userId
            ? _prefs.GetPreferences(userId).SelectedCharacter as HumanoidCharacterProfile
            : null;
        profile ??= HumanoidCharacterProfile.RandomWithSpecies();

        if (!_proto.TryIndex<SpeciesPrototype>(profile.Species, out var species))
            species = _proto.Index<SpeciesPrototype>(HumanoidCharacterProfile.DefaultSpecies);

        var newEntity = Spawn(species.Prototype, spawnCoords);
        var humanoidProfile = profile.WithSpecies(species.ID);
        _visualBody.ApplyProfileTo(newEntity, humanoidProfile);
        _humanoidProfile.ApplyProfileTo(newEntity, humanoidProfile);
        _metaData.SetEntityName(newEntity, profile.Name);

        EnsureComp<SpaceNinjaComponent>(newEntity);

        // Replace any default factions from the spawned prototype with only Syndicate.
        // Use NpcFactionSystem methods — direct mutation of Factions is write-protected.
        if (TryComp<NpcFactionMemberComponent>(newEntity, out var factionComp))
        {
            foreach (var faction in factionComp.Factions.ToList())
                _factions.RemoveFaction(newEntity, faction);
        }
        _factions.AddFaction(newEntity, SyndicateFaction);

        _spawning.EquipStartingGear(newEntity, new ProtoId<StartingGearPrototype>("SpaceNinjaGearRespawn"));

        if (_inventory.TryGetSlotEntity(newEntity, "outerClothing", out var suitUid)
            && TryComp<SpiderOSComponent>(suitUid, out var spiderOS))
        {
            spiderOS.SuitColor = comp.SavedSuitColor;
            spiderOS.SuitGender = comp.SavedSuitGender;
            spiderOS.SuitStyleVariant = comp.SavedSuitStyleVariant;
            spiderOS.AbilityChoices = (int[]) comp.SavedChoices.Clone();
            spiderOS.IsActivated = true;
            Dirty(suitUid.Value, spiderOS);

            _spiderOS.ApplyStyles(newEntity, spiderOS);
            _ninjaSuit.GrantChosenAbilities(suitUid.Value, newEntity, comp.SavedChoices);

            foreach (var (slot, _) in NinjaSlots)
            {
                if (_inventory.TryGetSlotEntity(newEntity, slot, out var item))
                    EnsureComp<UnremoveableComponent>(item.Value);
            }
        }

        // Grant auto-dust and restore the player's chosen trigger mode
        var cloneDust = EnsureComp<AutoDustComponent>(newEntity);
        cloneDust.Trigger = savedDustTrigger;
        Dirty(newEntity, cloneDust);

        // Mark clone as already used so it cannot clone again; inherit the capsule binding
        var cloneComp = EnsureComp<NinjaCloningComponent>(newEntity);
        cloneComp.Used = true;
        cloneComp.InCapsule = true;
        cloneComp.RespawnAt = _timing.CurTime + NinjaCloningComponent.CapsuleDuration;
        cloneComp.CapsuleEntity = capsuleUid;
        Dirty(newEntity, cloneComp);

        // Transfer mind BEFORE inserting into container — TransferTo fails if target is in a container
        _mind.TransferTo(mindId, newEntity, ghostCheckOverride: true, createGhost: false);

        // Lock clone inside the capsule; ejected after the timer
        _container.Insert(newEntity, capsuleComp.BodyContainer);

        // Prevent the player from doing anything while regenerating inside the capsule
        EnsureComp<AdminFrozenComponent>(newEntity);

        // Original dead body stays at the death location — clone is not a teleport
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<NinjaCloningComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.InCapsule || _timing.CurTime < comp.RespawnAt)
                continue;

            EjectFromCapsule((uid, comp));
        }
    }

    private void EjectFromCapsule(Entity<NinjaCloningComponent> ent)
    {
        var (uid, comp) = ent;

        comp.InCapsule = false;
        Dirty(uid, comp);

        RemComp<AdminFrozenComponent>(uid);
        _container.TryRemoveFromContainer(uid);

        ResetCapsuleAppearance(comp);
    }

    private void ResetCapsuleAppearance(NinjaCloningComponent comp)
    {
        if (comp.CapsuleEntity == null
            || !TryComp<NinjaCloningCapsuleComponent>(comp.CapsuleEntity.Value, out var capsuleComp))
            return;

        capsuleComp.BoundNinja = null;
        _appearance.SetData(comp.CapsuleEntity.Value, NinjaCloningCapsuleVisuals.Active, false);
    }

}
