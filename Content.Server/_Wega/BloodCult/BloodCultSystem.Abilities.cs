using System.Linq;
using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Content.Server.Emp;
using Content.Server.Flash;
using Content.Server.Hallucinations;
using Content.Shared.Bed.Sleep;
using Content.Shared.Blood.Cult;
using Content.Shared.Blood.Cult.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Clothing;
using Content.Shared.Cuffs;
using Content.Shared.Cuffs.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.DoAfter;
using Content.Shared.Doors.Components;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.Stacks;
using Content.Shared.Standing;
using Content.Shared.Speech.Muting;
using Content.Shared.Stunnable;
using Content.Shared.Timing;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Content.Shared.Flash.Components;
using Content.Shared.Body.Components;
using Content.Shared.NullRod.Components;
using Content.Shared.Chat;

namespace Content.Server.Blood.Cult;

public sealed partial class BloodCultSystem
{
    [Dependency] private readonly BloodstreamSystem _blood = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly EmpSystem _emp = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly FixtureSystem _fixtures = default!;
    [Dependency] private readonly FlashSystem _flash = default!;
    [Dependency] private readonly HallucinationsSystem _hallucinations = default!;
    [Dependency] private readonly IAdminLogManager _admin = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly QuickDialogSystem _quickDialog = default!;
    [Dependency] private readonly SharedCuffableSystem _cuff = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly VisibilitySystem _visibility = default!;
    [Dependency] private readonly LoadoutSystem _loadout = default!;

    private void InitializeBloodAbilities()
    {
        // Blood Magic
        SubscribeLocalEvent<BloodCultistComponent, BloodCultBloodMagicActionEvent>(OnBloodMagic);
        SubscribeNetworkEvent<BloodMagicMenuClosedEvent>(AfterSpellSelect);
        SubscribeLocalEvent<BloodCultistComponent, BloodMagicDoAfterEvent>(DoAfterSpellSelect);

        // Abilities
        SubscribeLocalEvent<BloodCultCommuneActionEvent>(OnCultCommune);
        SubscribeLocalEvent<BloodSpellComponent, AfterInteractEvent>(OnInteract);
        SubscribeLocalEvent<BloodCultistComponent, RecallBloodDaggerEvent>(OnRecallDagger);

        SubscribeLocalEvent<BloodCultistComponent, BloodCultStunActionEvent>(OnStun);
        SubscribeLocalEvent<BloodCultistComponent, BloodCultTeleportActionEvent>(OnTeleport);
        SubscribeLocalEvent<BloodCultistComponent, TeleportSpellDoAfterEvent>(OnTeleportDoAfter);
        SubscribeLocalEvent<BloodCultistComponent, BloodCultElectromagneticPulseActionEvent>(OnElectromagneticPulse);
        SubscribeLocalEvent<BloodCultistComponent, BloodCultShadowShacklesActionEvent>(OnShadowShackles);
        SubscribeLocalEvent<BloodCultistComponent, BloodCultTwistedConstructionActionEvent>(OnTwistedConstruction);
        SubscribeLocalEvent<BloodCultistComponent, BloodCultSummonEquipmentActionEvent>(OnSummonEquipment);
        SubscribeLocalEvent<BloodCultistComponent, BloodCultSummonDaggerActionEvent>(OnSummonDagger);
        SubscribeLocalEvent<BloodCultistComponent, BloodCultHallucinationsActionEvent>(OnHallucinations);
        SubscribeLocalEvent<BloodCultistComponent, BloodCultConcealPresenceActionEvent>(OnConcealPresence);
        SubscribeLocalEvent<BloodCultistComponent, BloodCultBloodRitesActionEvent>(OnBloodRites);

        SubscribeLocalEvent<BloodSpellComponent, UseInHandEvent>(BloodRites);
        SubscribeNetworkEvent<BloodRitesMenuClosedEvent>(BloodRitesSelect);
        SubscribeLocalEvent<BloodCultistComponent, BloodCultBloodOrbActionEvent>(OnBloodOrb);
        SubscribeLocalEvent<BloodOrbComponent, UseInHandEvent>(OnBloodOrbAbsorbed);
        SubscribeLocalEvent<BloodCultistComponent, BloodCultBloodRechargeActionEvent>(OnBloodRecharge);
        SubscribeLocalEvent<BloodCultistComponent, BloodCultBloodSpearActionEvent>(OnBloodSpear);
        SubscribeLocalEvent<BloodCultistComponent, RecallBloodSpearEvent>(OnRecallSpear);
        SubscribeLocalEvent<BloodCultistComponent, BloodCultBloodBoltBarrageActionEvent>(OnBloodBoltBarrage);
    }

