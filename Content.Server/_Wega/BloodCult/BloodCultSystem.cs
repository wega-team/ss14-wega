using System.Linq;
using System.Numerics;
using Content.Server.Audio;
using Content.Server.Body.Systems;
using Content.Server.GameTicking.Rules;
using Content.Server.RoundEnd;
using Content.Shared.Actions;
using Content.Shared.Blood.Cult;
using Content.Shared.Blood.Cult.Components;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.EnergyShield;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Blood.Cult;

public sealed partial class BloodCultSystem : SharedBloodCultSystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly BloodCultRuleSystem _bloodCult = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly ServerGlobalSoundSystem _sound = default!;
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeRunes();
        InitializeBloodAbilities();

        SubscribeLocalEvent<BloodCultistEyesComponent, ExaminedEvent>(OnCultistEyesExamined);

        SubscribeLocalEvent<BloodCultistComponent, ShotAttemptedEvent>(OnShotAttempted); // Corvax-Wega-Testing
        SubscribeLocalEvent<BloodCultWeaponComponent, AttemptMeleeEvent>(OnAttemptMelee);
        SubscribeLocalEvent<BloodDaggerComponent, AfterInteractEvent>(OnInteract);

        SubscribeLocalEvent<StoneSoulComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<StoneSoulComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<StoneSoulComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<StoneSoulComponent, MindAddedMessage>(OnSoulStoneMindAdded);
        SubscribeLocalEvent<StoneSoulComponent, MindRemovedMessage>(OnSoulStoneMindRemoved);

        SubscribeLocalEvent<BloodShuttleCurseComponent, UseInHandEvent>(OnShuttleCurse);

        SubscribeLocalEvent<VeilShifterComponent, ExaminedEvent>(OnVeilShifterExamined);
        SubscribeLocalEvent<VeilShifterComponent, UseInHandEvent>(OnVeilShifter);

        SubscribeLocalEvent<BloodShieldActivaebleComponent, GotUnequippedEvent>(OnShieldGotUnequipped);

        SubscribeLocalEvent<ConstructComponent, InteractHandEvent>(OnConstructInteract);
        SubscribeLocalEvent<ConstructComponent, BloodConstructSelectMessage>(OnConstructSelect);

        SubscribeLocalEvent<BloodStructureComponent, MapInitEvent>(OnStructureMapInit);
        SubscribeLocalEvent<BloodStructureComponent, InteractHandEvent>(OnStructureInteract);
        SubscribeLocalEvent<BloodStructureComponent, BloodStructureSelectMessage>(OnStructureItemSelect);
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
                    .Where(cultist => !_mobState.IsDead(cultist))
                    .ToList();

                foreach (var target in nearbyCultists)
                {
                    var heal = new DamageSpecifier { DamageDict = { { "Blunt", -1 }, { "Slash", -1 } } };
                    _damage.TryChangeDamage(target.Owner, heal, true);

                    _blood.TryModifyBloodLevel(target.Owner, +1);
                }
            }
            pylonQueryComponent.NextTimeTick -= frameTime;
        }

        var ritualQuery = EntityQueryEnumerator<BloodRitualDimensionalRendingComponent>();
        while (ritualQuery.MoveNext(out var rune, out var ritualDimensional))
        {
            if (ritualDimensional.Activate)
            {
                if (ritualDimensional.NextTimeTick <= 0)
                {
                    ritualDimensional.NextTimeTick = 1;
                    if (!CheckRitual(_transform.GetMapCoordinates(rune), 9))
                        ritualDimensional.Activate = false;

                    if (!ritualDimensional.SoundPlayed && _gameTiming.CurTime > ritualDimensional.ActivateTime + TimeSpan.FromSeconds(30))
                    {
                        _sound.PlayGlobalOnStation(rune, _audio.ResolveSound(ritualDimensional.RitualMusic));
                        ritualDimensional.SoundPlayed = true;
                    }
                }
                ritualDimensional.NextTimeTick -= frameTime;
            }
        }
    }

    private void OnCultistEyesExamined(EntityUid uid, BloodCultistEyesComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var name = Identity.Name(uid, EntityManager, args.Examiner);
        if (Name(uid) == name)
            args.PushMarkup(Loc.GetString("blood-cultist-eyes-glow-examined", ("name", name)));
    }

    // Corvax-Wega-Testing-start
    // Да я пометил тегами чтобы банально не забыть про это и чо?
    private void OnShotAttempted(Entity<BloodCultistComponent> ent, ref ShotAttemptedEvent args)
    {
        if (HasComp<DeleteOnDropComponent>(args.Used))
            return;

        _popup.PopupEntity(Loc.GetString("gun-disabled"), ent, ent);
        args.Cancel();
    }
    // Corvax-Wega-Testing-end

    #region Dagger & Weapon
    private void OnAttemptMelee(Entity<BloodCultWeaponComponent> entity, ref AttemptMeleeEvent args)
    {
        var user = Transform(entity.Owner).ParentUid;
        if (!HasComp<BloodCultistComponent>(user))
        {
            _popup.PopupEntity(Loc.GetString("blood-cult-failed-attack"), user, user, PopupType.SmallCaution);

            var dropEvent = new DropHandItemsEvent();
            RaiseLocalEvent(user, ref dropEvent);
            args.Cancelled = true;
        }
    }

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
            args.Handled = true;
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
        if (!HasComp<BodyComponent>(args.Target) || !TryComp<BloodstreamComponent>(args.Target, out var bloodstream))
            return;

        var solution = bloodstream.BloodSolution;
        if (solution == null)
            return;

        var holywaterReagentId = new ReagentId("Holywater", new List<ReagentData>());
        var holywater = solution.Value.Comp.Solution.GetReagentQuantity(holywaterReagentId);

        if (holywater <= 0)
            return;

        solution.Value.Comp.Solution.RemoveReagent(holywaterReagentId, holywater);

        var unholywaterReagentId = new ReagentId("Unholywater", new List<ReagentData>());
        var unholywaterQuantity = new ReagentQuantity(unholywaterReagentId, holywater);

        args.Handled = _solution.TryAddReagent(solution.Value, unholywaterQuantity, out var addedQuantity) && addedQuantity > 0;
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
            QueueDel(args.Target);
            Spawn("Ash", Transform(user).Coordinates);
            _popup.PopupEntity(Loc.GetString("blood-sharpener-success"), user, user, PopupType.Small);
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("blood-sharpener-failed"), user, user, PopupType.Small);
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
        if (component.SoulEntity != null && Exists(component.SoulEntity.Value))
            QueueDel(component.SoulEntity.Value);
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
        _transform.AttachToGridOrMap(soul, Transform(soul));

        if (!string.IsNullOrWhiteSpace(mind.CharacterName))
            _meta.SetEntityName(soul, mind.CharacterName);

        _mind.Visit(mindId, soul, mind);
        component.SoulEntity = soul;
        component.IsSoulSummoned = true;

        _popup.PopupEntity(Loc.GetString("stone-soul-summoned"), user, user);
    }

    private void RetractSoul(EntityUid stone, StoneSoulComponent component, EntityUid user)
    {
        if (component.SoulEntity == null || !Exists(component.SoulEntity.Value))
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

        var cult = _bloodCult.GetActiveRule();
        if (cult != null && cult.Curses > 0)
        {
            _roundEndSystem.CancelRoundEndCountdown(user, true);
            QueueDel(entity);
            cult.Curses--;
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("blood-curse-failed"), user, user, PopupType.SmallCaution);
        }
        args.Handled = true;
    }
    #endregion

    #region Veil Shifter
    private void OnVeilShifterExamined(EntityUid uid, VeilShifterComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || !HasComp<BloodCultistComponent>(args.Examiner))
            return;

        args.PushMarkup(Loc.GetString("veil-shifter-examined", ("count", component.ActivationsCount)));
    }

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

            _appearance.SetData(uid, VeilShifterVisuals.Charged, component.ActivationsCount > 0);
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

    #region Shield
    private void OnShieldGotUnequipped(Entity<BloodShieldActivaebleComponent> ent, ref GotUnequippedEvent args)
    {
        if (!TryComp<EnergyShieldOwnerComponent>(args.Equipee, out var energyShield))
            return;

        QueueDel(energyShield.ShieldEntity);
        RemComp(args.Equipee, energyShield);
    }
    #endregion

    #region Construct
    private void OnConstructInteract(Entity<ConstructComponent> construct, ref InteractHandEvent args)
    {
        var user = args.User;
        if (args.Handled || !HasComp<BloodCultistComponent>(user))
            return;

        if (TryComp<ItemSlotsComponent>(construct, out var itemSlotsComponent))
        {
            EntityUid? item = itemSlotsComponent.Slots.First().Value.Item;

            if (item != null)
            {
                if (_mind.TryGetMind(item.Value, out _, out _))
                {
                    _ui.OpenUi(construct.Owner, BloodConstructUiKey.Key, user);
                }
                else
                {
                    _popup.PopupEntity(Loc.GetString("blood-construct-no-mind"), user, user, PopupType.SmallCaution);
                }
            }
            else
            {
                _popup.PopupEntity(Loc.GetString("blood-construct-failed"), user, user, PopupType.SmallCaution);
            }
        }
    }

    private void OnConstructSelect(Entity<ConstructComponent> construct, ref BloodConstructSelectMessage args)
    {
        EntityUid? mindUid = null;
        if (TryComp<ItemSlotsComponent>(construct, out var itemSlotsComponent))
            mindUid = itemSlotsComponent.Slots.First().Value.Item;

        if (mindUid == null || !_mind.TryGetMind(mindUid.Value, out var mind, out _))
        {
            _popup.PopupEntity(Loc.GetString("blood-construct-no-mind"), args.Actor, args.Actor, PopupType.SmallCaution);
            return;
        }

        var constructMobe = Spawn(args.Construct, Transform(construct).Coordinates);
        _mind.TransferTo(mind, constructMobe);
        QueueDel(construct);

        _popup.PopupEntity(Loc.GetString("blood-construct-succses"), args.Actor, args.Actor);
    }
    #endregion

    #region Structures
    private void OnStructureMapInit(EntityUid structure, BloodStructureComponent component, MapInitEvent args)
    {
        component.ActivateTime = _gameTiming.CurTime + TimeSpan.FromMinutes(4);
    }

    private void OnStructureInteract(EntityUid structure, BloodStructureComponent component, InteractHandEvent args)
    {
        var user = args.User;
        if (args.Handled || !HasComp<BloodCultistComponent>(user))
            return;

        var currentTime = _gameTiming.CurTime;
        var nextActivateTime = component.ActivateTime + TimeSpan.FromMinutes(4);
        if (currentTime < nextActivateTime)
        {
            var remainingTime = (nextActivateTime - currentTime).TotalSeconds;
            _popup.PopupEntity(Loc.GetString("blood-structure-failed", ("time", Math.Ceiling(remainingTime))), user, user, PopupType.Small);
            return;
        }

        _ui.OpenUi(structure, BloodStructureUiKey.Key, user);
        var state = new BloodStructureBoundUserInterfaceState(component.StructureGear);
        _ui.SetUiState(structure, BloodStructureUiKey.Key, state);
    }

    private void OnStructureItemSelect(Entity<BloodStructureComponent> structure, ref BloodStructureSelectMessage args)
    {
        var currentTime = _gameTiming.CurTime;
        var nextActivateTime = structure.Comp.ActivateTime + TimeSpan.FromMinutes(4);
        if (currentTime < nextActivateTime)
        {
            var remainingTime = (nextActivateTime - currentTime).TotalSeconds;
            _popup.PopupEntity(Loc.GetString("blood-structure-failed", ("time", Math.Ceiling(remainingTime))), args.Actor, args.Actor, PopupType.Small);
            return;
        }

        structure.Comp.ActivateTime = currentTime;

        var item = Spawn(args.Item, Transform(structure).Coordinates);
        _audio.PlayPvs(structure.Comp.Sound, structure);

        var cultistPosition = _transform.GetWorldPosition(args.Actor);
        var structurePosition = _transform.GetWorldPosition(structure);
        var distance = (structurePosition - cultistPosition).Length();
        if (distance < 3f) _hands.TryPickupAnyHand(args.Actor, item);
    }
    #endregion

    #region God Check
    private BloodCultGod GetCurrentGod()
    {
        var cult = _bloodCult.GetActiveRule();
        if (cult != null && cult.SelectedGod != null)
            return cult.SelectedGod.Value;

        return BloodCultGod.NarSi;
    }
    #endregion
}
