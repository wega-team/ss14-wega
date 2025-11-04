using System.Linq;
using System.Threading.Tasks;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Bible.Components;
using Content.Server.GameTicking;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Pinpointer;
using Content.Server.Station.Components;
using Content.Shared.Administration.Systems;
using Content.Shared.Atmos.Components;
using Content.Shared.Blood.Cult;
using Content.Shared.Blood.Cult.Components;
using Content.Shared.Body.Components;
using Content.Shared.Chat;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Ghost;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.NullRod.Components;
using Content.Shared.Popups;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Standing;
using Content.Shared.Surgery.Components;
using Content.Shared.Timing;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Blood.Cult;

public sealed partial class BloodCultSystem
{
    [Dependency] private readonly BloodCultSystem _bloodCult = default!;
    [Dependency] private readonly IConsoleHost _consoleHost = default!;
    [Dependency] private readonly FlammableSystem _flammable = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly RejuvenateSystem _rejuvenate = default!;
    [Dependency] private readonly SharedGhostSystem _ghost = default!;

    private static readonly EntProtoId ActionComms = "ActionBloodCultComms";
    private const string BloodCultObserver = "MobObserverIfrit";
    private static int _offerings = 3;
    private bool _isRitualRuneUnlocked = false;

    private void InitializeRunes()
    {
        base.Initialize();

        SubscribeLocalEvent<RitualConductedEvent>(UnlockRitual);

        SubscribeNetworkEvent<RuneSelectEvent>(AfterRuneSelect);
        SubscribeLocalEvent<BloodCultistComponent, BloodRuneDoAfterEvent>(DoAfterRuneSelect);
        SubscribeLocalEvent<BloodDaggerComponent, UseInHandEvent>(OnDaggerInteract);
        SubscribeLocalEvent<BloodRuneComponent, InteractHandEvent>(OnRuneInteract);
        SubscribeLocalEvent<BloodRitualDimensionalRendingComponent, InteractHandEvent>(OnRitualInteract);
        SubscribeLocalEvent<BloodRitualDimensionalRendingComponent, ComponentShutdown>(OnComponentShutdown);

        SubscribeNetworkEvent<EmpoweringRuneMenuClosedEvent>(OnEmpoweringSelected);
        SubscribeLocalEvent<BloodCultistComponent, EmpoweringDoAfterEvent>(OnEmpoweringDoAfter);
        SubscribeNetworkEvent<SummoningSelectedEvent>(OnSummoningSelected);

        SubscribeLocalEvent<BloodRuneCleaningDoAfterEvent>(DoAfterInteractRune);
        SubscribeLocalEvent<BloodCultistComponent, BloodRuneCleaningDoAfterEvent>(DoAfterInteractRune);
    }

    #region Runes
    private void UnlockRitual(RitualConductedEvent ev)
    {
        _isRitualRuneUnlocked = true;
    }

    private void OnComponentShutdown(EntityUid uid, BloodRitualDimensionalRendingComponent component, ComponentShutdown args)
    {
        _isRitualRuneUnlocked = false;
    }

