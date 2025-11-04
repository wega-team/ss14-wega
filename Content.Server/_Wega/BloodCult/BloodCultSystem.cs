using System.Linq;
using System.Numerics;
using Content.Server.Bed.Cryostorage;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Prayer;
using Content.Server.RoundEnd;
using Content.Shared.Actions;
using Content.Shared.Blood.Cult;
using Content.Shared.Blood.Cult.Components;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Blood.Cult;

public sealed partial class BloodCultSystem : SharedBloodCultSystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly PrayerSystem _prayerSystem = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;

    private readonly List<EntityUid> _selectedTargets = new();
    private bool _firstTriggered = false;
    private bool _secondTriggered = false;
    private bool _conductedComplete = false;
    private bool _ritualStage = false;
    private int _curses = 2;

    public override void Initialize()
    {
        SubscribeLocalEvent<BloodCultRuleComponent, ComponentShutdown>(OnRuleShutdown);
        SubscribeLocalEvent<BloodCultistComponent, BloodCultObjectiveActionEvent>(OnCheckObjective);
        SubscribeLocalEvent<BloodCultistComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<BloodCultistComponent, ShotAttemptedEvent>(OnShotAttempted); // Corvax-Wega-Testing
        SubscribeLocalEvent<BloodCultConstructComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<BloodCultObjectComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<BloodCultObjectComponent, CryostorageEnterEvent>(OnCryostorageEnter);
        SubscribeLocalEvent<BloodDaggerComponent, AfterInteractEvent>(OnInteract);
        SubscribeLocalEvent<AttackAttemptEvent>(OnAttackAttempt);

        SubscribeLocalEvent<StoneSoulComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<StoneSoulComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<StoneSoulComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<StoneSoulComponent, MindAddedMessage>(OnSoulStoneMindAdded);
        SubscribeLocalEvent<StoneSoulComponent, MindRemovedMessage>(OnSoulStoneMindRemoved);

        SubscribeLocalEvent<BloodShuttleCurseComponent, UseInHandEvent>(OnShuttleCurse);

        SubscribeLocalEvent<VeilShifterComponent, UseInHandEvent>(OnVeilShifter);

        SubscribeLocalEvent<ConstructComponent, InteractHandEvent>(OnConstructInteract);
        SubscribeNetworkEvent<BloodConstructMenuClosedEvent>(OnConstructSelect);

        SubscribeLocalEvent<BloodStructureComponent, InteractHandEvent>(OnStructureInteract);
        SubscribeNetworkEvent<BloodStructureMenuClosedEvent>(OnStructureItemSelect);

        InitializeRunes();
        InitializeBloodAbilities();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var pylonQuery = EntityQueryEnumerator<BloodPylonComponent>();
        while (pylonQuery.MoveNext(out var pylon, out var pylonQueryComponent))
        {
            if (pylonQueryComponent.NextTimeTick <= 0)
            {
                pylonQueryComponent.NextTimeTick = 3;
                var nearbyCultists = _entityLookup.GetEntitiesInRange<BloodCultistComponent>(Transform(pylon).Coordinates, 11f)
                    .Where(cultist => !_entityManager.TryGetComponent<MobThresholdsComponent>(cultist.Owner, out var thresholds)
                        || thresholds.CurrentThresholdState != MobState.Dead)
                    .ToList();

                foreach (var target in nearbyCultists)
                {
                    var heal = new DamageSpecifier { DamageDict = { { "Blunt", -1 }, { "Slash", -1 } } };
                    _damage.TryChangeDamage(target, heal, true);

                    if (TryComp<BloodstreamComponent>(target, out var blood))
                        _blood.TryModifyBloodLevel(target.Owner, +1);
                }
            }
            pylonQueryComponent.NextTimeTick -= frameTime;
        }

        var ritualQuery = EntityQueryEnumerator<BloodRitualDimensionalRendingComponent>();
        while (ritualQuery.MoveNext(out var rune, out var ritualQueryComponent))
        {
            if (ritualQueryComponent.Activate)
            {
                if (!_ritualStage)
                {
                    _ritualStage = true;
                    CheckStage();
                }

                if (ritualQueryComponent.NextTimeTick <= 0)
                {
                    ritualQueryComponent.NextTimeTick = 1;
                    if (!CheckRitual(_transform.GetMapCoordinates(rune), 9))
                        ritualQueryComponent.Activate = false;
                }
                ritualQueryComponent.NextTimeTick -= frameTime;
            }
        }
    }

    // Corvax-Wega-Testing-start
    // Да я пометил тегами чтобы банально не забыть про это и чо?
    private void OnShotAttempted(Entity<BloodCultistComponent> ent, ref ShotAttemptedEvent args)
    {
        _popup.PopupEntity(Loc.GetString("gun-disabled"), ent, ent);
        args.Cancel();
    }
    // Corvax-Wega-Testing-end

    #region Stages Update
    private void OnRuleShutdown(EntityUid uid, BloodCultRuleComponent component, ComponentShutdown args)
    {
        _selectedTargets.Clear();
        _firstTriggered = false;
        _secondTriggered = false;
        _conductedComplete = false;
        _curses = 2;

        _offerings = 3;
        _isRitualRuneUnlocked = false;
    }

    public void SelectRandomTargets()
    {
        _selectedTargets.Clear();

        var candidates = new List<EntityUid>();
        var enumerator = EntityQueryEnumerator<MindShieldComponent>();
        while (enumerator.MoveNext(out var uid, out _))
        {
            candidates.Add(uid);
        }

        if (candidates.Count >= 2)
        {
            var selectedIndices = new HashSet<int>();
            while (selectedIndices.Count < 2)
            {
                var index = _random.Next(0, candidates.Count);
                selectedIndices.Add(index);
            }

            foreach (var index in selectedIndices)
            {
                var target = candidates[index];
                _selectedTargets.Add(target);
                EnsureComp<BloodCultObjectComponent>(target);
            }
            return;
        }

        _selectedTargets.AddRange(candidates);
        foreach (var target in candidates)
        {
            EnsureComp<BloodCultObjectComponent>(target);
        }

        var globalCandidates = new List<EntityUid>();
        var globalEnumerator = EntityQueryEnumerator<HumanoidAppearanceComponent, ActorComponent, MobStateComponent>();
        while (globalEnumerator.MoveNext(out var uid, out _, out _, out _))
        {
            if (_selectedTargets.Contains(uid) || HasComp<BloodCultistComponent>(uid))
            {
                continue;
            }
            globalCandidates.Add(uid);
        }

        while (_selectedTargets.Count < 2 && globalCandidates.Count > 0)
        {
            var index = _random.Next(0, globalCandidates.Count);
            var target = globalCandidates[index];
            _selectedTargets.Add(target);
            EnsureComp<BloodCultObjectComponent>(target);
            globalCandidates.RemoveAt(index);
        }
    }

    private EntityUid? FindNewRandomTarget(Entity<BloodCultObjectComponent> excludedEntity)
    {
        var candidates = new List<EntityUid>();
        var query = EntityQueryEnumerator<HumanoidAppearanceComponent, ActorComponent, MobStateComponent>();
        while (query.MoveNext(out var uid, out _, out _, out _))
        {
            if (uid == excludedEntity.Owner || HasComp<BloodCultistComponent>(uid)
                || HasComp<BloodCultObjectComponent>(uid))
            {
                continue;
            }
            candidates.Add(uid);
        }

        if (candidates.Count == 0)
            return null;

        var index = _random.Next(0, candidates.Count);
        return candidates[index];
    }

    private void CheckTargetsConducted(EntityUid eliminatedTarget)
    {
        if (_selectedTargets.Contains(eliminatedTarget))
            _selectedTargets.Remove(eliminatedTarget);

        if (_selectedTargets.Count == 0 || !_selectedTargets.Any(IsTargetValid))
        {
            _conductedComplete = true;
            RaiseLocalEvent(new RitualConductedEvent());
        }
    }

    private bool IsTargetValid(EntityUid target)
    {
        return _entityManager.EntityExists(target);
    }

    private void OnCheckObjective(EntityUid uid, BloodCultistComponent component, BloodCultObjectiveActionEvent args)
    {
        if (!TryComp<ActorComponent>(uid, out var playerActor))
            return;

        string msg;
        if (_selectedTargets.Count == 0 && !_conductedComplete || !_selectedTargets.Any(IsTargetValid) && !_conductedComplete)
        {
            msg = Loc.GetString("blood-cult-targets-no-select");
        }
        else if (_selectedTargets.Count == 0 && IsRitualConducted())
        {
            msg = Loc.GetString("blood-cult-ritual-completed-next-objective");
        }
        else if (IsGodCalled())
        {
            msg = Loc.GetString("blood-cult-objective-complete");
        }
        else
        {
            var targetNames = _selectedTargets
                .Where(IsTargetValid)
                .Select(target => Name(target))
                .ToList();

            if (targetNames.Count > 0)
            {
                msg = Loc.GetString("blood-cult-current-targets", ("targets", string.Join(", ", targetNames)));
            }
            else
            {
                msg = Loc.GetString("blood-cult-no-valid-targets");
            }
        }

        _prayerSystem.SendSubtleMessage(playerActor.PlayerSession, playerActor.PlayerSession, string.Empty, msg);
        args.Handled = true;
    }

    private bool IsRitualConducted()
    {
        var query = EntityManager.EntityQuery<BloodCultRuleComponent>();
        foreach (var cult in query)
        {
            var winConditions = cult.BloodCultWinCondition.ToList();
            if (winConditions.Contains(BloodCultWinType.RitualConducted))
                return true;
        }
        return false;
    }

    private bool IsGodCalled()
    {
        var query = EntityManager.EntityQuery<BloodCultRuleComponent>();
        foreach (var cult in query)
        {
            var winConditions = cult.BloodCultWinCondition.ToList();
            if (winConditions.Contains(BloodCultWinType.GodCalled))
                return true;
        }
        return false;
    }

    private void OnComponentStartup(Entity<BloodCultistComponent> entity, ref ComponentStartup args)
    {
        CheckStage();
    }

    private void OnComponentStartup(Entity<BloodCultConstructComponent> entity, ref ComponentStartup args)
    {
        CheckStage();
    }

    private void OnComponentShutdown(Entity<BloodCultObjectComponent> entity, ref ComponentShutdown args)
    {
        CheckStage();
    }

    private void OnCryostorageEnter(Entity<BloodCultObjectComponent> entity, ref CryostorageEnterEvent args)
    {
        if (!TryComp<BloodCultObjectComponent>(args.Uid, out var objectComponent))
            return;

        var newTarget = FindNewRandomTarget((args.Uid, objectComponent));
        if (newTarget != null)
        {
            _selectedTargets.Add(newTarget.Value);
            EnsureComp<BloodCultObjectComponent>(newTarget.Value);
        }

        _selectedTargets.Remove(args.Uid);
        RemComp<BloodCultObjectComponent>(args.Uid);
    }

    private void CheckStage()
    {
        var totalCultEntities = GetCultEntities();
        var playerCount = GetPlayerCount();

        // Second
        if (playerCount >= 100 && totalCultEntities >= playerCount * 0.1f || playerCount < 100 && totalCultEntities >= playerCount * 0.2f || _ritualStage)
        {
            foreach (var cultist in GetAllCultists())
            {
                if (!HasComp<CultistEyesComponent>(cultist))
                {
                    UpdateCultistEyes(cultist);
                    AddComp<CultistEyesComponent>(cultist);
                }
            }
            if (!_firstTriggered)
            {
                var actorFilter = Filter.Empty();
                var actorQuery = EntityQueryEnumerator<ActorComponent, BloodCultistComponent>();
                while (actorQuery.MoveNext(out var actorUid, out var actor, out _))
                {
                    if (actorUid != EntityUid.Invalid)
                    {
                        actorFilter.AddPlayer(actor.PlayerSession);
                        _popup.PopupEntity(Loc.GetString("blood-cult-first-warning"), actorUid, actorUid, PopupType.SmallCaution);
                    }
                }
                _audio.PlayGlobal(new SoundPathSpecifier("/Audio/_Wega/Ambience/Antag/bloodcult_eyes.ogg"), actorFilter, true);
                _firstTriggered = true;
            }
        }

        // Third
        if (playerCount >= 100 && totalCultEntities >= playerCount * 0.2f || playerCount < 100 && totalCultEntities >= playerCount * 0.3f || _ritualStage)
        {
            foreach (var cultist in GetAllCultists())
            {
                if (!HasComp<PentagramDisplayComponent>(cultist))
                {
                    AddComp<PentagramDisplayComponent>(cultist);
                }
            }
            if (!_secondTriggered)
            {
                var actorFilter = Filter.Empty();
                var actorQuery = EntityQueryEnumerator<ActorComponent, BloodCultistComponent>();
                while (actorQuery.MoveNext(out var actorUid, out var actor, out _))
                {
                    if (actorUid != EntityUid.Invalid)
                    {
                        actorFilter.AddPlayer(actor.PlayerSession);
                        _popup.PopupEntity(Loc.GetString("blood-cult-second-warning"), actorUid, actorUid, PopupType.SmallCaution);
                    }
                }
                _audio.PlayGlobal(new SoundPathSpecifier("/Audio/_Wega/Ambience/Antag/bloodcult_halos.ogg"), actorFilter, true);
                _secondTriggered = true;
            }
        }
    }

    private void UpdateCultistEyes(EntityUid cultist)
    {
        if (TryComp<HumanoidAppearanceComponent>(cultist, out var appearanceComponent))
        {
            appearanceComponent.EyeColor = Color.FromHex("#E22218FF");
            Dirty(cultist, appearanceComponent);
        }
    }

    private int GetCultEntities()
    {
        var totalCultists = GetAllCultists().Count;
        var totalConstructs = EntityQuery<BloodCultConstructComponent>().Count();
        return totalCultists + totalConstructs;
    }

    private int GetPlayerCount()
    {
        var players = AllEntityQuery<HumanoidAppearanceComponent, ActorComponent, MobStateComponent, TransformComponent>();
        int count = 0;
        while (players.MoveNext(out _, out _, out _, out _, out _))
        {
            count++;
        }
        return count;
    }

    private List<EntityUid> GetAllCultists()
    {
        var cultists = new List<EntityUid>();
        var enumerator = EntityQueryEnumerator<BloodCultistComponent>();
        while (enumerator.MoveNext(out var uid, out _))
        {
            cultists.Add(uid);
        }
        return cultists;
    }
    #endregion

    #region Dagger
    private void OnInteract(EntityUid uid, BloodDaggerComponent component, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { Valid: true } target)
            return;

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

        if (HasComp<BloodCultistComponent>(target))
        {
            HandleCultistInteraction(args);
            return;
        }

        if (HasComp<BloodRuneComponent>(target))
        {
            HandleRuneInteraction(args);
            return;
        }

        if (HasComp<BloodSharpenerComponent>(target))
        {
            HandleSharpenerInteraction(uid, component, args);
            return;
        }
    }

    private void HandleCultistInteraction(AfterInteractEvent args)
    {
        if (!TryComp<BodyComponent>(args.Target, out var bodyComponent))
            return;

        foreach (var organ in _body.GetBodyOrgans(args.Target.Value, bodyComponent))
        {
            if (!HasComp<MetabolizerComponent>(organ.Id)
                || !TryComp<StomachComponent>(organ.Id, out var stomachComponent) || stomachComponent.Solution == null
                || !TryComp<SolutionContainerManagerComponent>(stomachComponent.Solution.Value, out var solutionContainer)
                || !_solution.TryGetSolution((stomachComponent.Solution.Value, solutionContainer), null, out var solutionEntity, out var solution))
                continue;

            var holywaterReagentId = new ReagentId("Holywater", new List<ReagentData>());
            var holywater = solution.GetReagentQuantity(holywaterReagentId);

            if (holywater <= 0)
                continue;

            solution.RemoveReagent(holywaterReagentId, holywater);

            var unholywaterReagentId = new ReagentId("Unholywater", new List<ReagentData>());
            var unholywaterQuantity = new ReagentQuantity(unholywaterReagentId, holywater);
            if (solutionEntity != null && _solution.TryAddReagent(solutionEntity.Value, unholywaterQuantity, out var addedQuantity) && addedQuantity > 0)
                args.Handled = true;
        }
    }

    private void HandleRuneInteraction(AfterInteractEvent args)
    {
        var user = args.User;
        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, user, TimeSpan.FromSeconds(4f), new BloodRuneCleaningDoAfterEvent(), user, args.Target)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 0.01f,
            NeedHand = false
        });
    }

    private void HandleSharpenerInteraction(EntityUid dagger, BloodDaggerComponent component, AfterInteractEvent args)
    {
        var user = args.User;
        if (!TryComp<MeleeWeaponComponent>(dagger, out var meleeWeaponComponent))
            return;

        if (!component.IsSharpered)
        {
            if (meleeWeaponComponent.Damage.DamageDict.TryGetValue("Slash", out var currentSlashDamage))
                meleeWeaponComponent.Damage.DamageDict["Slash"] = currentSlashDamage + FixedPoint2.New(4);
            else
                meleeWeaponComponent.Damage.DamageDict["Slash"] = FixedPoint2.New(4);

            component.IsSharpered = true;
            _entityManager.DeleteEntity(args.Target);
            _entityManager.SpawnEntity("Ash", Transform(user).Coordinates);
            _popup.PopupEntity(Loc.GetString("blood-sharpener-success"), user, user, PopupType.Small);
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("blood-sharpener-failed"), user, user, PopupType.Small);
        }
    }

    private void OnAttackAttempt(AttackAttemptEvent args)
    {
        if (args.Weapon == null || !HasComp<BloodDaggerComponent>(args.Weapon))
            return;

        var user = args.Uid;
        if (!HasComp<BloodCultistComponent>(user))
        {
            _popup.PopupEntity(Loc.GetString("blood-cult-failed-attack"), user, user, PopupType.SmallCaution);
            args.Cancel();
        }
    }
    #endregion

    #region Soul Stone
    private void OnComponentInit(EntityUid uid, StoneSoulComponent component, ComponentInit args)
    {
        component.SoulContainer = _container.EnsureContainer<ContainerSlot>(uid, "SoulContainer");
    }

    private void OnShutdown(EntityUid uid, StoneSoulComponent component, ComponentShutdown args)
    {
        if (component.SoulEntity != null && _entityManager.EntityExists(component.SoulEntity.Value))
        {
            QueueDel(component.SoulEntity.Value);
        }
    }

    private void OnUseInHand(EntityUid uid, StoneSoulComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        var user = args.User;
        if (component.IsSoulSummoned)
        {
            RetractSoul(uid, component, user);
        }
        else
        {
            SummonSoul(uid, component, user);
        }

        args.Handled = true;
    }

    private void SummonSoul(EntityUid stone, StoneSoulComponent component, EntityUid user)
    {
        if (!TryComp<MindContainerComponent>(stone, out var mindContainer) || mindContainer.Mind == null)
        {
            _popup.PopupEntity(Loc.GetString("stone-soul-empty"), user, user);
            return;
        }

        var transformSystem = _entityManager.System<TransformSystem>();
        var metaDataSystem = _entityManager.System<MetaDataSystem>();

        if (!_mind.TryGetMind(stone, out var mindId, out var mind))
        {
            _popup.PopupEntity(Loc.GetString("stone-soul-empty"), user, user);
            return;
        }

        if (mind.VisitingEntity != default)
        {
            _popup.PopupEntity(Loc.GetString("stone-soul-already-summoned"), user, user);
            return;
        }

        var stoneTransform = Transform(stone).Coordinates;
        var soul = Spawn(component.SoulProto, stoneTransform);
        transformSystem.AttachToGridOrMap(soul, Transform(soul));

        if (!string.IsNullOrWhiteSpace(mind.CharacterName))
            metaDataSystem.SetEntityName(soul, mind.CharacterName);

        _mind.Visit(mindId, soul, mind);
        component.SoulEntity = soul;
        component.IsSoulSummoned = true;

        _popup.PopupEntity(Loc.GetString("stone-soul-summoned"), user, user);
    }

    private void RetractSoul(EntityUid stone, StoneSoulComponent component, EntityUid user)
    {
        if (component.SoulEntity == null || !_entityManager.EntityExists(component.SoulEntity.Value))
        {
            _popup.PopupEntity(Loc.GetString("stone-soul-empty"), user, user);
            return;
        }

        if (!_mind.TryGetMind(component.SoulEntity.Value, out var mindId, out var mind))
        {
            _popup.PopupEntity(Loc.GetString("stone-soul-empty"), user, user);
            return;
        }

        _mind.UnVisit(mindId, mind);
        QueueDel(component.SoulEntity.Value);
        component.SoulEntity = null;
        component.IsSoulSummoned = false;

        _popup.PopupEntity(Loc.GetString("stone-soul-retracted"), user);
    }

    private void OnSoulStoneMindAdded(Entity<StoneSoulComponent> entity, ref MindAddedMessage args)
    {
        _appearance.SetData(entity, StoneSoulVisuals.HasSoul, true);
    }

    private void OnSoulStoneMindRemoved(Entity<StoneSoulComponent> entity, ref MindRemovedMessage args)
    {
        _appearance.SetData(entity, StoneSoulVisuals.HasSoul, false);
    }
    #endregion

    #region ShuttleCurse
    private void OnShuttleCurse(Entity<BloodShuttleCurseComponent> entity, ref UseInHandEvent args)
    {
        var user = args.User;
        if (args.Handled || !HasComp<BloodCultistComponent>(user))
            return;

        if (_curses > 0)
        {
            _roundEndSystem.CancelRoundEndCountdown(user);
            _entityManager.DeleteEntity(entity);
            _curses--;
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("blood-curse-failed"), user, user, PopupType.SmallCaution);
        }
        args.Handled = true;
    }
    #endregion

    #region Veil Shifter
    private void OnVeilShifter(EntityUid uid, VeilShifterComponent component, UseInHandEvent args)
    {
        var user = args.User;
        if (args.Handled || !HasComp<BloodCultistComponent>(user))
        {
            var dropEvent = new DropHandItemsEvent();
            RaiseLocalEvent(user, ref dropEvent);
            return;
        }

        if (component.ActivationsCount > 0)
        {
            component.ActivationsCount--;
            var alignedDirection = GetAlignedDirection(user);
            var randomDistance = _random.NextFloat(1f, 9f);

            var transform = Transform(user);
            var targetPosition = transform.Coordinates.Offset(alignedDirection * randomDistance);
            _transform.SetCoordinates(user, targetPosition);
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("blood-veil-shifter-failed"), user, user, PopupType.SmallCaution);
        }
        args.Handled = true;
    }

    private Vector2 GetAlignedDirection(EntityUid uid)
    {
        var transform = Transform(uid);
        var direction = transform.LocalRotation.ToWorldVec().Normalized();
        if (Math.Abs(direction.X) > Math.Abs(direction.Y))
        {
            return direction.X > 0 ? Vector2.UnitX : -Vector2.UnitX;
        }
        else
        {
            return direction.Y > 0 ? Vector2.UnitY : -Vector2.UnitY;
        }
    }
    #endregion

    #region Construct
    private void OnConstructInteract(Entity<ConstructComponent> cosntruct, ref InteractHandEvent args)
    {
        var user = args.User;
        if (args.Handled || !HasComp<BloodCultistComponent>(user))
            return;

        if (TryComp<ItemSlotsComponent>(cosntruct, out var itemSlotsComponent))
        {
            foreach (var slot in itemSlotsComponent.Slots.Values)
            {
                if (slot.HasItem)
                {
                    var containedEntity = slot.Item;
                    if (containedEntity != null)
                    {
                        if (TryComp<MindContainerComponent>(containedEntity.Value, out var mindContainer) && mindContainer.Mind != null)
                        {
                            var netEntity = _entityManager.GetNetEntity(user);
                            var netCosntruct = _entityManager.GetNetEntity(cosntruct);
                            var mind = _entityManager.GetNetEntity(mindContainer.Mind.Value);
                            RaiseNetworkEvent(new OpenConstructMenuEvent(netEntity, netCosntruct, mind));
                        }
                        else
                        {
                            _popup.PopupEntity(Loc.GetString("blood-construct-no-mind"), user, user, PopupType.SmallCaution);
                        }
                    }
                }
                else
                {
                    _popup.PopupEntity(Loc.GetString("blood-construct-failed"), user, user, PopupType.SmallCaution);
                }
            }
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("blood-construct-failed"), user, user, PopupType.SmallCaution);
        }
    }

    private void OnConstructSelect(BloodConstructMenuClosedEvent args)
    {
        var user = _entityManager.GetEntity(args.Uid);
        var construct = _entityManager.GetEntity(args.ConstructUid);
        var mind = _entityManager.GetEntity(args.Mind);

        if (mind == EntityUid.Invalid)
        {
            _popup.PopupEntity(Loc.GetString("blood-construct-no-mind"), user, user, PopupType.SmallCaution);
            return;
        }

        var constructMobe = _entityManager.SpawnEntity(args.ConstructProto, Transform(construct).Coordinates);
        _mind.TransferTo(mind, constructMobe);
        _entityManager.DeleteEntity(construct);

        _popup.PopupEntity(Loc.GetString("blood-construct-succses"), user, user);
    }
    #endregion

    #region Structures
    private void OnStructureInteract(EntityUid structure, BloodStructureComponent component, InteractHandEvent args)
    {
        var user = args.User;
        if (args.Handled || !HasComp<BloodCultistComponent>(user))
            return;

        if (structure is not { Valid: true } target || !component.CanInteract)
            return;

        var currentTime = _gameTiming.RealTime;
        if (currentTime < component.ActivateTime)
        {
            var remainingTime = (component.ActivateTime - currentTime).TotalSeconds;
            _popup.PopupEntity(Loc.GetString("blood-structure-failed", ("time", Math.Ceiling(remainingTime))), user, user, PopupType.Small);
            return;
        }

        var netEntity = _entityManager.GetNetEntity(user);
        var netStructureEntity = _entityManager.GetNetEntity(target);
        RaiseNetworkEvent(new OpenStructureMenuEvent(netEntity, netStructureEntity));
    }

    private void OnStructureItemSelect(BloodStructureMenuClosedEvent args)
    {
        var user = _entityManager.GetEntity(args.Uid);
        var structure = _entityManager.GetEntity(args.Structure);
        if (!TryComp<BloodStructureComponent>(structure, out var structureComp))
            return;

        var currentTime = _gameTiming.RealTime;
        if (currentTime < structureComp.ActivateTime)
        {
            var remainingTime = (structureComp.ActivateTime - currentTime).TotalSeconds;
            _popup.PopupEntity(Loc.GetString("blood-structure-failed", ("time", Math.Ceiling(remainingTime))), user, user, PopupType.Small);
            return;
        }

        structureComp.ActivateTime = currentTime + TimeSpan.FromMinutes(4);

        var item = _entityManager.SpawnEntity(args.Item, Transform(structure).Coordinates);
        _audio.PlayPvs(structureComp.Sound, structure);

        var cultistPosition = _transform.GetWorldPosition(user);
        var structurePosition = _transform.GetWorldPosition(structure);
        var distance = (structurePosition - cultistPosition).Length();
        if (distance < 3f)
            _hands.TryPickupAnyHand(user, item);
    }
    #endregion

    #region God Check
    private string GetCurrentGod()
    {
        var query = EntityQueryEnumerator<BloodCultRuleComponent>();
        while (query.MoveNext(out var cult))
        {
            if (cult.SelectedGod == null)
            {
                return "Narsie";
            }
            return cult.SelectedGod;
        }
        return "Narsie";
    }
    #endregion
}
