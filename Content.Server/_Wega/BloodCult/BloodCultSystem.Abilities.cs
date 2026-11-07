using System.Linq;
using Content.Server.Administration;
using Content.Server.Blood.Cult.UI;
using Content.Server.Chat.Systems;
using Content.Server.Emp;
using Content.Server.EUI;
using Content.Server.Flash;
using Content.Server.Hallucinations;
using Content.Shared.Bed.Sleep;
using Content.Shared.Blood.Cult;
using Content.Shared.Blood.Cult.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
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
using Content.Shared.Timing.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Examine;
using Content.Shared.Timing.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs;
using Content.Shared.Charges.Systems;
using Content.Shared.Actions.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Blood.Cult;

public sealed partial class BloodCultSystem
{
    [Dependency] private BloodstreamSystem _blood = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private EmpSystem _emp = default!;
    [Dependency] private EntityLookupSystem _entityLookup = default!;
    [Dependency] private EuiManager _euiMan = default!;
    [Dependency] private FixtureSystem _fixtures = default!;
    [Dependency] private FlashSystem _flash = default!;
    [Dependency] private HallucinationsSystem _hallucinations = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private LoadoutSystem _loadout = default!;
    [Dependency] private QuickDialogSystem _quickDialog = default!;
    [Dependency] private SharedCuffableSystem _cuff = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedStackSystem _stack = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private StatusEffectsSystem _statusEffect = default!;
    [Dependency] private UseDelaySystem _useDelay = default!;
    [Dependency] private VisibilitySystem _visibility = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private SharedChargesSystem _charges = default!;