    private void AfterRuneSelect(RuneSelectEvent args, EntitySessionEventArgs eventArgs)
    {
        var uid = _entityManager.GetEntity(args.Uid);
        if (!HasComp<BloodCultistComponent>(uid) || IsInSpace(uid))
            return;

        var selectedRune = args.RuneProto;
        if (selectedRune == "BloodRuneRitualDimensionalRending" && !_isRitualRuneUnlocked)
        {
            _popup.PopupEntity(Loc.GetString("rune-ritual-failed"), uid, uid, PopupType.MediumCaution);
            return;
        }
        else if (selectedRune == "BloodRuneRitualDimensionalRending" && _isRitualRuneUnlocked)
        {
            var xform = Transform(uid);
            if (!TryComp<MapGridComponent>(xform.GridUid, out var grid) || !HasComp<BecomesStationComponent>(xform.GridUid.Value))
            {
                _popup.PopupEntity(Loc.GetString("rune-ritual-failed"), uid, uid, PopupType.MediumCaution);
                return;
            }

            bool isValidSurface = true;
            var cultistPosition = _transform.GetMapCoordinates(Transform(uid));
            if (!_mapMan.TryFindGridAt(cultistPosition, out _, out _))
                isValidSurface = false;

            if (isValidSurface)
            {
                var ritualRune = _entityManager.SpawnEntity(TrySelectRuneEffect(selectedRune), Transform(uid).Coordinates);
                _appearance.SetData(ritualRune, RuneColorVisuals.Color, TryFindColor(uid));

                _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, uid, TimeSpan.FromSeconds(9.75f),
                    new BloodRuneDoAfterEvent(selectedRune, GetNetEntity(ritualRune)), uid)
                {
                    BreakOnMove = true,
                    BreakOnDamage = true,
                    MovementThreshold = 0.01f,
                    NeedHand = false
                });
            }
            else
            {
                _popup.PopupEntity(Loc.GetString("rune-ritual-failed"), uid, uid, PopupType.MediumCaution);
            }
            return;
        }

        var rune = _entityManager.SpawnEntity(TrySelectRuneEffect(selectedRune), Transform(uid).Coordinates);
        _appearance.SetData(rune, RuneColorVisuals.Color, TryFindColor(uid));

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, uid, TimeSpan.FromSeconds(4f),
            new BloodRuneDoAfterEvent(selectedRune, GetNetEntity(rune)), uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 0.01f,
            NeedHand = false
        });
    }

    private void DoAfterRuneSelect(EntityUid cultist, BloodCultistComponent component, BloodRuneDoAfterEvent args)
    {
        if (args.Cancelled)
        {
            _entityManager.DeleteEntity(GetEntity(args.Rune));
            return;
        }

        var rune = _entityManager.SpawnEntity(args.SelectedRune, Transform(cultist).Coordinates);
        _appearance.SetData(rune, RuneColorVisuals.Color, TryFindColor(cultist));

        if (args.SelectedRune == "BloodRuneRitualDimensionalRending")
        {
            var xform = _entityManager.GetComponent<TransformComponent>(rune);
            var msg = Loc.GetString("blood-ritual-warning",
                ("location", FormattedMessage.RemoveMarkupOrThrow(_navMap.GetNearestBeaconString((rune, xform)))));
            _chat.DispatchGlobalAnnouncement(msg, colorOverride: Color.Red);

            _isRitualRuneUnlocked = false;
        }

        if (TryComp<BloodstreamComponent>(cultist, out var blood) && _blood.GetBloodLevelPercentage(cultist) > 0)
            _blood.TryModifyBloodLevel(cultist, -5);
        else
        {
            var damage = new DamageSpecifier { DamageDict = { { "Slash", 5 } } };
            _damage.TryChangeDamage(cultist, damage, true);
        }
        _popup.PopupEntity(Loc.GetString("rune-select-complete"), cultist, cultist, PopupType.SmallCaution);
        args.Handled = true;
    }

    private void OnRuneInteract(EntityUid rune, BloodRuneComponent component, InteractHandEvent args)
    {
        if (args.Handled || !HasComp<BloodCultistComponent>(args.User))
            return;

        if (rune is not { Valid: true } target)
            return;

        if (component.Prototype is null)
            return;

        OnRuneAfterInteract(target, component, args.User);
        args.Handled = true;
    }

    private void OnRitualInteract(EntityUid rune, BloodRitualDimensionalRendingComponent component, InteractHandEvent args)
    {
        if (args.Handled || !HasComp<BloodCultistComponent>(args.User))
            return;

        var currentTime = _gameTiming.RealTime;
        if (currentTime < component.ActivateTime)
        {
            var remainingTime = component.ActivateTime - currentTime;
            _popup.PopupEntity(Loc.GetString("ritual-activate-too-soon", ("time", remainingTime.TotalSeconds)), args.User, args.User, PopupType.LargeCaution);
            return;
        }

        if (rune is not { Valid: true } target || !CheckRitual(_transform.GetMapCoordinates(target), 9))
        {
            _popup.PopupEntity(Loc.GetString("ritual-activate-failed"), args.User, args.User, PopupType.LargeCaution);
            return;
        }

        component.ActivateTime = currentTime + TimeSpan.FromSeconds(120);
        component.Activate = true;

        OnRitualAfterInteract(target, component);
        var cultistEntities = _entityLookup.GetEntitiesInRange<BloodCultistComponent>(_transform.GetMapCoordinates(target), 6f);
        foreach (var cultistEntity in cultistEntities)
        {
            SendCultistMessage(cultistEntity.Owner, "ritual");
        }
        args.Handled = true;
    }

    private void OnRuneAfterInteract(EntityUid rune, BloodRuneComponent runeComp, EntityUid cultist)
    {
        var coords = _transform.GetMapCoordinates(rune);
        if (!TryComp<UseDelayComponent>(rune, out var useDelay) || _useDelay.IsDelayed((rune, useDelay)))
        {
            _popup.PopupEntity(Loc.GetString("rune-activate-failed"), cultist, cultist, PopupType.MediumCaution);
            return;
        }

        switch (runeComp.Prototype)
        {
            case "offering":
                var targets = _entityLookup.GetEntitiesInRange<HumanoidAppearanceComponent>(coords, 1f);
                foreach (var targetEntity in targets)
                {
                    var target = targetEntity.Owner;
                    if (HasComp<BloodCultistComponent>(target) || HasComp<BloodCultConstructComponent>(target)
                        || HasComp<NullRodOwnerComponent>(target))
                        continue;

                    if (!_entityManager.TryGetComponent<MobThresholdsComponent>(target, out var targetThresholds))
                        continue;

                    var currentState = targetThresholds.CurrentThresholdState;
                    if (currentState is MobState.Dead && (HasComp<MindShieldComponent>(target) || HasComp<BibleUserComponent>(target)
                        || HasComp<BloodCultObjectComponent>(target)) && !HasComp<SyntheticOperatedComponent>(target))
                    {
                        if (CheckRuneActivate(coords, 3))
                        {
                            var cultistEntities = _entityLookup.GetEntitiesInRange<BloodCultistComponent>(coords, 2f);
                            foreach (var cultistEntity in cultistEntities)
                            {
                                SendCultistMessage(cultistEntity.Owner, "offering");
                            }

                            var soulStone = _entityManager.SpawnEntity("BloodCultSoulStone", Transform(target).Coordinates);
                            if (TryComp<MindContainerComponent>(target, out var mindContainer) && mindContainer.Mind != null)
                                _mind.TransferTo(mindContainer.Mind.Value, soulStone);

                            // Gib
                            if (HasComp<BloodCultObjectComponent>(target))
                            {
                                _bloodCult.CheckTargetsConducted(target);
                                RemComp<BloodCultObjectComponent>(target);
                            }

                            var damage = new DamageSpecifier { DamageDict = { { "Blunt", 1000 } } };
                            _damage.TryChangeDamage(target, damage, true);
                            IncrementOfferingsCount();
                        }
                        else
                        {
                            _popup.PopupEntity(Loc.GetString("rune-activate-failed"), cultist, cultist, PopupType.MediumCaution);
                        }
                        break;
                    }
                    else if (currentState != MobState.Dead && !HasComp<MindShieldComponent>(target) && !HasComp<BibleUserComponent>(target)
                        && !HasComp<SyntheticOperatedComponent>(target))
                    {
                        if (CheckRuneActivate(coords, 2))
                        {
                            var cultistEntities = _entityLookup.GetEntitiesInRange<BloodCultistComponent>(coords, 2f);
                            foreach (var cultistEntity in cultistEntities)
                            {
                                SendCultistMessage(cultistEntity.Owner, "offering");
                            }

                            _rejuvenate.PerformRejuvenate(target);
                            EnsureComp<AutoCultistComponent>(target);
                        }
                        else
                        {
                            _popup.PopupEntity(Loc.GetString("rune-activate-failed"), cultist, cultist, PopupType.MediumCaution);
                        }
                        break;
                    }
                    else if (currentState is MobState.Dead && !HasComp<MindShieldComponent>(target) && !HasComp<BibleUserComponent>(target)
                        && !HasComp<SyntheticOperatedComponent>(target))
                    {
                        if (CheckRuneActivate(coords, 1))
                        {
                            var cultistEntities = _entityLookup.GetEntitiesInRange<BloodCultistComponent>(coords, 2f);
                            foreach (var cultistEntity in cultistEntities)
                            {
                                SendCultistMessage(cultistEntity.Owner, "offering");
                            }

                            var soulStone = _entityManager.SpawnEntity("BloodCultSoulStone", Transform(target).Coordinates);
                            if (TryComp<MindContainerComponent>(target, out var mindContainer) && mindContainer.Mind != null)
                                _mind.TransferTo(mindContainer.Mind.Value, soulStone);

                            // Gib
                            var damage = new DamageSpecifier { DamageDict = { { "Blunt", 1000 } } };
                            _damage.TryChangeDamage(target, damage, true);
                            IncrementOfferingsCount();
                        }
                        else
                        {
                            _popup.PopupEntity(Loc.GetString("rune-activate-failed"), cultist, cultist, PopupType.MediumCaution);
                        }
                        break;
                    }
                    else
                    {
                        _popup.PopupEntity(Loc.GetString("rune-activate-failed"), cultist, cultist, PopupType.MediumCaution);
                    }
                }
                break;
            case "teleport":
                var runes = new List<EntityUid>();
                var runeQuery = EntityQueryEnumerator<BloodRuneComponent>();
                while (runeQuery.MoveNext(out var runeUid, out var runeCompQ))
                {
                    if (runeCompQ.Prototype == "teleport" && runeUid != rune)
                        runes.Add(runeUid);
                }

                if (runes.Any() && CheckRuneActivate(coords, 1))
                {
                    var randomRuneEntity = runes[_random.Next(runes.Count)];
                    var runeTransform = _entityManager.GetComponent<TransformComponent>(randomRuneEntity);
                    var runeCoords = runeTransform.Coordinates;
                    SendCultistMessage(cultist, "teleport");

                    _entityManager.SpawnEntity("BloodCultOutEffect", Transform(cultist).Coordinates);
                    _transform.SetCoordinates(cultist, runeCoords);
                    _entityManager.SpawnEntity("BloodCultInEffect", runeCoords);
                    _entityManager.DeleteEntity(randomRuneEntity);
                }
                else
                {
                    _popup.PopupEntity(Loc.GetString("rune-activate-failed"), cultist, cultist, PopupType.MediumCaution);
                }
                break;
            case "empowering":
                if (CheckRuneActivate(coords, 1))
                {
                    if (TryComp<BloodCultistComponent>(cultist, out var comp) && comp.Empowering < 4)
                    {
                        SendCultistMessage(cultist, "empowering");

                        var netEntity = _entityManager.GetNetEntity(cultist);
                        RaiseNetworkEvent(new EmpoweringRuneMenuOpenedEvent(netEntity));
                    }
                    else
                    {
                        _popup.PopupEntity(Loc.GetString("rune-activate-failed"), cultist, cultist, PopupType.MediumCaution);
                    }
                }
                break;
            case "revive":
                if (CheckRuneActivate(coords, 1))
                {
                    var revivetarget = _entityLookup.GetEntitiesInRange<BodyComponent>(coords, 1f);
                    foreach (var targetEntity in revivetarget)
                    {
                        var target = targetEntity.Owner;
                        if (!_entityManager.TryGetComponent<MobThresholdsComponent>(target, out var targetThresholds) || target == cultist)
                            continue;

                        var currentState = targetThresholds.CurrentThresholdState;
                        if (HasComp<BloodCultistComponent>(target) && HasComp<HumanoidAppearanceComponent>(target)
                            && currentState is MobState.Dead)
                        {
                            if (GetOfferingsCount() >= 3)
                            {
                                SendCultistMessage(cultist, "revive");
                                _rejuvenate.PerformRejuvenate(target);
                                SubtractOfferingsCount();

                                if (TryComp<MindContainerComponent>(target, out var mind) && mind.Mind is null
                                    && !HasComp<GhostRoleComponent>(target))
                                {
                                    var formattedCommand = string.Format(
                                        "makeghostrole {0} {1} {2} {3}",
                                        target,
                                        Loc.GetString("ghost-role-information-cultist"),
                                        Loc.GetString("ghost-role-information-cultist-desc"),
                                        Loc.GetString("ghost-role-information-cultist-rules")
                                        );
                                    _consoleHost.ExecuteCommand(formattedCommand);
                                }
                            }
                            else
                            {
                                _popup.PopupEntity(Loc.GetString("rune-activate-failed"), cultist, cultist, PopupType.MediumCaution);
                            }
                            break;
                        }
                        else if (HasComp<BloodCultistComponent>(target) && HasComp<HumanoidAppearanceComponent>(target)
                            && TryComp<MindContainerComponent>(target, out var mind) && mind.Mind is null && !HasComp<GhostRoleComponent>(target))
                        {
                            SendCultistMessage(cultist, "revive");
                            var formattedCommand = string.Format(
                                "makeghostrole {0} {1} {2} {3}",
                                target,
                                Loc.GetString("ghost-role-information-cultist"),
                                Loc.GetString("ghost-role-information-cultist-desc"),
                                Loc.GetString("ghost-role-information-cultist-rules")
                                );
                            _consoleHost.ExecuteCommand(formattedCommand);
                        }
                        else if (HasComp<BodyComponent>(target) && !HasComp<BloodCultistComponent>(target) && currentState is MobState.Dead
                            && !HasComp<BorgChassisComponent>(target) && !HasComp<BloodCultObjectComponent>(target)
                            && !HasComp<HumanoidAppearanceComponent>(target)/*Stop killing humanoid this way*/)
                        {
                            SendCultistMessage(cultist, "revive");

                            // Gib
                            var damage = new DamageSpecifier { DamageDict = { { "Blunt", 1000 } } };
                            _damage.TryChangeDamage(target, damage, true);
                            IncrementOfferingsCount();
                            break;
                        }
                        else
                        {
                            _popup.PopupEntity(Loc.GetString("rune-activate-failed"), cultist, cultist, PopupType.MediumCaution);
                        }
                    }
                }
                else
                {
                    _popup.PopupEntity(Loc.GetString("rune-activate-failed"), cultist, cultist, PopupType.MediumCaution);
                }
                break;
            case "barrier":
                if (CheckRuneActivate(coords, 1))
                {
                    if (!runeComp.BarrierActive)
                    {
                        runeComp.BarrierActive = true;
                        SendCultistMessage(cultist, "barrier");
                        var nearbyRunes = _entityLookup.GetEntitiesInRange<BloodRuneComponent>(coords, 1f)
                            .Where(r => EntityManager.TryGetComponent(r, out BloodRuneComponent? nearbyRuneComp)
                                && nearbyRuneComp.Prototype == "barrier" && r.Owner != rune)
                            .ToList();

                        Entity<BloodRuneComponent>? randomRune = nearbyRunes.Any()
                            ? nearbyRunes[new Random().Next(nearbyRunes.Count)]
                            : null;

                        if (randomRune != null)
                        {
                            var randomRuneUid = randomRune.Value;
                            if (TryComp<BloodRuneComponent>(randomRuneUid, out var randomRuneComp) && !randomRuneComp.BarrierActive)
                            {
                                randomRuneComp.BarrierActive = true;
                                if (EntityManager.TryGetComponent(randomRuneUid, out PhysicsComponent? randomPhysicsComp))
                                {
                                    var fixture = _fixtures.GetFixtureOrNull(randomRuneUid, "barrier");
                                    if (fixture != null)
                                    {
                                        _physics.SetHard(randomRuneUid, fixture, randomRuneComp.BarrierActive);
                                    }
                                }
                            }
                        }

                        if (EntityManager.TryGetComponent(rune, out PhysicsComponent? physicsComp))
                        {
                            var fixture = _fixtures.GetFixtureOrNull(rune, "barrier");
                            if (fixture != null)
                            {
                                _physics.SetHard(rune, fixture, runeComp.BarrierActive);
                            }
                        }

                        var barrierRunes = new List<EntityUid>();
                        var barrierRuneQuery = EntityQueryEnumerator<BloodRuneComponent>();

                        while (barrierRuneQuery.MoveNext(out var runeUid, out var runeCompQ))
                        {
                            if (runeCompQ.Prototype == "barrier")
                                barrierRunes.Add(runeUid);
                        }

                        var damageFormula = 2 * barrierRunes.Count;
                        var damage = new DamageSpecifier { DamageDict = { { "Slash", damageFormula } } };
                        _damage.TryChangeDamage(cultist, damage, true);
                    }
                    else
                    {
                        runeComp.BarrierActive = false;
                        SendCultistMessage(cultist, "barrier");
                        if (EntityManager.TryGetComponent(rune, out PhysicsComponent? physicsComp))
                        {
                            var fixture = _fixtures.GetFixtureOrNull(rune, "barrier");
                            if (fixture != null)
                            {
                                _physics.SetHard(rune, fixture, runeComp.BarrierActive);
                            }
                        }
                    }
                }
                else
                {
                    _popup.PopupEntity(Loc.GetString("rune-activate-failed"), cultist, cultist, PopupType.MediumCaution);
                }
                break;
            case "summoning":
                if (CheckRuneActivate(coords, 2))
                {
                    var cultistEntities = _entityLookup.GetEntitiesInRange<BloodCultistComponent>(coords, 2f);
                    foreach (var cultistEntity in cultistEntities)
                    {
                        SendCultistMessage(cultist, "summoning");
                    }

                    var netEntity = _entityManager.GetNetEntity(cultist);
                    RaiseNetworkEvent(new SummoningRuneMenuOpenedEvent(netEntity));

                    _entityManager.DeleteEntity(rune);
                }
                else
                {
                    _popup.PopupEntity(Loc.GetString("rune-activate-failed"), cultist, cultist, PopupType.MediumCaution);
                }
                break;
            case "bloodboil":
                if (CheckRuneActivate(coords, 2))
                {
                    RemComp<BloodRuneComponent>(rune);
                    var cultistEntities = _entityLookup.GetEntitiesInRange<BloodCultistComponent>(coords, 2f);
                    foreach (var cultistEntity in cultistEntities)
                    {
                        SendCultistMessage(cultistEntity.Owner, "bloodboil");
                    }

                    Task.Run(async () =>
                    {
                        var damageValues = new[] { 5, 10, 10 };
                        for (int i = 0; i < 3; i++)
                        {
                            var targetsFlammable = _entityLookup.GetEntitiesInRange<FlammableComponent>(coords, 10f)
                                .Where(flammableEntity =>
                                    !HasComp<BloodCultistComponent>(flammableEntity.Owner))
                                .ToList();

                            foreach (var targetFlammable in targetsFlammable)
                            {
                                if (HasComp<NullRodOwnerComponent>(targetFlammable.Owner))
                                    continue;

                                if (TryComp<FlammableComponent>(targetFlammable.Owner, out var flammable))
                                {
                                    flammable.FireStacks = 3f;
                                    _flammable.Ignite(targetFlammable.Owner, rune);

                                    var damage = new DamageSpecifier { DamageDict = { { "Heat", damageValues[i] } } };
                                    _damage.TryChangeDamage(cultist, damage, false);
                                }
                            }

                            if (i < 2)
                            {
                                await Task.Delay(5000);
                            }
                        }

                        _entityManager.DeleteEntity(rune);
                    });
                }
                else
                {
                    _popup.PopupEntity(Loc.GetString("rune-activate-failed"), cultist, cultist, PopupType.MediumCaution);
                }
                break;
            case "spiritrealm":
                if (CheckRuneActivate(coords, 1))
                {
                    SendCultistMessage(cultist, "spiritrealm");
                    if (TryComp<MindContainerComponent>(cultist, out var mindContainer) && mindContainer.Mind != null)
                    {
                        var mindSystem = _entityManager.System<SharedMindSystem>();
                        var metaDataSystem = _entityManager.System<MetaDataSystem>();
                        var transformSystem = _entityManager.System<TransformSystem>();
                        var gameTicker = _entityManager.System<GameTicker>();

                        if (!mindSystem.TryGetMind(cultist, out var mindId, out var mind))
                            return;

                        if (mind.VisitingEntity != default && _entityManager.TryGetComponent<GhostComponent>(mind.VisitingEntity, out var oldGhostComponent))
                        {
                            mindSystem.UnVisit(mindId, mind);
                            if (oldGhostComponent.CanGhostInteract)
                                return;
                        }

                        var canReturn = mind.CurrentEntity != null
                            && !_entityManager.HasComponent<GhostComponent>(mind.CurrentEntity);
                        var ghost = _entityManager.SpawnEntity(BloodCultObserver, coords);
                        transformSystem.AttachToGridOrMap(ghost, _entityManager.GetComponent<TransformComponent>(ghost));

                        if (canReturn)
                        {
                            if (!string.IsNullOrWhiteSpace(mind.CharacterName))
                                metaDataSystem.SetEntityName(ghost, mind.CharacterName);

                            mindSystem.Visit(mindId, ghost, mind);
                        }
                        else
                        {
                            metaDataSystem.SetEntityName(ghost, Name(cultist));
                            mindSystem.TransferTo(mindId, ghost, mind: mind);
                        }

                        var comp = _entityManager.GetComponent<GhostComponent>(ghost);
                        _action.RemoveAction(ghost, comp.ToggleGhostBarActionEntity); // Ghost-Bar-Block
                        _action.AddAction(ghost, ActionComms);
                        _ghost.SetCanReturnToBody((ghost, comp), canReturn);
                        break;
                    }
                    else
                    {
                        _popup.PopupEntity(Loc.GetString("rune-activate-failed"), cultist, cultist, PopupType.MediumCaution);
                    }
                }
                break;
            default:
                _popup.PopupEntity(Loc.GetString("rune-activate-failed"), cultist, cultist, PopupType.MediumCaution);
                break;
        }

        if (_entityManager.EntityExists(rune))
            _useDelay.TryResetDelay((rune, useDelay));
    }

    private void OnRitualAfterInteract(EntityUid rune, BloodRitualDimensionalRendingComponent runeComp)
    {
        var xform = _entityManager.GetComponent<TransformComponent>(rune);
        var msg = Loc.GetString("blood-ritual-activate-warning",
            ("location", FormattedMessage.RemoveMarkupOrThrow(_navMap.GetNearestBeaconString((rune, xform)))));
        _chat.DispatchGlobalAnnouncement(msg, playSound: false, colorOverride: Color.Red);
        _audio.PlayGlobal(new SoundPathSpecifier("/Audio/_Wega/Ambience/Antag/bloodcult_scribe.ogg"), Filter.Broadcast(), true);
        Timer.Spawn(TimeSpan.FromSeconds(45), () =>
        {
            if (runeComp.Activate)
            {
                var coords = Transform(rune).Coordinates;
                _entityManager.DeleteEntity(rune);
                _entityManager.SpawnEntity("BloodCultDistortedEffect", coords);
                string currentGod = GetCurrentGod() switch
                {
                    "Narsie" => "MobNarsieSpawn",
                    "Reaper" => "MobReaperSpawn",
                    "Kharin" => "MobKharinSpawn",
                    _ => "MobNarsieSpawn"
                };
                _entityManager.SpawnEntity(currentGod, coords);
                RaiseLocalEvent(new GodCalledEvent());

                var nearbyCultists = _entityLookup.GetEntitiesInRange<BloodCultistComponent>(coords, 6f)
                    .ToList();

                foreach (var target in nearbyCultists)
                {
                    var harvester = _entityManager.SpawnEntity("MobConstructHarvester", Transform(target).Coordinates);
                    if (TryComp<MindContainerComponent>(target, out var mindContainer) && mindContainer.Mind != null)
                        _mind.TransferTo(mindContainer.Mind.Value, harvester);

                    var damage = new DamageSpecifier { DamageDict = { { "Blunt", 1000 } } };
                    _damage.TryChangeDamage(target, damage, true);
                }
            }
            else
            {
                var cultists = EntityQueryEnumerator<BloodCultistComponent>();
                while (cultists.MoveNext(out var cultist, out _))
                {
                    _popup.PopupEntity(Loc.GetString("ritual-failed"), cultist, cultist, PopupType.LargeCaution);
                }
            }
        });
    }
    #endregion

    #region Other
    private void OnEmpoweringSelected(EmpoweringRuneMenuClosedEvent args)
    {
        var cultist = _entityManager.GetEntity(args.Uid);
        if (!HasComp<BloodCultistComponent>(cultist))
            return;

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, cultist, TimeSpan.FromSeconds(4f), new EmpoweringDoAfterEvent(args.SelectedSpell), cultist)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 0.01f,
            NeedHand = true
        });
    }

    private void OnEmpoweringDoAfter(EntityUid cultist, BloodCultistComponent component, EmpoweringDoAfterEvent args)
    {
        if (args.Cancelled) return;

        var actionEntityUid = _action.AddAction(cultist, args.SelectedSpell);
        component.SelectedEmpoweringSpells.Add(actionEntityUid);
        component.Empowering++;

        if (TryComp<BloodstreamComponent>(cultist, out var blood) && _blood.GetBloodLevelPercentage(cultist) > 0)
            _blood.TryModifyBloodLevel(cultist, -5);
        else
        {
            var damage = new DamageSpecifier { DamageDict = { { "Slash", 2 } } };
            _damage.TryChangeDamage(cultist, damage, true);
        }
    }

    private void OnSummoningSelected(SummoningSelectedEvent args)
    {
        var user = _entityManager.GetEntity(args.User);
        var target = _entityManager.GetEntity(args.Target);
        _entityManager.SpawnEntity("BloodCultOutEffect", Transform(target).Coordinates);
        _transform.SetCoordinates(target, Transform(user).Coordinates);
        _entityManager.SpawnEntity("BloodCultInEffect", Transform(user).Coordinates);
    }

    private bool CheckRuneActivate(MapCoordinates coords, int needCount)
    {
        var constructsCount = _entityLookup.GetEntitiesInRange<BloodCultConstructComponent>(coords, 2f).Count();
        var aliveCultistsCount = _entityLookup.GetEntitiesInRange<BloodCultistComponent>(coords, 2f)
            .Count(cultist => !_entityManager.TryGetComponent<MobThresholdsComponent>(cultist.Owner, out var thresholds)
                            || thresholds.CurrentThresholdState != MobState.Dead);
        return aliveCultistsCount + constructsCount >= needCount;
    }

    private bool CheckRitual(MapCoordinates coords, int needCount)
    {
        var aliveCultistsCount = _entityLookup.GetEntitiesInRange<BloodCultistComponent>(coords, 6f)
            .Count(cultist => !_entityManager.TryGetComponent<MobThresholdsComponent>(cultist.Owner, out var thresholds)
                            || thresholds.CurrentThresholdState != MobState.Dead);
        return aliveCultistsCount >= needCount;
    }

    private void SendCultistMessage(EntityUid cultist, string messageType)
    {
        string message = messageType switch
        {
            "offering" => Loc.GetString("blood-cultist-offering-message"),
            "teleport" => Loc.GetString("blood-cultist-teleport-message"),
            "empowering" => Loc.GetString("blood-cultist-empowering-message"),
            "revive" => Loc.GetString("blood-cultist-revive-message"),
            "barrier" => Loc.GetString("blood-cultist-barrier-message"),
            "summoning" => Loc.GetString("blood-cultist-summoning-message"),
            "bloodboil" => Loc.GetString("blood-cultist-bloodboil-message"),
            "spiritrealm" => Loc.GetString("blood-cultist-spiritrealm-message"),
            "ritual" => Loc.GetString("blood-cultist-ritual-message"),
            _ => Loc.GetString("blood-cultist-default-message")
        };

        _chat.TrySendInGameICMessage(cultist, message, InGameICChatType.Whisper, ChatTransmitRange.Normal, checkRadioPrefix: false);
    }

    private string TrySelectRuneEffect(string messageType)
    {
        string message = messageType switch
        {
            "BloodRuneOffering" => "BloodRuneOfferingEffect",
            "BloodRuneTeleport" => "BloodRuneTeleportEffect",
            "BloodRuneEmpowering" => "BloodRuneEmpoweringEffect",
            "BloodRuneRevive" => "BloodRuneReviveEffect",
            "BloodRuneBarrier" => "BloodRuneBarrierEffect",
            "BloodRuneSummoning" => "BloodRuneSummoningEffect",
            "BloodRuneBloodBoil" => "BloodRuneBloodBoilEffect",
            "BloodRuneSpiritealm" => "BloodRuneSpiritealmEffect",
            "BloodRuneRitualDimensionalRending" => "BloodRuneRitualDimensionalRendingEffect",
            _ => "BloodRuneOfferingEffect"
        };
        return message;
    }

    private void OnDaggerInteract(Entity<BloodDaggerComponent> ent, ref UseInHandEvent args)
    {
        var user = args.User;
        if (!HasComp<BloodCultistComponent>(user))
        {
            var dropEvent = new DropHandItemsEvent();
            RaiseLocalEvent(user, ref dropEvent);
            var damage = new DamageSpecifier { DamageDict = { { "Slash", 5 } } };
            _damage.TryChangeDamage(user, damage, true);
            _popup.PopupEntity(Loc.GetString("blood-dagger-failed-interact"), user, user, PopupType.SmallCaution);
            return;
        }

        var netEntity = _entityManager.GetNetEntity(args.User);
        RaiseNetworkEvent(new RunesMenuOpenedEvent(netEntity));
        args.Handled = true;
    }

    private bool IsInSpace(EntityUid cultist)
    {
        var cultistPosition = _transform.GetMapCoordinates(Transform(cultist));
        if (!_mapMan.TryFindGridAt(cultistPosition, out _, out _))
            return true;

        return false;
    }

    private Color TryFindColor(EntityUid cultist)
    {
        Color bloodColor;
        if (TryComp<BloodstreamComponent>(cultist, out var bloodStreamComponent))
        {
            var bloodReagentPrototypeId = bloodStreamComponent.BloodReagent;
            if (_prototypeManager.TryIndex(bloodReagentPrototypeId, out ReagentPrototype? reagentPrototype))
            {
                bloodColor = reagentPrototype.SubstanceColor;
            }
            else
            {
                bloodColor = Color.White;
            }
        }
        else
        {
            bloodColor = Color.White;
        }
        return bloodColor;
    }

    private void DoAfterInteractRune(BloodRuneCleaningDoAfterEvent args)
    {
        if (args.Cancelled) return;

        _entityManager.DeleteEntity(args.Target);
    }

    private void DoAfterInteractRune(EntityUid cultist, BloodCultistComponent component, BloodRuneCleaningDoAfterEvent args)
    {
        if (args.Cancelled) return;

        _entityManager.DeleteEntity(args.Target);
    }

    private static void IncrementOfferingsCount()
    {
        _offerings++;
    }

    private static void SubtractOfferingsCount()
    {
        _offerings -= 3;
    }

    private static int GetOfferingsCount()
    {
        return _offerings;
    }
    #endregion
}
