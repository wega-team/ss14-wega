using System.Linq;
using Content.Server.Administration;
using Content.Server.Blood.Cult.UI;
using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Content.Server.Emp;
using Content.Server.EUI;
using Content.Server.Flash;
using Content.Server.Hallucinations;
using Content.Shared.Bed.Sleep;
using Content.Shared.Blood.Cult;
using Content.Shared.Blood.Cult.Components;
using Content.Shared.Body.Components;
using Content.Shared.Card.Tarot;
using Content.Shared.Card.Tarot.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Clothing;
using Content.Shared.Cuffs;
using Content.Shared.Cuffs.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Doors.Components;
using Content.Shared.EnergyShield;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.NullRod.Components;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.Stacks;
using Content.Shared.Standing;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Content.Shared.Timing;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Blood.Cult;

public sealed partial class BloodCultSystem
{
    [Dependency] private readonly BloodstreamSystem _blood = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly EmpSystem _emp = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly EuiManager _euiMan = default!;
    [Dependency] private readonly FixtureSystem _fixtures = default!;
    [Dependency] private readonly FlashSystem _flash = default!;
    [Dependency] private readonly HallucinationsSystem _hallucinations = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly LoadoutSystem _loadout = default!;
    [Dependency] private readonly QuickDialogSystem _quickDialog = default!;
    [Dependency] private readonly SharedCuffableSystem _cuff = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffect = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly VisibilitySystem _visibility = default!;

    private static readonly SoundPathSpecifier CultSpell = new SoundPathSpecifier("/Audio/_Wega/Effects/cult_spell.ogg");

    private void InitializeBloodAbilities()
    {
        // Blood Magic
        SubscribeLocalEvent<BloodCultistComponent, BloodCultBloodMagicActionEvent>(OnBloodMagic);
        SubscribeLocalEvent<BloodCultistComponent, BloodMagicDoAfterEvent>(DoAfterSpellSelect);

        // Abilities
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
        SubscribeLocalEvent<BloodSpellComponent, BloodRitesSelectRitesMessage>(BloodRitesSelect);
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
        if (_mind.TryGetMind(uid, out _, out var mind) &&
            mind is { UserId: not null } && _player.TryGetSessionById(mind.UserId, out var session))
        {
            var menu = new BloodMagicEui(uid, this);
            _euiMan.OpenEui(menu, session);

            args.Handled = true;
        }
    }

    public void AfterSpellSelect(EntityUid cultist, EntProtoId selectedSpell)
    {
        if (!TryComp<BloodCultistComponent>(cultist, out var cult))
            return;

        if (!cult.BloodMagicActive)
        {
            _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, cultist, TimeSpan.FromSeconds(10f), new BloodMagicDoAfterEvent(selectedSpell), cultist)
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
            _action.RemoveAction(cultist, remSpell);
            cult.SelectedSpell = null;
            cult.BloodMagicActive = false;

            _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, cultist, TimeSpan.FromSeconds(10f), new BloodMagicDoAfterEvent(selectedSpell), cultist)
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
        if (args.Cancelled)
            return;

        component.SelectedSpell = _action.AddAction(cultist, args.SelectedSpell);