    #region Blood Magic
    private void OnBloodMagic(EntityUid uid, BloodCultistComponent component, BloodCultBloodMagicActionEvent args)
    {
        var netEntity = _entityManager.GetNetEntity(uid);
        RaiseNetworkEvent(new BloodMagicPressedEvent(netEntity));
        args.Handled = true;
    }

    private void AfterSpellSelect(BloodMagicMenuClosedEvent args, EntitySessionEventArgs eventArgs)
    {
        var uid = _entityManager.GetEntity(args.Uid);
        if (!TryComp<BloodCultistComponent>(uid, out var cult))
            return;

        if (!cult.BloodMagicActive)
        {
            _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, uid, TimeSpan.FromSeconds(10f), new BloodMagicDoAfterEvent(args.SelectedSpell), uid)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
                MovementThreshold = 0.01f,
                NeedHand = true
            });
        }
        else
        {
            var remSpell = cult.SelectedSpell;
            if (remSpell != null)
                _action.RemoveAction(uid, remSpell);
            cult.SelectedSpell = null;
            cult.BloodMagicActive = false;

            _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, uid, TimeSpan.FromSeconds(10f), new BloodMagicDoAfterEvent(args.SelectedSpell), uid)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
                MovementThreshold = 0.01f,
                NeedHand = true
            });
        }
    }

    private void DoAfterSpellSelect(EntityUid cultist, BloodCultistComponent component, BloodMagicDoAfterEvent args)
    {
        if (args.Cancelled) return;

        var actionEntityUid = _action.AddAction(cultist, args.SelectedSpell);
        if (actionEntityUid.HasValue)
            component.SelectedSpell = actionEntityUid.Value;
        else
            component.SelectedSpell = null;

        ExtractBlood(cultist, -20, 10);
        component.BloodMagicActive = true;
    }
    #endregion

    #region Abilities
    private void OnCultCommune(BloodCultCommuneActionEvent args)
    {
        var uid = args.Performer;
        if (!TryComp<ActorComponent>(uid, out var playerActor))
            return;

        // Админ логика, зато как просто
        var playerSession = playerActor.PlayerSession;
        _quickDialog.OpenDialog(playerSession, Loc.GetString("cult-commune-title"), "",
            (string message) =>
            {
                var finalMessage = string.IsNullOrWhiteSpace(message)
                    ? ""
                    : message;

                var senderName = Name(uid) ?? "Unknown";
                var popupMessage = Loc.GetString("cult-commune-massage", ("name", senderName), ("massage", finalMessage));

                var cultistQuery = EntityQueryEnumerator<ActorComponent, BloodCultistComponent>();
                while (cultistQuery.MoveNext(out var cultistUid, out var actorComp, out var cultistComp))
                {
                    if (actorComp == playerActor) continue;

                    _prayerSystem.SendSubtleMessage(actorComp.PlayerSession, actorComp.PlayerSession, string.Empty, popupMessage);
                }

                var constructQuery = EntityQueryEnumerator<ActorComponent, BloodCultConstructComponent>();
                while (constructQuery.MoveNext(out var constructUid, out var actorComp, out var constructComp))
                {
                    if (actorComp == playerActor) continue;

                    _prayerSystem.SendSubtleMessage(actorComp.PlayerSession, actorComp.PlayerSession, string.Empty, popupMessage);
                }

                _admin.Add(LogType.Chat, LogImpact.Low, $"{ToPrettyString(uid):user} saying the: {finalMessage} in cult commune");
                _chat.TrySendInGameICMessage(uid, finalMessage, InGameICChatType.Whisper, ChatTransmitRange.Normal, checkRadioPrefix: false);
            });
        args.Handled = true;
    }

    private void OnRecallDagger(EntityUid cultist, BloodCultistComponent component, RecallBloodDaggerEvent args)
    {
        if (component.RecallDaggerActionEntity is not { } dagger || !HasComp<BloodDaggerComponent>(dagger))
        {
            _popup.PopupEntity(Loc.GetString("blood-cult-dagger-not-found"), cultist, cultist, PopupType.SmallCaution);
            args.Handled = true;
            return;
        }

        var cultistPosition = _transform.GetWorldPosition(cultist);
        _transform.SetWorldPosition(dagger, cultistPosition);
        _popup.PopupEntity(Loc.GetString("blood-cult-dagger-recalled"), cultist, cultist);
        _hands.TryPickupAnyHand(cultist, dagger);
        args.Handled = true;
    }

    private void OnStun(EntityUid cultist, BloodCultistComponent component, BloodCultStunActionEvent args)
    {
        var spellGear = new ProtoId<StartingGearPrototype>("BloodCultSpellStunGear");

        var dropEvent = new DropHandItemsEvent();
        RaiseLocalEvent(cultist, ref dropEvent);
        List<ProtoId<StartingGearPrototype>> gear = new() { spellGear };
        _loadout.Equip(cultist, gear, null);

        args.Handled = true;
        EmpoweringCheck(args.Action, component);
    }

    private void OnTeleport(EntityUid cultist, BloodCultistComponent component, BloodCultTeleportActionEvent args)
    {
        var spellGear = new ProtoId<StartingGearPrototype>("BloodCultSpellTeleportGear");

        var dropEvent = new DropHandItemsEvent();
        RaiseLocalEvent(cultist, ref dropEvent);
        List<ProtoId<StartingGearPrototype>> gear = new() { spellGear };
        _loadout.Equip(cultist, gear, null);

        args.Handled = true;
        EmpoweringCheck(args.Action, component);
    }

    private void OnElectromagneticPulse(EntityUid cultist, BloodCultistComponent component, BloodCultElectromagneticPulseActionEvent args)
    {
        var coords = _transform.GetMapCoordinates(cultist);
        var exclusions = new List<EntityUid>();
        var entitiesInRange = _entityLookup.GetEntitiesInRange(coords, 5f);
        foreach (var uid in entitiesInRange)
        {
            if (HasComp<BloodCultistComponent>(uid))
                exclusions.Add(uid);
        }
        _emp.EmpPulseExclusions(coords, 5f, 100000f, 60f, exclusions);

        args.Handled = true;
        EmpoweringCheck(args.Action, component);
    }

    private void OnShadowShackles(EntityUid cultist, BloodCultistComponent component, BloodCultShadowShacklesActionEvent args)
    {
        var spellGear = new ProtoId<StartingGearPrototype>("BloodCultSpellShadowShacklesGear");

        var dropEvent = new DropHandItemsEvent();
        RaiseLocalEvent(cultist, ref dropEvent);
        List<ProtoId<StartingGearPrototype>> gear = new() { spellGear };
        _loadout.Equip(cultist, gear, null);

        args.Handled = true;
        EmpoweringCheck(args.Action, component);
    }

    private void OnTwistedConstruction(EntityUid cultist, BloodCultistComponent component, BloodCultTwistedConstructionActionEvent args)
    {
        var spellGear = new ProtoId<StartingGearPrototype>("BloodCultSpellTwistedConstructionGear");

        var dropEvent = new DropHandItemsEvent();
        RaiseLocalEvent(cultist, ref dropEvent);
        List<ProtoId<StartingGearPrototype>> gear = new() { spellGear };
        _loadout.Equip(cultist, gear, null);

        args.Handled = true;
        EmpoweringCheck(args.Action, component);
    }

    private void OnSummonEquipment(EntityUid cultist, BloodCultistComponent component, BloodCultSummonEquipmentActionEvent args)
    {
        var spellGear = new ProtoId<StartingGearPrototype>("BloodCultSpellSummonEquipmentGear");

        var dropEvent = new DropHandItemsEvent();
        RaiseLocalEvent(cultist, ref dropEvent);
        List<ProtoId<StartingGearPrototype>> gear = new() { spellGear };
        _loadout.Equip(cultist, gear, null);

        args.Handled = true;
        EmpoweringCheck(args.Action, component);
    }

    private void OnSummonDagger(EntityUid cultist, BloodCultistComponent component, BloodCultSummonDaggerActionEvent args)
    {
        if (_entityManager.EntityExists(component.RecallDaggerActionEntity))
        {
            _popup.PopupEntity(Loc.GetString("blood-cult-blood-dagger-exists"), cultist, cultist, PopupType.SmallCaution);
            args.Handled = true;
            return;
        }

        var cultistCoords = Transform(cultist).Coordinates;
        string selectedDagger = GetCurrentGod() switch
        {
            "Narsie" => "WeaponBloodDagger",
            "Reaper" => "WeaponDeathDagger",
            "Kharin" => "WeaponHellDagger",
            _ => "WeaponBloodDagger"
        };

        var dagger = _entityManager.SpawnEntity(selectedDagger, cultistCoords);
        component.RecallDaggerActionEntity = dagger;
        _hands.TryPickupAnyHand(cultist, dagger);

        args.Handled = true;
        EmpoweringCheck(args.Action, component);
    }

    private void OnHallucinations(EntityUid cultist, BloodCultistComponent component, BloodCultHallucinationsActionEvent args)
    {
        if (!HasComp<BloodCultistComponent>(args.Target))
            _hallucinations.StartHallucinations(args.Target, "Hallucinations", TimeSpan.FromSeconds(30f), true, "MindBreaker");

        args.Handled = true;
        EmpoweringCheck(args.Action, component);
    }

    private void OnConcealPresence(EntityUid cultist, BloodCultistComponent component, BloodCultConcealPresenceActionEvent args)
    {
        var transform = _entityManager.GetComponent<TransformComponent>(cultist);
        var runes = _entityLookup.GetEntitiesInRange<BloodRuneComponent>(transform.Coordinates, 4f);
        var structures = _entityLookup.GetEntitiesInRange<BloodStructureComponent>(transform.Coordinates, 4f);

        if (runes.Count > 0)
        {
            foreach (var rune in runes)
            {
                if (EntityManager.TryGetComponent(rune.Owner, out BloodRuneComponent? bloodRuneComp))
                {
                    if (EntityManager.TryGetComponent(rune.Owner, out VisibilityComponent? visibilityComp))
                    {
                        var entity = new Entity<VisibilityComponent?>(rune.Owner, visibilityComp);
                        if (bloodRuneComp.IsActive)
                            _visibility.SetLayer(entity, 6);
                        else
                            _visibility.SetLayer(entity, 1);
                    }
                    else
                    {
                        var newVisibilityComp = EntityManager.AddComponent<VisibilityComponent>(rune.Owner);
                        var entity = new Entity<VisibilityComponent?>(rune.Owner, newVisibilityComp);
                        if (bloodRuneComp.IsActive)
                            _visibility.SetLayer(entity, 6);
                        else
                            _visibility.SetLayer(entity, 1);
                    }

                    bloodRuneComp.IsActive = !bloodRuneComp.IsActive;
                }
            }
        }

        if (structures.Count > 0)
        {
            foreach (var structure in structures)
            {
                if (EntityManager.TryGetComponent(structure.Owner, out BloodStructureComponent? bloodStructureComp))
                {
                    if (EntityManager.TryGetComponent(structure.Owner, out VisibilityComponent? visibilityComp))
                    {
                        var entity = new Entity<VisibilityComponent?>(structure.Owner, visibilityComp);
                        if (bloodStructureComp.IsActive)
                            _visibility.SetLayer(entity, 6);
                        else
                            _visibility.SetLayer(entity, 1);
                    }
                    else
                    {
                        var newVisibilityComp = EntityManager.AddComponent<VisibilityComponent>(structure.Owner);
                        var entity = new Entity<VisibilityComponent?>(structure.Owner, newVisibilityComp);
                        if (bloodStructureComp.IsActive)
                            _visibility.SetLayer(entity, 6);
                        else
                            _visibility.SetLayer(entity, 1);
                    }

                    if (EntityManager.TryGetComponent(structure.Owner, out PhysicsComponent? physicsComp))
                    {
                        var fixture = _fixtures.GetFixtureOrNull(structure.Owner, bloodStructureComp.FixtureId);
                        if (fixture != null)
                        {
                            _physics.SetHard(structure.Owner, fixture, !bloodStructureComp.IsActive);
                        }
                    }

                    bloodStructureComp.IsActive = !bloodStructureComp.IsActive;
                }
            }
        }
        args.Handled = true;
        EmpoweringCheck(args.Action, component);
    }
    #region Blood Rites
    private void OnBloodRites(EntityUid cultist, BloodCultistComponent component, BloodCultBloodRitesActionEvent args)
    {
        var spellGear = new ProtoId<StartingGearPrototype>("BloodCultSpellBloodRitesGear");

        var dropEvent = new DropHandItemsEvent();
        RaiseLocalEvent(cultist, ref dropEvent);
        List<ProtoId<StartingGearPrototype>> gear = new() { spellGear };
        _loadout.Equip(cultist, gear, null);

        args.Handled = true;
        EmpoweringCheck(args.Action, component);
    }

    private void BloodRites(Entity<BloodSpellComponent> ent, ref UseInHandEvent args)
    {
        if (!TryComp<BloodSpellComponent>(ent, out var comp) || comp.Prototype.FirstOrDefault() != "bloodrites")
            return;

        args.Handled = true;
        _entityManager.DeleteEntity(ent);
        var netEntity = _entityManager.GetNetEntity(args.User);
        RaiseNetworkEvent(new BloodRitesPressedEvent(netEntity));
    }

    private void BloodRitesSelect(BloodRitesMenuClosedEvent args, EntitySessionEventArgs eventArgs)
    {
        var uid = _entityManager.GetEntity(args.Uid);
        if (!HasComp<BloodCultistComponent>(uid))
            return;

        _action.AddAction(uid, args.SelectedRites);
    }

    private void OnBloodOrb(EntityUid cultist, BloodCultistComponent component, BloodCultBloodOrbActionEvent args)
    {
        if (!TryComp<ActorComponent>(cultist, out var playerActor))
            return;

        var playerSession = playerActor.PlayerSession;
        _quickDialog.OpenDialog(playerSession, Loc.GetString("blood-orb-dialog-title"), Loc.GetString("blood-orb-dialog-prompt"),
            (string input) =>
            {
                if (!int.TryParse(input, out var inputValue) || inputValue <= 0)
                {
                    _popup.PopupEntity(Loc.GetString("blood-orb-invalid-input"), cultist, cultist, PopupType.Medium);
                    return;
                }

                if (inputValue > component.BloodCount)
                {
                    _popup.PopupEntity(Loc.GetString("blood-orb-not-enough-blood"), cultist, cultist, PopupType.Medium);
                }
                else
                {
                    component.BloodCount -= inputValue;

                    var bloodOrb = _entityManager.SpawnEntity("BloodCultOrb", Transform(cultist).Coordinates);
                    EnsureComp<BloodOrbComponent>(bloodOrb, out var orb);
                    orb.Blood = inputValue;

                    _action.RemoveAction(cultist, args.Action!);
                    _popup.PopupEntity(Loc.GetString("blood-orb-success", ("amount", inputValue)), cultist, cultist, PopupType.Medium);
                }
            });

        args.Handled = true;
    }

    private void OnBloodOrbAbsorbed(Entity<BloodOrbComponent> ent, ref UseInHandEvent args)
    {
        var cultist = args.User;
        if (!TryComp<BloodCultistComponent>(cultist, out var cultistcomp)
            || !TryComp<BloodOrbComponent>(ent, out var component))
            return;

        var addedBlood = component.Blood;
        cultistcomp.BloodCount += addedBlood;
        _popup.PopupEntity(Loc.GetString("blood-orb-absorbed"), cultist, cultist, PopupType.Small);
        _entityManager.DeleteEntity(ent);
    }

    private void OnBloodRecharge(EntityUid cultist, BloodCultistComponent component, BloodCultBloodRechargeActionEvent args)
    {
        var target = args.Target;
        if (TryComp<VeilShifterComponent>(target, out var veilShifterComponent))
        {
            var totalActivations = veilShifterComponent.ActivationsCount;
            veilShifterComponent.ActivationsCount = Math.Min(totalActivations + 4, 4);
        }

        _action.RemoveAction(cultist, args.Action!);
    }

    private void OnBloodSpear(EntityUid cultist, BloodCultistComponent component, BloodCultBloodSpearActionEvent args)
    {
        var totalBlood = component.BloodCount;
        if (totalBlood < 150)
        {
            _popup.PopupEntity(Loc.GetString("blood-cult-spear-failed"), cultist, cultist, PopupType.SmallCaution);
            return;
        }

        if (component.RecallSpearActionEntity != null)
        {
            _entityManager.DeleteEntity(component.RecallSpearActionEntity);
            component.RecallSpearActionEntity = null;

            _action.RemoveAction(cultist, component.RecallSpearAction);
            component.RecallSpearAction = null;
        }

        var spear = _entityManager.SpawnEntity("BloodCultSpear", Transform(cultist).Coordinates);
        component.RecallSpearActionEntity = spear;
        _hands.TryPickupAnyHand(cultist, spear);

        var action = _action.AddAction(cultist, BloodCultistComponent.RecallBloodSpear);
        component.RecallSpearAction = action;

        totalBlood -= 150;
        component.BloodCount = totalBlood;
        _action.RemoveAction(cultist, args.Action!);
        args.Handled = true;
    }

    private void OnRecallSpear(EntityUid cultist, BloodCultistComponent component, RecallBloodSpearEvent args)
    {
        if (component.RecallSpearActionEntity is not { } spear || !_entityManager.EntityExists(spear))
        {
            _popup.PopupEntity(Loc.GetString("cult-spear-not-found"), cultist, cultist);
            component.RecallSpearActionEntity = null;
            _action.RemoveAction(cultist, component.RecallSpearAction);
            component.RecallSpearAction = null;
            args.Handled = true;
            return;
        }

        var cultistPosition = _transform.GetWorldPosition(cultist);
        var spearPosition = _transform.GetWorldPosition(spear);
        var distance = (spearPosition - cultistPosition).Length();
        if (distance > 10f)
        {
            _popup.PopupEntity(Loc.GetString("cult-spear-too-far"), cultist, cultist);
            return;
        }

        _transform.SetWorldPosition(spear, cultistPosition);
        _hands.TryPickupAnyHand(cultist, spear);
        _popup.PopupEntity(Loc.GetString("cult-spear-recalled"), cultist, cultist);
        args.Handled = true;
    }

    private void OnBloodBoltBarrage(EntityUid cultist, BloodCultistComponent component, BloodCultBloodBoltBarrageActionEvent args)
    {
        var totalBlood = component.BloodCount;
        if (totalBlood < 300)
        {
            _popup.PopupEntity(Loc.GetString("blood-cult-bolt-barrage-failed"), cultist, cultist, PopupType.SmallCaution);
            return;
        }

        var boltBarrageGear = new ProtoId<StartingGearPrototype>("BloodCultSpellBloodBarrageGear");
        var dropEvent = new DropHandItemsEvent();
        RaiseLocalEvent(cultist, ref dropEvent);
        List<ProtoId<StartingGearPrototype>> gear = new() { boltBarrageGear };
        _loadout.Equip(cultist, gear, null);

        totalBlood -= 300;
        component.BloodCount = totalBlood;
        _action.RemoveAction(cultist, args.Action!);
        args.Handled = true;
    }
    #endregion Blood Rites
    #endregion Abilities

    #region Other
    private void OnInteract(Entity<BloodSpellComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { Valid: true } target
            || !TryComp<BloodSpellComponent>(entity, out var spellComp))
            return;

        var user = args.User;
        switch (spellComp.Prototype.FirstOrDefault())
        {
            case "stun":
                if (!HasComp<BloodCultistComponent>(target) && !HasComp<NullRodOwnerComponent>(target))
                {
                    ExtractBlood(user, -10, 6);
                    if (!HasComp<MutedComponent>(target))
                    {
                        EnsureComp<MutedComponent>(target);
                        Timer.Spawn(10000, () => { RemComp<MutedComponent>(target); });
                    }

                    _stun.TryUpdateParalyzeDuration(target, TimeSpan.FromSeconds(4f));
                    if (!TryComp<FlashImmunityComponent>(target, out var flash))
                        _flash.Flash(target, user, entity, TimeSpan.FromSeconds(2f), 1f);
                    _entityManager.DeleteEntity(entity);
                }
                break;
            case "teleport":
                ExtractBlood(user, -7, 5);
                if (HasComp<NullRodOwnerComponent>(target))
                    break;

                _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, user, TimeSpan.FromSeconds(3f), new TeleportSpellDoAfterEvent(), user, target, entity)
                {
                    BreakOnMove = true,
                    BreakOnDamage = true,
                    MovementThreshold = 0.01f,
                    NeedHand = true
                });
                break;
            case "shadowshackles":
                if (!HasComp<BloodCultistComponent>(target) && !HasComp<NullRodOwnerComponent>(target))
                {
                    if (TryComp<MobStateComponent>(target, out var mobstate) && mobstate.CurrentState != MobState.Alive && mobstate.CurrentState != MobState.Invalid
                        || HasComp<SleepingComponent>(target) || TryComp<StaminaComponent>(target, out var stamina) && stamina.StaminaDamage >= stamina.CritThreshold * 0.9f)
                    {
                        if (TryComp<CuffableComponent>(target, out var cuffable) && cuffable.CanStillInteract)
                        {
                            var handcuffs = _entityManager.SpawnEntity("Handcuffs", Transform(target).Coordinates);
                            if (TryComp<HandcuffComponent>(handcuffs, out var handcuffsComp))
                            {
                                if (_cuff.TryAddNewCuffs(target, user, handcuffs, cuffable, handcuffsComp))
                                {
                                    _cuff.CuffUsed(handcuffsComp);
                                    EnsureComp<MutedComponent>(target);
                                    Timer.Spawn(12000, () => { RemComp<MutedComponent>(target); });
                                    _entityManager.DeleteEntity(entity);
                                }
                                else
                                {
                                    _popup.PopupEntity(Loc.GetString("blood-cult-shadow-shackles-failed"), user, user, PopupType.SmallCaution);
                                    _entityManager.DeleteEntity(handcuffs);
                                }
                            }
                        }
                        else
                        {
                            _popup.PopupEntity(Loc.GetString("blood-cult-shadow-shackles-failed"), user, user, PopupType.SmallCaution);
                        }
                    }
                }
                break;
            case "twistedconstruction":
                if (HasComp<AirlockComponent>(target))
                {
                    ExtractBlood(user, -12, 8);
                    _entityManager.DeleteEntity(entity);

                    var airlockTransform = Transform(target).Coordinates;
                    _entityManager.DeleteEntity(target);
                    _entityManager.SpawnEntity("AirlockBloodCult", airlockTransform);
                }
                else if (TryComp<StackComponent>(target, out var stack))
                {
                    if (_prototypeManager.TryIndex<StackPrototype>(stack.StackTypeId, out var stackPrototype))
                    {
                        if (stackPrototype.ID is "Steel" || stackPrototype.ID is "Plasteel")
                        {
                            ExtractBlood(user, -12, 8);
                            var coords = Transform(target).Coordinates;
                            if (stackPrototype.ID is "Steel" && stack.Count >= 30)
                            {
                                _stack.ReduceCount(target, 30);
                                if (stack.Count > 0)
                                {
                                    _entityManager.SpawnEntity("BloodCultConstruct", coords);
                                }
                                else
                                {
                                    _entityManager.DeleteEntity(target);
                                    _entityManager.SpawnEntity("BloodCultConstruct", coords);
                                }
                            }
                            if (stackPrototype.ID is "Plasteel")
                            {
                                var count = stack.Count;
                                var runeSteel = _entityManager.SpawnEntity("SheetRuneMetal1", coords);
                                _entityManager.DeleteEntity(target);
                                if (TryComp<StackComponent>(runeSteel, out var newStack))
                                {
                                    _stack.SetCount((runeSteel, newStack), count);
                                }
                            }

                            _entityManager.DeleteEntity(entity);
                        }
                    }
                }
                else
                {
                    _popup.PopupEntity(Loc.GetString("blood-cult-twisted-failed"), user, user, PopupType.SmallCaution);
                    _entityManager.DeleteEntity(entity);
                }
                break;
            case "summonequipment":
                _entityManager.DeleteEntity(entity);
                var dropEvent = new DropHandItemsEvent();
                RaiseLocalEvent(target, ref dropEvent);
                ProtoId<StartingGearPrototype> selectedGear = GetCurrentGod() switch
                {
                    "Narsie" => new ProtoId<StartingGearPrototype>("BloodCultWeaponBloodGear"),
                    "Reaper" => new ProtoId<StartingGearPrototype>("BloodCultWeaponDeathGear"),
                    "Kharin" => new ProtoId<StartingGearPrototype>("BloodCultWeaponHellGear"),
                    _ => new ProtoId<StartingGearPrototype>("BloodCultWeaponBloodGear")
                };

                List<ProtoId<StartingGearPrototype>> gear = new() { selectedGear };
                _loadout.Equip(target, gear, null);
                if (TryComp<InventoryComponent>(target, out var targetInventory))
                {
                    var specificSlots = new[] { "outerClothing", "jumpsuit", "back", "shoes" };
                    foreach (var slot in specificSlots)
                    {
                        if (!_inventorySystem.TryGetSlotEntity(target, slot, out var slotEntity, targetInventory))
                        {
                            switch (slot)
                            {
                                case "outerClothing":
                                    var outerClothingGear = new ProtoId<StartingGearPrototype>("BloodCultOuterGear");
                                    List<ProtoId<StartingGearPrototype>> outerClothing = new() { outerClothingGear };
                                    _loadout.Equip(target, outerClothing, null);
                                    break;
                                case "jumpsuit":
                                    var jumpsuitGear = new ProtoId<StartingGearPrototype>("BloodCultJumpsuitGear");
                                    List<ProtoId<StartingGearPrototype>> jumpsuit = new() { jumpsuitGear };
                                    _loadout.Equip(target, jumpsuit, null);
                                    break;
                                case "back":
                                    var backGear = new ProtoId<StartingGearPrototype>("BloodCultBackpackGear");
                                    List<ProtoId<StartingGearPrototype>> back = new() { backGear };
                                    _loadout.Equip(target, back, null);
                                    break;
                                case "shoes":
                                    var shoesGear = new ProtoId<StartingGearPrototype>("BloodCultShoesGear");
                                    List<ProtoId<StartingGearPrototype>> shoes = new() { shoesGear };
                                    _loadout.Equip(target, shoes, null);
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                    _entityManager.DeleteEntity(entity);
                }
                break;
            case "bloodrites":
                if (!TryComp<BloodCultistComponent>(user, out var cultist))
                {
                    _entityManager.DeleteEntity(entity);
                    return;
                }

                if (!TryComp<UseDelayComponent>(entity, out var useDelay) || _useDelay.IsDelayed((entity, useDelay)))
                    return;

                if (HasComp<BloodCultistComponent>(target))
                {
                    if (!TryComp<DamageableComponent>(target, out var damage))
                        return;

                    var totalBlood = cultist.BloodCount;
                    var prioritizedDamageTypes = new[] { "Blunt", "Piercing", "Heat", "Slash", "Caustic" };
                    foreach (var damageType in prioritizedDamageTypes)
                    {
                        if (totalBlood <= 0)
                            break;

                        if (damage.Damage.DamageDict.TryGetValue(damageType, out var currentDamage) && currentDamage > 0)
                        {
                            var healAmount = FixedPoint2.Min(currentDamage, totalBlood);
                            var healSpecifier = new DamageSpecifier { DamageDict = { { damageType, -healAmount } } };
                            _damage.TryChangeDamage(target, healSpecifier, true);
                            totalBlood -= healAmount.Int();
                        }
                    }
                    cultist.BloodCount = totalBlood;
                    args.Handled = true;
                }
                else if (HasComp<HumanoidAppearanceComponent>(target) && !HasComp<NullRodOwnerComponent>(target))
                {
                    if (!TryComp<BloodstreamComponent>(target, out var blood) || HasComp<BloodCultistComponent>(target))
                        return;

                    if (_blood.GetBloodLevelPercentage(target) > 0.6)
                    {
                        _blood.TryModifyBloodLevel(target, -50);
                        cultist.BloodCount += 50;
                    }
                    else
                    {
                        _popup.PopupEntity(Loc.GetString("blood-cult-blood-rites-failed"), user, user, PopupType.SmallCaution);
                    }
                    args.Handled = true;
                }
                else if (TryComp<PuddleComponent>(target, out var puddle))
                {
                    var puddlesInRange = _entityLookup
                        .GetEntitiesInRange<PuddleComponent>(Transform(user).Coordinates, 4f)
                        .Where(puddle => TryComp(puddle.Owner, out ContainerManagerComponent? containerManager) &&
                                        containerManager.Containers.TryGetValue("solution@puddle", out var container) &&
                                        container.ContainedEntities.Any(containedEntity =>
                                            TryComp(containedEntity, out SolutionComponent? solutionComponent) &&
                                            solutionComponent.Solution.Contents.Any(r =>
                                                r.Reagent.Prototype == "Blood" || r.Reagent.Prototype == "CopperBlood")))
                        .ToList();

                    var absorbedBlood = 0;
                    foreach (var bloodPuddle in puddlesInRange)
                    {
                        if (TryComp(bloodPuddle.Owner, out ContainerManagerComponent? containerManager) &&
                            containerManager.Containers.TryGetValue("solution@puddle", out var container))
                        {
                            foreach (var containedEntity in container.ContainedEntities.ToList())
                            {
                                if (TryComp(containedEntity, out SolutionComponent? solutionComponent))
                                {
                                    foreach (var reagent in solutionComponent.Solution.Contents.ToList())
                                    {
                                        if (reagent.Reagent.Prototype == "Blood" || reagent.Reagent.Prototype == "CopperBlood")
                                        {
                                            absorbedBlood += reagent.Quantity.Int();
                                            solutionComponent.Solution.RemoveReagent(reagent.Reagent, reagent.Quantity);
                                        }
                                    }

                                    _entityManager.SpawnEntity("BloodCultFloorGlowEffect", Transform(bloodPuddle.Owner).Coordinates);
                                    if (solutionComponent.Solution.Contents.Count == 0)
                                        _entityManager.DeleteEntity(bloodPuddle.Owner);
                                }
                            }
                        }
                    }
                    cultist.BloodCount += absorbedBlood;
                    args.Handled = true;
                }
                else
                {
                    _popup.PopupEntity(Loc.GetString("blood-cult-blood-rites-failed"), user, user, PopupType.SmallCaution);
                    args.Handled = true;
                }
                _useDelay.TryResetDelay((entity, useDelay));
                break;
            default:
                _popup.PopupEntity(Loc.GetString("blood-cult-spell-failed"), user, user, PopupType.SmallCaution);
                break;
        }
    }

    private void ExtractBlood(EntityUid cultist, int extractBlood, FixedPoint2 bloodDamage)
    {
        if (TryComp<BloodstreamComponent>(cultist, out var blood) && _blood.GetBloodLevelPercentage(cultist) > 0)
            _blood.TryModifyBloodLevel(cultist, extractBlood);
        else
        {
            var damage = new DamageSpecifier { DamageDict = { { "Slash", bloodDamage } } };
            _damage.TryChangeDamage(cultist, damage, true);
        }
    }

    private void OnTeleportDoAfter(EntityUid cultist, BloodCultistComponent component, TeleportSpellDoAfterEvent args)
    {
        if (args.Cancelled || args.Target == null || args.Used == null)
            return;

        _entityManager.DeleteEntity(args.Used);

        var runes = new List<EntityUid>();
        var runeQuery = EntityQueryEnumerator<BloodRuneComponent>();

        while (runeQuery.MoveNext(out var runeUid, out var runeComp))
        {
            if (runeComp.Prototype == "teleport")
                runes.Add(runeUid);
        }

        if (runes.Count > 0)
        {
            var randomRune = runes[_random.Next(runes.Count)];
            var runeTransform = _entityManager.GetComponent<TransformComponent>(randomRune);
            var targetCoords = Transform(args.Target.Value).Coordinates;
            _entityManager.SpawnEntity("BloodCultOutEffect", targetCoords);
            _transform.SetCoordinates(args.Target.Value, runeTransform.Coordinates);
            _entityManager.SpawnEntity("BloodCultInEffect", runeTransform.Coordinates);
            _entityManager.DeleteEntity(randomRune);
        }
    }

    private void EmpoweringCheck(EntityUid spell, BloodCultistComponent component)
    {
        if (component.SelectedEmpoweringSpells.Contains(spell))
        {
            component.Empowering--;
            component.SelectedEmpoweringSpells.Remove(spell);

            _action.RemoveAction(spell);
        }
    }
    #endregion
}