    private static readonly SoundPathSpecifier CultSpell = new SoundPathSpecifier("/Audio/_Wega/Effects/cult_spell.ogg");
    private static readonly EntProtoId Muted = "StatusEffectMuted";

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
        SubscribeLocalEvent<BloodSpellComponent, ExaminedEvent>(OnExamine);
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
        TrySpawnSpellInHand(cultist, "BloodCultSpellStun", args.Action);
    }

    private void OnTeleport(EntityUid cultist, BloodCultistComponent component, BloodCultTeleportActionEvent args)
    {
        TrySpawnSpellInHand(cultist, "BloodCultSpellTeleport", args.Action);
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
        TrySpawnSpellInHand(cultist, "BloodCultSpellShadowShackles", args.Action);
    }

    private void OnTwistedConstruction(EntityUid cultist, BloodCultistComponent component, BloodCultTwistedConstructionActionEvent args)
    {
        TrySpawnSpellInHand(cultist, "BloodCultSpellTwistedConstruction", args.Action);
    }

    private void OnSummonEquipment(EntityUid cultist, BloodCultistComponent component, BloodCultSummonEquipmentActionEvent args)
    {
        TrySpawnSpellInHand(cultist, "BloodCultSpellSummonEquipment", args.Action);
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

        var dagger = SpawnAtPosition(selectedDagger, cultistCoords);
        component.RecallDaggerActionEntity = dagger;
        _hands.TryPickupAnyHand(cultist, dagger);

        component.SelectedEmpoweringSpells.Remove(args.Action);
        _action.RemoveAction(args.Action.Owner);
        args.Handled = true;
    }

    private void OnHallucinations(EntityUid cultist, BloodCultistComponent component, BloodCultHallucinationsActionEvent args)
    {
        if (!HasComp<ActorComponent>(args.Target) || HasComp<BloodCultistComponent>(args.Target))
            return;

        _hallucinations.StartHallucinations(
            args.Target,
            "Hallucinations",
            TimeSpan.FromSeconds(30f),
            true,
            "Bloodcult");

        if (!_charges.TryUseCharge(args.Action.Owner))
            return;

        _action.StartUseDelay(args.Action.Owner);

        if (_charges.IsEmpty(args.Action.Owner))
        {
            component.SelectedEmpoweringSpells.Remove(args.Action.Owner);
            _action.RemoveAction(args.Action.Owner);
        }
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
        if (!_charges.TryUseCharge(args.Action.Owner))
            return;

        _action.StartUseDelay(args.Action.Owner);

        if (_charges.IsEmpty(args.Action.Owner))
        {
            component.SelectedEmpoweringSpells.Remove(args.Action.Owner);
            _action.RemoveAction(args.Action.Owner);
        }
    }

    #region Blood Rites
    private void OnBloodRites(EntityUid cultist, BloodCultistComponent component, BloodCultBloodRitesActionEvent args)
    {
        if (TrySpawnSpellInHand(cultist, "BloodCultSpellBloodRites"))
        {
            args.Handled = true;
            EmpoweringCheck(args.Action, component);
        }
    }

    private void OnExamine(EntityUid uid, BloodSpellComponent spell, ExaminedEvent args)
    {
        if (spell.SpellType != BloodCultSpell.BloodRites)
            return;
        if (TryComp<BloodCultistComponent>(args.Examiner, out var cultist))
        {
            args.PushMarkup(Loc.GetString("blood-rites-count",
                ("blood", cultist.BloodCount)));
        }
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
        if (!TryComp<BloodCultistComponent>(args.Actor, out var cultist) ||
            ent.Comp.SpellType != BloodCultSpell.BloodRites)
            return;

        if (cultist.BloodRitesCooldowns.TryGetValue(args.Rites, out var cooldown) &&
            _gameTiming.CurTime < cooldown)
        {
            var remaining = Math.Ceiling((cooldown - _gameTiming.CurTime).TotalSeconds);

            _popup.PopupEntity(
                Loc.GetString("blood-cult-rites-cooldown", ("seconds", remaining)),
                args.Actor,
                args.Actor,
                PopupType.SmallCaution);

            return;
        }

        var actionUid = _action.AddAction(args.Actor, args.Rites);
        if (actionUid is not { } action)
            return;

        if (TryComp<ActionComponent>(action, out var actionComp) &&
            actionComp.UseDelay is { } delay)
        {
            cultist.BloodRitesCooldowns[args.Rites] = _gameTiming.CurTime + delay;
        }

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

                    var bloodOrb = SpawnAtPosition("BloodCultOrb", Transform(cultist).Coordinates);
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
                shield.ShieldEntity = SpawnAtPosition("BloodCultShieldEffect", Transform(cultist).Coordinates);
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

        var spear = SpawnAtPosition("BloodCultSpear", Transform(cultist).Coordinates);
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
        if (component.BloodCount < 200)
        {
            _popup.PopupEntity(Loc.GetString("blood-cult-bolt-barrage-failed"), cultist, cultist, PopupType.SmallCaution);
            return;
        }

        if (TrySpawnSpellInHand(cultist, "BloodCultSpellBloodBarrage"))
        {
            component.BloodCount -= 200;
            _action.RemoveAction(cultist, args.Action!);
            args.Handled = true;
        }
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

        if (TryComp<BloodSpellSourceActionComponent>(entity.Owner, out var source))
        {
            if (source.Action is not { } action ||
                !Exists(action) ||
                !TryComp<ActionComponent>(action, out var actionComp) ||
                actionComp.AttachedEntity != user ||
                !_charges.HasCharges(action, 1))
            {
                args.Handled = true;
                QueueDel(entity.Owner);
                return;
            }
        }

        switch (spellComp.SpellType)
        {
            case BloodCultSpell.Stun:
                if (HandleStunSpell(entity, user, target))
                    args.Handled = true;
                break;
            case BloodCultSpell.Teleport:
                if (HandleTeleportSpell(entity, user, target))
                    args.Handled = true;
                break;
            case BloodCultSpell.ShadowShackles:
                if (HandleShadowShacklesSpell(entity, user, target))
                    args.Handled = true;
                break;
            case BloodCultSpell.TwistedConstruction:
                if (HandleTwistedConstructionSpell(entity, user, target))
                    args.Handled = true;
                break;
            case BloodCultSpell.SummonEquipment:
                if (HandleSummonEquipmentSpell(entity, user, target))
                    args.Handled = true;
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

    private bool HandleStunSpell(Entity<BloodSpellComponent> spell, EntityUid user, EntityUid target)
    {
        if (!TryComp<MobStateComponent>(target, out var mobState) ||
            mobState.CurrentState != MobState.Alive ||
            HasComp<BloodCultistComponent>(target) ||
            HasComp<NullRodOwnerComponent>(target))
            return false;

        ExtractBlood(user, -10, 6);

        _stun.TryKnockdown(target, TimeSpan.FromSeconds(4f));
        _statusEffect.TryAddStatusEffectDuration(target, Muted, TimeSpan.FromSeconds(10f));
        _flash.Flash(target, user, spell, TimeSpan.FromSeconds(2f), 1f);

        ConsumeSpellAction(spell.Owner, user);
        QueueDel(spell);
        return true;
    }

    private bool HandleTeleportSpell(Entity<BloodSpellComponent> spell, EntityUid user, EntityUid target)
    {
        if (HasComp<NullRodOwnerComponent>(target))
            return false;

        return _doAfterSystem.TryStartDoAfter(new DoAfterArgs(
            EntityManager,
            user,
            TimeSpan.FromSeconds(2f),
            new TeleportSpellDoAfterEvent(),
            user,
            target,
            spell)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 0.01f,
            NeedHand = true
        });
    }

    private bool HandleShadowShacklesSpell(Entity<BloodSpellComponent> spell, EntityUid user, EntityUid target)
    {
        if (HasComp<BloodCultistComponent>(target) || HasComp<NullRodOwnerComponent>(target))
            return false;

        if (!IsValidForCuffing(target))
        {
            _popup.PopupEntity(Loc.GetString("blood-cult-shadow-shackles-failed"), user, user, PopupType.SmallCaution);
            return false;
        }

        return TryApplyHandcuffs(spell, user, target);
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

    private bool TryApplyHandcuffs(Entity<BloodSpellComponent> spell, EntityUid user, EntityUid target)
    {
        if (!TryComp<CuffableComponent>(target, out var cuffable) || !cuffable.CanStillInteract)
        {
            _popup.PopupEntity(Loc.GetString("blood-cult-shadow-shackles-failed"), user, user, PopupType.SmallCaution);
            return false;
        }

        var handcuffs = SpawnAtPosition("Handcuffs", Transform(target).Coordinates);

        if (!TryComp<HandcuffComponent>(handcuffs, out var handcuffsComp) ||
            !_cuff.TryAddNewCuffs(target, user, handcuffs, cuffable, handcuffsComp))
        {
            _popup.PopupEntity(Loc.GetString("blood-cult-shadow-shackles-failed"), user, user, PopupType.SmallCaution);
            QueueDel(handcuffs);
            return false;
        }

        _cuff.CuffUsed(handcuffsComp);
        _statusEffect.TryAddStatusEffectDuration(target, Muted, TimeSpan.FromSeconds(12f));

        ConsumeSpellAction(spell.Owner, user);
        QueueDel(spell);

        return true;
    }

    private bool HandleTwistedConstructionSpell(Entity<BloodSpellComponent> spell, EntityUid user, EntityUid target)
    {
        if (HasComp<AirlockComponent>(target))
            return TransformAirlock(spell, user, target);

        if (TryComp<StackComponent>(target, out var stack))
            return TransformMaterial(spell, user, target, stack);

        _popup.PopupEntity(Loc.GetString("blood-cult-twisted-failed"), user, user, PopupType.SmallCaution);
        return false;
    }

    private bool TransformAirlock(Entity<BloodSpellComponent> spell, EntityUid user, EntityUid airlock)
    {
        ExtractBlood(user, -12, 8);

        string spawnProto = "AirlockBloodCult";
        if (TryComp<DoorComponent>(airlock, out var door) && !door.Occludes)
            spawnProto = "AirlockBloodCultGlass";

        var airlockTransform = Transform(airlock).Coordinates;
        QueueDel(airlock);

        var ent = SpawnAtPosition(spawnProto, airlockTransform);
        _audio.PlayPvs(CultSpell, ent);

        ConsumeSpellAction(spell.Owner, user);
        QueueDel(spell);
        return true;
    }

    private bool TransformMaterial(Entity<BloodSpellComponent> spell, EntityUid user, EntityUid material, StackComponent stack)
    {
        if (!ProtoMan.TryIndex(stack.StackTypeId, out var stackPrototype))
            return false;

        if (stackPrototype.ID is not ("Steel" or "Plasteel"))
            return false;

        var coords = Transform(material).Coordinates;

        if (stackPrototype.ID == "Steel")
        {
            if (stack.Count < 30)
                return false;

            TransformSteelToConstruct(material, coords, stack);
        }
        else
        {
            TransformPlasteelToRuneMetal(material, coords, stack.Count);
        }

        ExtractBlood(user, -12, 8);
        _audio.PlayPvs(CultSpell, user);

        ConsumeSpellAction(spell.Owner, user);
        QueueDel(spell);
        return true;
    }

    private void TransformSteelToConstruct(EntityUid steelStack, EntityCoordinates coords, StackComponent stack)
    {
        _stack.ReduceCount(steelStack, 30);
        if (stack.Count > 0)
        {
            SpawnAtPosition("BloodCultConstruct", coords);
        }
        else
        {
            QueueDel(steelStack);
            SpawnAtPosition("BloodCultConstruct", coords);
        }
    }

    private void TransformPlasteelToRuneMetal(EntityUid plasteelStack, EntityCoordinates coords, int count)
    {
        var runeSteel = SpawnAtPosition("SheetRuneMetal1", coords);
        QueueDel(plasteelStack);

        if (TryComp<StackComponent>(runeSteel, out var newStack))
            _stack.SetCount((runeSteel, newStack), count);
    }

    private bool HandleSummonEquipmentSpell(Entity<BloodSpellComponent> spell, EntityUid user, EntityUid target)
    {
        if (!HasComp<BloodCultistComponent>(target) ||
            !HasComp<InventoryComponent>(target))
            return false;

        var dropEvent = new DropHandItemsEvent();
        RaiseLocalEvent(target, ref dropEvent);

        var selectedGear = GetGodWeaponGear();
        var gear = new List<ProtoId<StartingGearPrototype>> { selectedGear };

        _loadout.Equip(target, gear, null);
        FillMissingEquipmentSlots(target);

        ConsumeSpellAction(spell.Owner, user);
        QueueDel(spell);

        return true;
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
        var delay = TimeSpan.FromSeconds(5);

        if (HasComp<BloodCultistComponent>(target))
        {
            var healedDamage = HealCultist(cultist, target);

            if (healedDamage > 0)
            {
                handled = true;

                var cooldown = Math.Clamp(
                    (int)Math.Ceiling(healedDamage / 15.0),
                    1,
                    10);

                delay = TimeSpan.FromSeconds(cooldown);
            }
        }
        else if (HasComp<HumanoidProfileComponent>(target) && !HasComp<NullRodOwnerComponent>(target))
        {
            handled = StealBloodFromHumanoid(cultist, user, target);
        }
        else if (HasComp<PuddleComponent>(target))
        {
            handled = AbsorbBloodFromPuddles(cultist, user);
            delay = TimeSpan.FromSeconds(1);
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("blood-cult-blood-rites-failed"), user, user, PopupType.SmallCaution);
            return;
        }

        if (handled)
        {
            args.Handled = true;
            _useDelay.SetLength((spell, useDelay), delay);
            _useDelay.TryResetDelay((spell, useDelay));
        }
    }

    private int HealCultist(BloodCultistComponent cultist, EntityUid target)
    {
        if (!TryComp<DamageableComponent>(target, out var damage))
            return 0;

        var totalBlood = cultist.BloodCount;
        var initialBlood = totalBlood;
        var prioritizedDamageTypes = new[] { "Blunt", "Piercing", "Heat", "Slash", "Caustic" };

        var positiveDamage = _damage.GetPositiveDamage((target, damage));

        foreach (var damageType in prioritizedDamageTypes)
        {
            if (totalBlood <= 0)
                break;

            if (positiveDamage.DamageDict.TryGetValue(damageType, out var currentDamage) && currentDamage > 0)
            {
                var healAmount = FixedPoint2.Min(currentDamage, totalBlood);
                var healSpecifier = new DamageSpecifier { DamageDict = { { damageType, -healAmount } } };

                _damage.TryChangeDamage(target, healSpecifier, true);
                totalBlood -= healAmount.Int();
            }
        }

        cultist.BloodCount = totalBlood;
        return initialBlood - totalBlood;
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
        return absorbedBlood > 0;
    }

    private bool IsBloodPuddle(EntityUid puddle)
    {
        if (!TryComp<SolutionComponent>(puddle, out var solutionComp))
            return false;

        var solution = solutionComp.Solution;
        return solution.Contents.Any(r =>
            r.Reagent.Prototype == "Blood" ||
            _prototypeManager
                .EnumerateParents<ReagentPrototype>(r.Reagent.Prototype)
                .Any(parent => parent.ID == "Blood"));
    }

    private int ExtractBloodFromPuddle(EntityUid puddle)
    {
        if (!TryComp<SolutionComponent>(puddle, out var solutionComp))
            return 0;

        var solution = solutionComp.Solution;
        var absorbedBlood = 0;

        var bloodReagents = solution.Contents
            .Where(r =>
                r.Reagent.Prototype == "Blood" ||
                _prototypeManager
                    .EnumerateParents<ReagentPrototype>(r.Reagent.Prototype)
                    .Any(parent => parent.ID == "Blood"))
            .ToList();

        if (bloodReagents.Count == 0)
            return 0;

        foreach (var reagent in bloodReagents)
        {
            var reagentId = reagent.Reagent;
            var quantity = reagent.Quantity;

            var removed = solution.RemoveReagent(reagentId, quantity);
            absorbedBlood += removed.Int();
        }

        _solution.UpdateChemicals((puddle, solutionComp));

        if (absorbedBlood > 0)
            SpawnAtPosition("BloodCultFloorGlowEffect", Transform(puddle).Coordinates);

        if (solution.Volume <= FixedPoint2.Zero)
            QueueDel(puddle);

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

        if (!TryComp<BloodSpellSourceActionComponent>(args.Used.Value, out var source) ||
            source.Action is not { } action ||
            !Exists(action) ||
            !_charges.HasCharges(action, 1))
            return;

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

            SpawnAtPosition("BloodCultOutEffect", Transform(args.Target.Value).Coordinates);
            _transform.SetCoordinates(args.Target.Value, Transform(randomRune).Coordinates);
            SpawnAtPosition("BloodCultInEffect", Transform(randomRune).Coordinates);

            QueueDel(randomRune);

            ExtractBlood(cultist, -7, 5);
            ConsumeSpellAction(args.Used.Value, cultist);
            QueueDel(args.Used.Value);
        }
        else
        {
            _popup.PopupEntity(
                Loc.GetString("blood-cult-teleport-failed"),
                cultist,
                cultist,
                PopupType.SmallCaution);
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

    private void ConsumeSpellAction(EntityUid spell, EntityUid user)
    {
        if (!TryComp<BloodSpellSourceActionComponent>(spell, out var source)
            || source.Action is not { } action
            || !Exists(action)
            || !TryComp<BloodCultistComponent>(user, out var cultist))
            return;

        if (!_charges.TryUseCharge(action))
            return;

        _action.StartUseDelay(action);

        if (_charges.IsEmpty(action))
        {
            cultist.SelectedEmpoweringSpells.Remove(action);
            _action.RemoveAction(action);
        }
    }

    private bool TrySpawnSpellInHand(EntityUid uid, EntProtoId proto, EntityUid? action = null)
    {
        if (!TryComp<HandsComponent>(uid, out var hands))
            return false;

        var spell = SpawnAtPosition(proto, Transform(uid).Coordinates);
        var activeHand = _hands.GetActiveHand((uid, hands));

        if (_hands.TryPickupAnyHand(uid, spell) ||
            activeHand != null && _hands.TryForcePickup((uid, hands), spell, activeHand))
        {
            if (action != null)
            {
                var source = EnsureComp<BloodSpellSourceActionComponent>(spell);
                source.Action = action;
            }

            return true;
        }

        QueueDel(spell);
        return false;
    }
    #endregion
}