        ExtractBlood(cultist, -20, 10);
        component.BloodMagicActive = true;
    }
    #endregion

    #region Abilities
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
        _audio.PlayPvs(CultSpell, dagger);
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
            {
                exclusions.Add(uid);
                continue;
            }

            if (HasComp<BloodCultistComponent>(Transform(uid).ParentUid))
            {
                exclusions.Add(uid);
                if (!TryComp<ContainerManagerComponent>(uid, out var containerManager))
                    continue;

                var containers = _container.GetAllContainers(uid, containerManager)
                    .Where(c => c.ContainedEntities.Count > 0).ToList();

                foreach (var container in containers)
                {
                    foreach (var ent in container.ContainedEntities)
                        exclusions.Add(ent);
                }
            }
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
        if (Exists(component.RecallDaggerActionEntity))
        {
            _popup.PopupEntity(Loc.GetString("blood-cult-blood-dagger-exists"), cultist, cultist, PopupType.SmallCaution);
            args.Handled = true;
            return;
        }

        var cultistCoords = Transform(cultist).Coordinates;
        EntProtoId selectedDagger = GetCurrentGod() switch
        {
            BloodCultGod.NarSi => "WeaponBloodDagger",
            BloodCultGod.Reaper => "WeaponDeathDagger",
            BloodCultGod.Kharin => "WeaponHellDagger",
            _ => "WeaponBloodDagger"
        };

        var dagger = Spawn(selectedDagger, cultistCoords);
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
        var transform = Transform(cultist);
        var runes = _entityLookup.GetEntitiesInRange<BloodRuneComponent>(transform.Coordinates, 4f);
        var structures = _entityLookup.GetEntitiesInRange<BloodStructureComponent>(transform.Coordinates, 4f);

        if (runes.Count > 0)
        {
            foreach (var rune in runes)
            {
                if (TryComp(rune.Owner, out BloodRuneComponent? bloodRuneComp))
                {
                    if (TryComp(rune.Owner, out VisibilityComponent? visibilityComp))
                    {
                        var entity = new Entity<VisibilityComponent?>(rune.Owner, visibilityComp);
                        if (bloodRuneComp.IsActive)
                            _visibility.SetLayer(entity, 6);
                        else
                            _visibility.SetLayer(entity, 1);
                    }
                    else
                    {
                        var newVisibilityComp = AddComp<VisibilityComponent>(rune.Owner);
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
                if (TryComp(structure.Owner, out BloodStructureComponent? bloodStructureComp))
                {
                    if (TryComp(structure.Owner, out VisibilityComponent? visibilityComp))
                    {
                        var entity = new Entity<VisibilityComponent?>(structure.Owner, visibilityComp);
                        if (bloodStructureComp.IsActive)
                            _visibility.SetLayer(entity, 6);
                        else
                            _visibility.SetLayer(entity, 1);
                    }
                    else
                    {
                        var newVisibilityComp = AddComp<VisibilityComponent>(structure.Owner);
                        var entity = new Entity<VisibilityComponent?>(structure.Owner, newVisibilityComp);
                        if (bloodStructureComp.IsActive)
                            _visibility.SetLayer(entity, 6);
                        else
                            _visibility.SetLayer(entity, 1);
                    }

                    if (HasComp<PhysicsComponent>(structure.Owner))
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
        if (!HasComp<BloodCultistComponent>(args.User) || ent.Comp.SpellType != BloodCultSpell.BloodRites)
            return;

        args.Handled = true;
        _ui.OpenUi(ent.Owner, BloodRitesUiKey.Key, args.User);
    }

    private void BloodRitesSelect(Entity<BloodSpellComponent> ent, ref BloodRitesSelectRitesMessage args)
    {
        if (!HasComp<BloodCultistComponent>(args.Actor) || ent.Comp.SpellType != BloodCultSpell.BloodRites)
            return;

        _action.AddAction(args.Actor, args.Rites);
        QueueDel(ent);
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

                    var bloodOrb = Spawn("BloodCultOrb", Transform(cultist).Coordinates);
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

        cultistcomp.BloodCount += component.Blood;
        _popup.PopupEntity(Loc.GetString("blood-orb-absorbed"), cultist, cultist, PopupType.Small);
        QueueDel(ent);
    }

    private void OnBloodRecharge(EntityUid cultist, BloodCultistComponent component, BloodCultBloodRechargeActionEvent args)
    {
        if (component.BloodCount < 75)
        {
            _popup.PopupEntity(Loc.GetString("blood-cult-recharge-failed"), cultist, cultist, PopupType.SmallCaution);
            return;
        }

        var target = args.Target;
        if (TryComp<VeilShifterComponent>(target, out var veilShifter))
        {
            var totalActivations = veilShifter.ActivationsCount;
            veilShifter.ActivationsCount = Math.Min(totalActivations + 4, 4);

            _appearance.SetData(target, VeilShifterVisuals.Charged, veilShifter.ActivationsCount > 0);

            component.BloodCount -= 75;
            _audio.PlayPvs(CultSpell, target);
            _action.RemoveAction(cultist, args.Action!);
        }
        else if (TryComp<BloodShieldActivaebleComponent>(target, out var bloodShield) && !HasComp<EnergyShieldOwnerComponent>(cultist))
        {
            _inventory.TryUnequip(cultist, bloodShield.CurrentSlot, force: true);
            if (_inventory.TryEquip(cultist, target, bloodShield.CurrentSlot, force: true))
            {
                var shield = EnsureComp<EnergyShieldOwnerComponent>(cultist);
                shield.ShieldEntity = Spawn("BloodCultShieldEffect", Transform(cultist).Coordinates);
                _transform.SetParent(shield.ShieldEntity.Value, cultist);

                _audio.PlayPvs(CultSpell, target);
            }

            component.BloodCount -= 75;
            _action.RemoveAction(cultist, args.Action!);
        }
        else if (TryComp<CardTarotComponent>(target, out var tarot) && tarot.Card == CardTarot.NotEnchanted
            && component.BloodCount >= 100)
        {
            var allCards = Enum.GetValues<CardTarot>();
            tarot.Card = (CardTarot)_random.Next(1, allCards.Length);

            bool reversed = _random.Prob(0.5f);
            if (reversed) tarot.CardType = CardTarotType.Reversed;

            _appearance.SetData(target, CardTarotVisuals.State, tarot.Card);
            _appearance.SetData(target, CardTarotVisuals.Reversed, reversed);

            _meta.SetEntityName(target, Loc.GetString("tarot-card-name"));
            _meta.SetEntityDescription(target, Loc.GetString("tarot-card-desc"));

            component.BloodCount -= 100;
            _action.RemoveAction(cultist, args.Action!);
        }
    }

    private void OnBloodSpear(EntityUid cultist, BloodCultistComponent component, BloodCultBloodSpearActionEvent args)
    {
        if (component.BloodCount < 150)
        {
            _popup.PopupEntity(Loc.GetString("blood-cult-spear-failed"), cultist, cultist, PopupType.SmallCaution);
            return;
        }

        if (component.RecallSpearActionEntity != null)
        {
            QueueDel(component.RecallSpearActionEntity);
            component.RecallSpearActionEntity = null;

            _action.RemoveAction(cultist, component.RecallSpearAction);
            component.RecallSpearAction = null;
        }

        var spear = Spawn("BloodCultSpear", Transform(cultist).Coordinates);
        component.RecallSpearActionEntity = spear;
        _hands.TryPickupAnyHand(cultist, spear);

        var action = _action.AddAction(cultist, BloodCultistComponent.RecallBloodSpear);
        component.RecallSpearAction = action;

        component.BloodCount -= 150;
        _action.RemoveAction(cultist, args.Action!);
        args.Handled = true;
    }

    private void OnRecallSpear(EntityUid cultist, BloodCultistComponent component, RecallBloodSpearEvent args)
    {
        if (component.RecallSpearActionEntity is not { } spear || !Exists(spear))
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
        if (component.BloodCount < 300)
        {
            _popup.PopupEntity(Loc.GetString("blood-cult-bolt-barrage-failed"), cultist, cultist, PopupType.SmallCaution);
            return;
        }

        var boltBarrageGear = new ProtoId<StartingGearPrototype>("BloodCultSpellBloodBarrageGear");
        var dropEvent = new DropHandItemsEvent();
        RaiseLocalEvent(cultist, ref dropEvent);
        List<ProtoId<StartingGearPrototype>> gear = new() { boltBarrageGear };
        _loadout.Equip(cultist, gear, null);

        component.BloodCount -= 300;
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
        switch (spellComp.SpellType)
        {
            case BloodCultSpell.Stun:
                HandleStunSpell(entity, user, target);
                break;
            case BloodCultSpell.Teleport:
                HandleTeleportSpell(entity, user, target);
                break;
            case BloodCultSpell.ShadowShackles:
                HandleShadowShacklesSpell(entity, user, target);
                break;
            case BloodCultSpell.TwistedConstruction:
                HandleTwistedConstructionSpell(entity, user, target);
                break;
            case BloodCultSpell.SummonEquipment:
                HandleSummonEquipmentSpell(entity, user, target);
                break;
            case BloodCultSpell.BloodRites:
                HandleBloodRitesSpell(entity, user, target, ref args);
                break;
            default:
                _popup.PopupEntity(Loc.GetString("blood-cult-spell-failed"), user, user, PopupType.SmallCaution);
                break;
        }
    }

    #region Spell Handlers

    private void HandleStunSpell(Entity<BloodSpellComponent> spell, EntityUid user, EntityUid target)
    {
        if (HasComp<BloodCultistComponent>(target) || HasComp<NullRodOwnerComponent>(target))
            return;

        ExtractBlood(user, -10, 6);

        _stun.TryKnockdown(target, TimeSpan.FromSeconds(4f));
        _statusEffect.TryAddStatusEffectDuration(target, "Muted", TimeSpan.FromSeconds(10f));
        _flash.Flash(target, user, spell, TimeSpan.FromSeconds(2f), 1f);

        QueueDel(spell);
    }

    private void HandleTeleportSpell(Entity<BloodSpellComponent> spell, EntityUid user, EntityUid target)
    {
        if (HasComp<NullRodOwnerComponent>(target))
            return;

        ExtractBlood(user, -7, 5);

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, user, TimeSpan.FromSeconds(2f),
            new TeleportSpellDoAfterEvent(), user, target, spell)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 0.01f,
            NeedHand = true
        });
    }

    private void HandleShadowShacklesSpell(Entity<BloodSpellComponent> spell, EntityUid user, EntityUid target)
    {
        if (HasComp<BloodCultistComponent>(target) || HasComp<NullRodOwnerComponent>(target))
            return;

        if (!IsValidForCuffing(target))
        {
            _popup.PopupEntity(Loc.GetString("blood-cult-shadow-shackles-failed"), user, user, PopupType.SmallCaution);
            return;
        }

        TryApplyHandcuffs(spell, user, target);
    }

    private bool IsValidForCuffing(EntityUid target)
    {
        if (!_mobState.IsAlive(target))
            return true;

        if (HasComp<SleepingComponent>(target))
            return true;

        if (TryComp<StaminaComponent>(target, out var stamina) &&
            stamina.StaminaDamage >= stamina.CritThreshold * 0.9f)
            return true;

        return false;
    }

    private void TryApplyHandcuffs(Entity<BloodSpellComponent> spell, EntityUid user, EntityUid target)
    {
        if (!TryComp<CuffableComponent>(target, out var cuffable) || !cuffable.CanStillInteract)
        {
            _popup.PopupEntity(Loc.GetString("blood-cult-shadow-shackles-failed"), user, user, PopupType.SmallCaution);
            return;
        }

        var handcuffs = Spawn("Handcuffs", Transform(target).Coordinates);
        if (!TryComp<HandcuffComponent>(handcuffs, out var handcuffsComp) ||
            !_cuff.TryAddNewCuffs(target, user, handcuffs, cuffable, handcuffsComp))
        {
            _popup.PopupEntity(Loc.GetString("blood-cult-shadow-shackles-failed"), user, user, PopupType.SmallCaution);
            QueueDel(handcuffs);
            return;
        }

        _cuff.CuffUsed(handcuffsComp);
        _statusEffect.TryAddStatusEffectDuration(target, "Muted", TimeSpan.FromSeconds(12f));

        QueueDel(spell);
    }

    private void HandleTwistedConstructionSpell(Entity<BloodSpellComponent> spell, EntityUid user, EntityUid target)
    {
        if (HasComp<AirlockComponent>(target))
        {
            TransformAirlock(spell, user, target);
            return;
        }

        if (TryComp<StackComponent>(target, out var stack))
        {
            TransformMaterial(spell, user, target, stack);
            return;
        }

        _popup.PopupEntity(Loc.GetString("blood-cult-twisted-failed"), user, user, PopupType.SmallCaution);
    }

    private void TransformAirlock(Entity<BloodSpellComponent> spell, EntityUid user, EntityUid airlock)
    {
        ExtractBlood(user, -12, 8);

        string spawnProto = "AirlockBloodCult";
        if (TryComp<DoorComponent>(airlock, out var door) && !door.Occludes)
            spawnProto = "AirlockBloodCultGlass";

        var airlockTransform = Transform(airlock).Coordinates;
        QueueDel(airlock);

        var ent = Spawn(spawnProto, airlockTransform);
        _audio.PlayPvs(CultSpell, ent);
        QueueDel(spell);
    }

    private void TransformMaterial(Entity<BloodSpellComponent> spell, EntityUid user, EntityUid material, StackComponent stack)
    {
        if (!_prototypeManager.TryIndex(stack.StackTypeId, out var stackPrototype))
            return;

        if (stackPrototype.ID is not ("Steel" or "Plasteel"))
            return;

        ExtractBlood(user, -12, 8);
        var coords = Transform(material).Coordinates;

        if (stackPrototype.ID == "Steel" && stack.Count >= 30)
        {
            TransformSteelToConstruct(material, coords, stack);
        }
        else if (stackPrototype.ID == "Plasteel")
        {
            TransformPlasteelToRuneMetal(material, coords, stack.Count);
        }

        _audio.PlayPvs(CultSpell, user);
        QueueDel(spell);
    }

    private void TransformSteelToConstruct(EntityUid steelStack, EntityCoordinates coords, StackComponent stack)
    {
        _stack.ReduceCount(steelStack, 30);
        if (stack.Count > 0)
        {
            Spawn("BloodCultConstruct", coords);
        }
        else
        {
            QueueDel(steelStack);
            Spawn("BloodCultConstruct", coords);
        }
    }

    private void TransformPlasteelToRuneMetal(EntityUid plasteelStack, EntityCoordinates coords, int count)
    {
        var runeSteel = Spawn("SheetRuneMetal1", coords);
        QueueDel(plasteelStack);

        if (TryComp<StackComponent>(runeSteel, out var newStack))
            _stack.SetCount((runeSteel, newStack), count);
    }

    private void HandleSummonEquipmentSpell(Entity<BloodSpellComponent> spell, EntityUid user, EntityUid target)
    {
        QueueDel(spell);

        var dropEvent = new DropHandItemsEvent();
        RaiseLocalEvent(target, ref dropEvent);

        var selectedGear = GetGodWeaponGear();
        var gear = new List<ProtoId<StartingGearPrototype>> { selectedGear };
        _loadout.Equip(target, gear, null);

        FillMissingEquipmentSlots(target);

        QueueDel(spell);
    }

    private ProtoId<StartingGearPrototype> GetGodWeaponGear()
    {
        return GetCurrentGod() switch
        {
            BloodCultGod.NarSi => new ProtoId<StartingGearPrototype>("BloodCultWeaponBloodGear"),
            BloodCultGod.Reaper => new ProtoId<StartingGearPrototype>("BloodCultWeaponDeathGear"),
            BloodCultGod.Kharin => new ProtoId<StartingGearPrototype>("BloodCultWeaponHellGear"),
            _ => new ProtoId<StartingGearPrototype>("BloodCultWeaponBloodGear")
        };
    }

    private void FillMissingEquipmentSlots(EntityUid target)
    {
        if (!TryComp<InventoryComponent>(target, out var targetInventory))
            return;

        var slotGearMap = new Dictionary<string, ProtoId<StartingGearPrototype>>
        {
            ["outerClothing"] = new ProtoId<StartingGearPrototype>("BloodCultOuterGear"),
            ["jumpsuit"] = new ProtoId<StartingGearPrototype>("BloodCultJumpsuitGear"),
            ["back"] = new ProtoId<StartingGearPrototype>("BloodCultBackpackGear"),
            ["shoes"] = new ProtoId<StartingGearPrototype>("BloodCultShoesGear")
        };

        foreach (var (slot, gearPrototype) in slotGearMap)
        {
            if (!_inventory.TryGetSlotEntity(target, slot, out _, targetInventory))
            {
                var gear = new List<ProtoId<StartingGearPrototype>> { gearPrototype };
                _loadout.Equip(target, gear, null);
            }
        }
    }

    private void HandleBloodRitesSpell(Entity<BloodSpellComponent> spell, EntityUid user, EntityUid target, ref AfterInteractEvent args)
    {
        if (!TryComp<BloodCultistComponent>(user, out var cultist))
        {
            QueueDel(spell);
            return;
        }

        if (!TryComp<UseDelayComponent>(spell, out var useDelay) || _useDelay.IsDelayed((spell, useDelay)))
            return;

        var handled = false;
        if (HasComp<BloodCultistComponent>(target))
        {
            handled = HealCultist(cultist, target);
        }
        else if (HasComp<HumanoidAppearanceComponent>(target) && !HasComp<NullRodOwnerComponent>(target))
        {
            handled = StealBloodFromHumanoid(cultist, user, target);
        }
        else if (TryComp<PuddleComponent>(target, out _))
        {
            handled = AbsorbBloodFromPuddles(cultist, user);
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("blood-cult-blood-rites-failed"), user, user, PopupType.SmallCaution);
            handled = true;
        }

        if (handled)
        {
            args.Handled = true;
            _useDelay.TryResetDelay((spell, useDelay));
        }
    }

    private bool HealCultist(BloodCultistComponent cultist, EntityUid target)
    {
        if (!TryComp<DamageableComponent>(target, out var damage))
            return false;

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
        return true;
    }

    private bool StealBloodFromHumanoid(BloodCultistComponent cultist, EntityUid user, EntityUid target)
    {
        if (!HasComp<BloodstreamComponent>(target) || HasComp<BloodCultistComponent>(target))
            return false;

        if (_blood.GetBloodLevel(target) > 0.6)
        {
            _blood.TryModifyBloodLevel(target, -50);
            cultist.BloodCount += 50;
            return true;
        }

        _popup.PopupEntity(Loc.GetString("blood-cult-blood-rites-failed"), user, user, PopupType.SmallCaution);
        return false;
    }

    private bool AbsorbBloodFromPuddles(BloodCultistComponent cultist, EntityUid user)
    {
        var puddlesInRange = _entityLookup
            .GetEntitiesInRange<PuddleComponent>(Transform(user).Coordinates, 4f)
            .Where(puddle => IsBloodPuddle(puddle.Owner))
            .ToList();

        var absorbedBlood = 0;
        foreach (var bloodPuddle in puddlesInRange)
        {
            absorbedBlood += ExtractBloodFromPuddle(bloodPuddle.Owner);
        }

        cultist.BloodCount += absorbedBlood;
        return true;
    }

    private bool IsBloodPuddle(EntityUid puddle)
    {
        if (!TryComp(puddle, out ContainerManagerComponent? containerManager) ||
            !containerManager.Containers.TryGetValue("solution@puddle", out var container))
            return false;

        return container.ContainedEntities.Any(containedEntity =>
            TryComp(containedEntity, out SolutionComponent? solutionComponent) &&
            solutionComponent.Solution.Contents.Any(r =>
                r.Reagent.Prototype == "Blood" || r.Reagent.Prototype == "CopperBlood"));
    }

    private int ExtractBloodFromPuddle(EntityUid puddle)
    {
        if (!TryComp(puddle, out ContainerManagerComponent? containerManager) ||
            !containerManager.Containers.TryGetValue("solution@puddle", out var container))
            return 0;

        var absorbedBlood = 0;
        foreach (var containedEntity in container.ContainedEntities.ToList())
        {
            if (!TryComp(containedEntity, out SolutionComponent? solutionComponent))
                continue;

            foreach (var reagent in solutionComponent.Solution.Contents.ToList())
            {
                if (reagent.Reagent.Prototype == "Blood" || reagent.Reagent.Prototype == "CopperBlood")
                {
                    absorbedBlood += reagent.Quantity.Int();
                    solutionComponent.Solution.RemoveReagent(reagent.Reagent, reagent.Quantity);
                }
            }

            Spawn("BloodCultFloorGlowEffect", Transform(puddle).Coordinates);

            if (solutionComponent.Solution.Contents.Count == 0)
                QueueDel(puddle);
        }

        return absorbedBlood;
    }

    #endregion

    private void ExtractBlood(EntityUid cultist, int extractBlood, FixedPoint2 bloodDamage)
    {
        if (HasComp<BloodstreamComponent>(cultist) && _blood.GetBloodLevel(cultist) > 0)
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

        QueueDel(args.Used);

        var runes = new List<EntityUid>();
        var runeQuery = EntityQueryEnumerator<BloodRuneComponent>();

        while (runeQuery.MoveNext(out var runeUid, out var runeComp))
        {
            if (runeComp.RuneType == BloodCultRune.Teleport)
                runes.Add(runeUid);
        }

        if (runes.Count > 0)
        {
            var randomRune = runes[_random.Next(runes.Count)];
            Spawn("BloodCultOutEffect", Transform(args.Target.Value).Coordinates);
            _transform.SetCoordinates(args.Target.Value, Transform(randomRune).Coordinates);
            Spawn("BloodCultInEffect", Transform(randomRune).Coordinates);
            QueueDel(randomRune);
        }
    }

    private void EmpoweringCheck(EntityUid spell, BloodCultistComponent component)
    {
        if (component.SelectedEmpoweringSpells.Contains(spell))
        {
            component.SelectedEmpoweringSpells.Remove(spell);
            _action.RemoveAction(spell);
        }
    }
    #endregion
}
