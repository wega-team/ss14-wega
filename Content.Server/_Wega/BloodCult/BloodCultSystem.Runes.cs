using System.Linq;
using System.Threading.Tasks;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Bible.Components;
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
using Content.Shared.Examine;
using Content.Shared.Ghost;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mindshield.Components;
using Content.Shared.NullRod.Components;
using Content.Shared.Popups;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Standing;
using Content.Shared.Surgery.Components;
using Content.Shared.Timing;
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
    [Dependency] private readonly FlammableSystem _flammable = default!;
    [Dependency] private readonly IConsoleHost _consoleHost = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly RejuvenateSystem _rejuvenate = default!;
    [Dependency] private readonly SharedGhostSystem _ghost = default!;

    private static readonly EntProtoId BloodCultObserver = "MobObserverIfrit";

    private void InitializeRunes()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodDaggerComponent, SelectBloodRuneMessage>(AfterRuneSelect);
        SubscribeLocalEvent<BloodCultistComponent, BloodRuneDoAfterEvent>(DoAfterRuneSelect);
        SubscribeLocalEvent<BloodDaggerComponent, UseInHandEvent>(OnDaggerInteract);
        SubscribeLocalEvent<BloodRuneComponent, InteractHandEvent>(OnRuneInteract);
        SubscribeLocalEvent<BloodRuneComponent, ExaminedEvent>(OnRuneExamined);
        SubscribeLocalEvent<BloodRitualDimensionalRendingComponent, InteractHandEvent>(OnRitualInteract);

        SubscribeLocalEvent<BloodRuneComponent, EmpoweringRuneSelectSpellMessage>(OnEmpoweringSelected);
        SubscribeLocalEvent<BloodCultistComponent, EmpoweringDoAfterEvent>(OnEmpoweringDoAfter);
        SubscribeLocalEvent<BloodRuneComponent, SummoningRuneSelectCultistMessage>(OnSummoningSelected);

        SubscribeLocalEvent<BloodRuneCleaningDoAfterEvent>(DoAfterInteractRune);
        SubscribeLocalEvent<BloodCultistComponent, BloodRuneCleaningDoAfterEvent>(DoAfterInteractRune);
    }

    #region Runes

    private void AfterRuneSelect(Entity<BloodDaggerComponent> rune, ref SelectBloodRuneMessage args)
    {
        if (!HasComp<BloodCultistComponent>(args.Actor) || IsInSpace(args.Actor))
            return;

        var selectedRune = args.RuneProtoId;
        if (!ValidateRuneSelection(args.Actor, selectedRune, out _))
            return;

        var effectRune = SpawnRuneEffect(args.Actor, selectedRune);

        StartRuneCreationDoAfter(args.Actor, selectedRune, effectRune,
            selectedRune == "BloodRuneRitualDimensionalRending" ? 9.75f : 4f);
    }

    private bool ValidateRuneSelection(EntityUid cultist, string selectedRune, out bool isValidSurface)
    {
        isValidSurface = true;
        var cult = _bloodCult.GetActiveRule();
        if (cult != null && selectedRune == "BloodRuneRitualDimensionalRending" && !cult.RitualStage)
        {
            _popup.PopupEntity(Loc.GetString("rune-ritual-failed"), cultist, cultist, PopupType.MediumCaution);
            return false;
        }

        if (cult != null && selectedRune == "BloodRuneRitualDimensionalRending" && cult.RitualStage)
        {
            var xform = Transform(cultist);
            if (!HasComp<MapGridComponent>(xform.GridUid) || !HasComp<BecomesStationComponent>(xform.GridUid))
            {
                _popup.PopupEntity(Loc.GetString("rune-ritual-failed"), cultist, cultist, PopupType.MediumCaution);
                return false;
            }

            var cultistPosition = _transform.GetMapCoordinates(Transform(cultist));
            isValidSurface = _mapMan.TryFindGridAt(cultistPosition, out _, out _);

            var ritual = EntityQuery<BloodRitualDimensionalRendingComponent>().FirstOrDefault();
            if (!isValidSurface || ritual != default)
            {
                _popup.PopupEntity(Loc.GetString("rune-ritual-failed"), cultist, cultist, PopupType.MediumCaution);
                return false;
            }
        }

        return true;
    }

    private EntityUid SpawnRuneEffect(EntityUid cultist, string runeProto)
    {
        var rune = Spawn(runeProto + "Effect", Transform(cultist).Coordinates);
        _appearance.SetData(rune, RuneColorVisuals.Color, TryFindColor(cultist));
        return rune;
    }

    private void StartRuneCreationDoAfter(EntityUid cultist, string selectedRune, EntityUid effectRune, float duration)
    {
        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, cultist, TimeSpan.FromSeconds(duration),
            new BloodRuneDoAfterEvent(selectedRune, GetNetEntity(effectRune)), cultist)
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
            QueueDel(GetEntity(args.Rune));
            return;
        }

        var rune = SpawnFinalRune(cultist, args.SelectedRune);
        if (args.SelectedRune == "BloodRuneRitualDimensionalRending")
            AnnounceRitualRune(rune);

        ExtractBloodCost(cultist, 5);
        _popup.PopupEntity(Loc.GetString("rune-select-complete"), cultist, cultist, PopupType.SmallCaution);
        args.Handled = true;
    }

    private EntityUid SpawnFinalRune(EntityUid cultist, string runeProto)
    {
        var rune = Spawn(runeProto, Transform(cultist).Coordinates);
        _appearance.SetData(rune, RuneColorVisuals.Color, TryFindColor(cultist));
        return rune;
    }

    private void AnnounceRitualRune(EntityUid rune)
    {
        var xform = Transform(rune);
        var msg = Loc.GetString("blood-ritual-warning",
            ("location", FormattedMessage.RemoveMarkupOrThrow(_navMap.GetNearestBeaconString((rune, xform)))));
        _chat.DispatchGlobalAnnouncement(msg, colorOverride: Color.Red);
    }

    private void OnRuneInteract(EntityUid rune, BloodRuneComponent component, InteractHandEvent args)
    {
        if (args.Handled || !HasComp<BloodCultistComponent>(args.User))
            return;

        if (rune is not { Valid: true } target)
            return;

        OnRuneAfterInteract(target, component, args.User);
        args.Handled = true;
    }

    private void OnRuneExamined(EntityUid uid, BloodRuneComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || !HasComp<BloodCultistComponent>(args.Examiner))
            return;

        args.PushMarkup(component.LocDesc);
    }

    private void OnRitualInteract(EntityUid rune, BloodRitualDimensionalRendingComponent component, InteractHandEvent args)
    {
        if (args.Handled || !HasComp<BloodCultistComponent>(args.User))
            return;

        if (!ValidateRitualActivation(rune, component, args.User))
            return;

        ActivateRitual(rune, component, args.User);
        args.Handled = true;
    }

    private bool ValidateRitualActivation(EntityUid rune, BloodRitualDimensionalRendingComponent component, EntityUid user)
    {
        if (component.Activate)
            return false;

        var currentTime = _gameTiming.CurTime;
        if (currentTime < component.ActivateTime + TimeSpan.FromSeconds(120))
        {
            var remainingTime = component.ActivateTime + TimeSpan.FromSeconds(120) - currentTime;
            _popup.PopupEntity(Loc.GetString("ritual-activate-too-soon",
                ("time", remainingTime.TotalSeconds)), user, user, PopupType.LargeCaution);
            return false;
        }

        if (rune is not { Valid: true } target || !CheckRitual(_transform.GetMapCoordinates(target), 9))
        {
            _popup.PopupEntity(Loc.GetString("ritual-activate-failed"), user, user, PopupType.LargeCaution);
            return false;
        }

        return true;
    }

    private void ActivateRitual(EntityUid rune, BloodRitualDimensionalRendingComponent component, EntityUid user)
    {
        component.ActivateTime = _gameTiming.CurTime;
        component.Activate = true;

        OnRitualAfterInteract(rune, component);

        var cultistEntities = _entityLookup.GetEntitiesInRange<BloodCultistComponent>(
            _transform.GetMapCoordinates(rune), 6f);

        foreach (var cultistEntity in cultistEntities)
        {
            SendCultistMessage(cultistEntity.Owner, BloodCultRune.Ritual);
        }
    }

    private void OnRuneAfterInteract(EntityUid rune, BloodRuneComponent runeComp, EntityUid cultist)
    {
        var coords = _transform.GetMapCoordinates(rune);
        if (!TryComp<UseDelayComponent>(rune, out var useDelay) || _useDelay.IsDelayed((rune, useDelay)))
        {
            _popup.PopupEntity(Loc.GetString("rune-activate-failed"), cultist, cultist, PopupType.MediumCaution);
            return;
        }

        HandleRuneActivation(rune, runeComp, cultist, coords);

        if (Exists(rune)) _useDelay.TryResetDelay((rune, useDelay));
    }

    private void HandleRuneActivation(EntityUid rune, BloodRuneComponent runeComp, EntityUid cultist, MapCoordinates coords)
    {
        switch (runeComp.RuneType)
        {
            case BloodCultRune.Offering:
                HandleOfferingRune(rune, runeComp, cultist, coords);
                break;
            case BloodCultRune.Teleport:
                HandleTeleportRune(rune, runeComp, cultist, coords);
                break;
            case BloodCultRune.Empowering:
                HandleEmpoweringRune(rune, runeComp, cultist, coords);
                break;
            case BloodCultRune.Revive:
                HandleReviveRune(rune, runeComp, cultist, coords);
                break;
            case BloodCultRune.Barrier:
                HandleBarrierRune(rune, runeComp, cultist, coords);
                break;
            case BloodCultRune.Summoning:
                HandleSummoningRune(rune, runeComp, cultist, coords);
                break;
            case BloodCultRune.Bloodboil:
                HandleBloodboilRune(rune, runeComp, cultist, coords);
                break;
            case BloodCultRune.Spiritrealm:
                HandleSpiritrealmRune(rune, runeComp, cultist, coords);
                break;
            default:
                _popup.PopupEntity(Loc.GetString("rune-activate-failed"), cultist, cultist, PopupType.MediumCaution);
                break;
        }
    }

    #region Rune Type Handlers

    private void HandleOfferingRune(EntityUid rune, BloodRuneComponent runeComp, EntityUid cultist, MapCoordinates coords)
    {
        var targets = _entityLookup.GetEntitiesInRange<HumanoidAppearanceComponent>(coords, 1f);
        foreach (var targetEntity in targets)
        {
            var target = targetEntity.Owner;
            if (HasComp<BloodCultistComponent>(target) || HasComp<BloodCultConstructComponent>(target) ||
                HasComp<NullRodOwnerComponent>(target))
                continue;

            if (_mobState.IsDead(target) && IsSpecialTarget(target) && !HasComp<SyntheticOperatedComponent>(target))
            {
                if (CheckRuneActivate(coords, 3))
                    HandleSpecialSacrifice(target, cultist, coords, runeComp);
                else
                    ShowActivationFailed(cultist);
                break;
            }
            else if (!_mobState.IsDead(target) && IsConvertibleTarget(target))
            {
                if (CheckRuneActivate(coords, 2))
                    ConvertToCultist(target, cultist, coords, runeComp);
                else
                    ShowActivationFailed(cultist);
                break;
            }
            else if (_mobState.IsDead(target) && IsRegularTarget(target))
            {
                if (CheckRuneActivate(coords, 1))
                    HandleRegularSacrifice(target, cultist, coords, runeComp);
                else
                    ShowActivationFailed(cultist);
                break;
            }
            else
            {
                ShowActivationFailed(cultist);
            }
        }
    }

    private bool IsSpecialTarget(EntityUid target)
    {
        return HasComp<MindShieldComponent>(target)
            || HasComp<BibleUserComponent>(target)
            || HasComp<BloodCultObjectComponent>(target);
    }

    private bool IsConvertibleTarget(EntityUid target)
    {
        return !HasComp<MindShieldComponent>(target)
            && !HasComp<BibleUserComponent>(target)
            && !HasComp<SyntheticOperatedComponent>(target);
    }

    private bool IsRegularTarget(EntityUid target)
    {
        return !HasComp<MindShieldComponent>(target)
            && !HasComp<BibleUserComponent>(target)
            && !HasComp<SyntheticOperatedComponent>(target);
    }

    private void HandleSpecialSacrifice(EntityUid target, EntityUid cultist, MapCoordinates coords, BloodRuneComponent runeComp)
    {
        SendRuneMessageToCultists(coords, 2f, runeComp.RuneType);

        var cult = _bloodCult.GetActiveRule();
        if (cult == null)
            return;

        cult.Offerings++;
        CreateSoulStone(target);
        if (HasComp<BloodCultObjectComponent>(target))
        {
            cult.SelectedTargets.Remove(target);
            RemComp<BloodCultObjectComponent>(target);
        }

        _body.GibBody(target);
    }

    private void ConvertToCultist(EntityUid target, EntityUid cultist, MapCoordinates coords, BloodRuneComponent runeComp)
    {
        SendRuneMessageToCultists(coords, 2f, runeComp.RuneType);
        _rejuvenate.PerformRejuvenate(target);
        EnsureComp<AutoCultistComponent>(target);
    }

    private void HandleRegularSacrifice(EntityUid target, EntityUid cultist, MapCoordinates coords, BloodRuneComponent runeComp)
    {
        SendRuneMessageToCultists(coords, 2f, runeComp.RuneType);

        CreateSoulStone(target);
        _body.GibBody(target);

        var cult = _bloodCult.GetActiveRule();
        if (cult != null) cult.Offerings++;
    }

    private void CreateSoulStone(EntityUid target)
    {
        var soulStone = Spawn("BloodCultSoulStone", Transform(target).Coordinates);
        if (TryComp<MindContainerComponent>(target, out var mindContainer) && mindContainer.Mind != null)
            _mind.TransferTo(mindContainer.Mind.Value, soulStone);
    }

    private void HandleTeleportRune(EntityUid rune, BloodRuneComponent runeComp, EntityUid cultist, MapCoordinates coords)
    {
        var teleportRunes = FindTeleportRunes(rune);

        if (teleportRunes.Any() && CheckRuneActivate(coords, 1))
        {
            TeleportToRandomRune(cultist, teleportRunes);
            SendCultistMessage(cultist, runeComp.RuneType);
        }
        else
        {
            ShowActivationFailed(cultist);
        }
    }

    private List<EntityUid> FindTeleportRunes(EntityUid excludeRune)
    {
        var runes = new List<EntityUid>();
        var runeQuery = EntityQueryEnumerator<BloodRuneComponent>();

        while (runeQuery.MoveNext(out var runeUid, out var runeCompQ))
        {
            if (runeCompQ.RuneType == BloodCultRune.Teleport && runeUid != excludeRune)
                runes.Add(runeUid);
        }

        return runes;
    }

    private void TeleportToRandomRune(EntityUid cultist, List<EntityUid> teleportRunes)
    {
        var randomRuneEntity = teleportRunes[_random.Next(teleportRunes.Count)];
        var runeCoords = Transform(randomRuneEntity).Coordinates;

        Spawn("BloodCultOutEffect", Transform(cultist).Coordinates);
        _transform.SetCoordinates(cultist, runeCoords);
        Spawn("BloodCultInEffect", runeCoords);
        QueueDel(randomRuneEntity);
    }

    private void HandleEmpoweringRune(EntityUid rune, BloodRuneComponent runeComp, EntityUid cultist, MapCoordinates coords)
    {
        if (!CheckRuneActivate(coords, 1))
            return;

        if (TryComp<BloodCultistComponent>(cultist, out var comp) && comp.SelectedEmpoweringSpells.Count < 4)
        {
            SendCultistMessage(cultist, runeComp.RuneType);
            OpenEmpoweringMenu(rune, cultist);
        }
        else
        {
            ShowActivationFailed(cultist);
        }
    }

    private void OpenEmpoweringMenu(EntityUid rune, EntityUid cultist)
    {
        _ui.OpenUi(rune, EmpoweringRuneUiKey.Key, cultist);
    }

    private void HandleReviveRune(EntityUid rune, BloodRuneComponent runeComp, EntityUid cultist, MapCoordinates coords)
    {
        if (!CheckRuneActivate(coords, 1))
        {
            ShowActivationFailed(cultist);
            return;
        }

        var reviveTargets = _entityLookup.GetEntitiesInRange<BodyComponent>(coords, 1f);

        foreach (var targetEntity in reviveTargets)
        {
            var target = targetEntity.Owner;
            if (target == cultist)
                continue;

            if (TryReviveDeadCultist(target, cultist, runeComp))
                break;

            if (TryCreateGhostRoleForCultist(target, cultist, runeComp))
                break;

            if (TrySacrificeNonHumanoidBody(target, cultist, runeComp))
                break;

            ShowActivationFailed(cultist);
        }
    }

    private bool TryReviveDeadCultist(EntityUid target, EntityUid cultist, BloodRuneComponent runeComp)
    {
        if (!HasComp<BloodCultistComponent>(target) || !HasComp<HumanoidAppearanceComponent>(target) ||
            _mobState.IsDead(target))
            return false;

        var cult = _bloodCult.GetActiveRule();
        if (cult == null || cult.Offerings < 3)
        {
            ShowActivationFailed(cultist);
            return true;
        }

        SendCultistMessage(cultist, runeComp.RuneType);
        _rejuvenate.PerformRejuvenate(target);
        cult.Offerings -= 3;

        if (TryComp<MindContainerComponent>(target, out var mind) && mind.Mind is null &&
            !HasComp<GhostRoleComponent>(target))
        {
            CreateGhostRole(target);
        }

        return true;
    }

    private bool TryCreateGhostRoleForCultist(EntityUid target, EntityUid cultist, BloodRuneComponent runeComp)
    {
        if (!HasComp<BloodCultistComponent>(target) || !HasComp<HumanoidAppearanceComponent>(target))
            return false;

        if (!TryComp<MindContainerComponent>(target, out var mind) || mind.Mind is not null ||
            HasComp<GhostRoleComponent>(target))
            return false;

        SendCultistMessage(cultist, runeComp.RuneType);
        CreateGhostRole(target);
        return true;
    }

    private bool TrySacrificeNonHumanoidBody(EntityUid target, EntityUid cultist, BloodRuneComponent runeComp)
    {
        if (!HasComp<BodyComponent>(target) || HasComp<BloodCultistComponent>(target) ||
            !_mobState.IsDead(target) || HasComp<BorgChassisComponent>(target) ||
            HasComp<BloodCultObjectComponent>(target) || HasComp<HumanoidAppearanceComponent>(target))
            return false;

        SendCultistMessage(cultist, runeComp.RuneType);
        _body.GibBody(target);

        var cult = _bloodCult.GetActiveRule();
        if (cult != null) cult.Offerings++;

        return true;
    }

    private void CreateGhostRole(EntityUid target)
    {
        var formattedCommand = string.Format(
            "makeghostrole {0} {1} {2} {3}",
            target,
            Loc.GetString("ghost-role-information-cultist"),
            Loc.GetString("ghost-role-information-cultist-desc"),
            Loc.GetString("ghost-role-information-cultist-rules"));
        _consoleHost.ExecuteCommand(formattedCommand);
    }

    private void HandleBarrierRune(EntityUid rune, BloodRuneComponent runeComp, EntityUid cultist, MapCoordinates coords)
    {
        if (!CheckRuneActivate(coords, 1))
        {
            ShowActivationFailed(cultist);
            return;
        }

        if (!runeComp.BarrierActive)
        {
            ActivateBarrier(rune, runeComp, cultist, coords);
        }
        else
        {
            DeactivateBarrier(rune, runeComp, cultist);
        }
    }

    private void ActivateBarrier(EntityUid rune, BloodRuneComponent runeComp, EntityUid cultist, MapCoordinates coords)
    {
        runeComp.BarrierActive = true;
        SendCultistMessage(cultist, runeComp.RuneType);

        ActivateNearbyBarrierRunes(coords, rune);
        SetBarrierPhysics(rune, true);

        ApplyBarrierDamage(cultist);
    }

    private void ActivateNearbyBarrierRunes(MapCoordinates coords, EntityUid excludeRune)
    {
        var nearbyRunes = _entityLookup.GetEntitiesInRange<BloodRuneComponent>(coords, 1f)
            .Where(r => TryComp(r, out BloodRuneComponent? nearbyRuneComp) &&
                    nearbyRuneComp.RuneType == BloodCultRune.Barrier && r.Owner != excludeRune)
            .ToList();

        if (!nearbyRunes.Any())
            return;

        var randomRune = nearbyRunes[new Random().Next(nearbyRunes.Count)];
        if (TryComp<BloodRuneComponent>(randomRune, out var randomRuneComp) && !randomRuneComp.BarrierActive)
        {
            randomRuneComp.BarrierActive = true;
            SetBarrierPhysics(randomRune, true);
        }
    }

    private void SetBarrierPhysics(EntityUid rune, bool active)
    {
        if (!TryComp(rune, out PhysicsComponent? physicsComp))
            return;

        var fixture = _fixtures.GetFixtureOrNull(rune, "barrier");
        if (fixture != null)
        {
            _physics.SetHard(rune, fixture, active);
        }
    }

    private void ApplyBarrierDamage(EntityUid cultist)
    {
        var barrierRunes = EntityQuery<BloodRuneComponent>()
            .Count(r => r.RuneType == BloodCultRune.Barrier);

        var damageFormula = 2 * barrierRunes;
        var damage = new DamageSpecifier { DamageDict = { { "Slash", damageFormula } } };
        _damage.TryChangeDamage(cultist, damage, true);
    }

    private void DeactivateBarrier(EntityUid rune, BloodRuneComponent runeComp, EntityUid cultist)
    {
        runeComp.BarrierActive = false;
        SendCultistMessage(cultist, runeComp.RuneType);
        SetBarrierPhysics(rune, false);
    }

    private void HandleSummoningRune(EntityUid rune, BloodRuneComponent runeComp, EntityUid cultist, MapCoordinates coords)
    {
        if (CheckRuneActivate(coords, 2))
        {
            SendRuneMessageToCultists(coords, 2f, runeComp.RuneType);
            OpenSummoningMenu(rune, cultist);
        }
        else
        {
            ShowActivationFailed(cultist);
        }
    }

    private void SendRuneMessageToCultists(MapCoordinates coords, float range, BloodCultRune runeType)
    {
        var cultistEntities = _entityLookup.GetEntitiesInRange<BloodCultistComponent>(coords, range);
        foreach (var cultistEntity in cultistEntities)
        {
            SendCultistMessage(cultistEntity.Owner, runeType);
        }
    }

    private void OpenSummoningMenu(EntityUid rune, EntityUid cultist)
    {
        _ui.OpenUi(rune, SummoningRuneUiKey.Key, cultist);
    }

    private void HandleBloodboilRune(EntityUid rune, BloodRuneComponent runeComp, EntityUid cultist, MapCoordinates coords)
    {
        if (CheckRuneActivate(coords, 2))
        {
            RemComp<BloodRuneComponent>(rune);
            SendRuneMessageToCultists(coords, 2f, runeComp.RuneType);
            StartBloodboilEffect(rune, coords, cultist);
        }
        else
        {
            ShowActivationFailed(cultist);
        }
    }

    private void StartBloodboilEffect(EntityUid rune, MapCoordinates coords, EntityUid cultist)
    {
        Task.Run(async () =>
        {
            var damageValues = new[] { 5, 10, 10 };
            for (int i = 0; i < 3; i++)
            {
                ApplyBloodboilDamage(coords, cultist, damageValues[i]);

                await Task.Delay(5000);
            }

            QueueDel(rune);
        });
    }

    private void ApplyBloodboilDamage(MapCoordinates coords, EntityUid cultist, int damageValue)
    {
        var targetsFlammable = _entityLookup.GetEntitiesInRange<FlammableComponent>(coords, 10f)
            .Where(flammableEntity => !HasComp<BloodCultistComponent>(flammableEntity.Owner)
                && HasComp<BloodstreamComponent>(flammableEntity.Owner))
            .ToList();

        foreach (var targetFlammable in targetsFlammable)
        {
            if (HasComp<NullRodOwnerComponent>(targetFlammable.Owner))
                continue;

            targetFlammable.Comp.FireStacks = 3f;
            _flammable.Ignite(targetFlammable.Owner, targetFlammable.Owner);

            var damage = new DamageSpecifier { DamageDict = { { "Heat", damageValue } } };
            _damage.TryChangeDamage(cultist, damage, false);
        }
    }

    private void HandleSpiritrealmRune(EntityUid rune, BloodRuneComponent runeComp, EntityUid cultist, MapCoordinates coords)
    {
        if (!CheckRuneActivate(coords, 1))
            return;

        SendCultistMessage(cultist, runeComp.RuneType);

        if (!TryComp<MindContainerComponent>(cultist, out var mindContainer) || mindContainer.Mind == null)
        {
            ShowActivationFailed(cultist);
            return;
        }

        EnterSpiritRealm(cultist, coords, mindContainer.Mind.Value);
    }

    private void EnterSpiritRealm(EntityUid cultist, MapCoordinates coords, EntityUid mindId)
    {
        if (!_mind.TryGetMind(cultist, out _, out var mind))
            return;

        CleanupExistingGhost(mindId, mind);

        var canReturn = mind.CurrentEntity != null && !HasComp<GhostComponent>(mind.CurrentEntity);
        var ghost = Spawn(BloodCultObserver, coords);

        _transform.AttachToGridOrMap(ghost, Transform(ghost));

        if (canReturn)
        {
            if (!string.IsNullOrWhiteSpace(mind.CharacterName))
                _meta.SetEntityName(ghost, mind.CharacterName);

            _mind.Visit(mindId, ghost, mind);
        }
        else
        {
            _meta.SetEntityName(ghost, Name(cultist));
            _mind.TransferTo(mindId, ghost, mind: mind);
        }

        if (TryComp<GhostComponent>(ghost, out var ghostComp))
            _action.RemoveAction(ghost, ghostComp.ToggleGhostBarActionEntity);
        _ghost.SetCanReturnToBody((ghost, ghostComp), canReturn);
    }

    private void CleanupExistingGhost(EntityUid mindId, MindComponent mind)
    {
        if (mind.VisitingEntity != default &&
            TryComp<GhostComponent>(mind.VisitingEntity, out var oldGhostComponent))
        {
            _mind.UnVisit(mindId, mind);
            if (oldGhostComponent.CanGhostInteract)
                return;
        }
    }

    private void ShowActivationFailed(EntityUid cultist)
    {
        _popup.PopupEntity(Loc.GetString("rune-activate-failed"), cultist, cultist, PopupType.MediumCaution);
    }

    #endregion

    private void OnRitualAfterInteract(EntityUid rune, BloodRitualDimensionalRendingComponent runeComp)
    {
        AnnounceRitualActivation(rune);

        _audio.PlayGlobal(new SoundPathSpecifier("/Audio/_Wega/Ambience/Antag/bloodcult_scribe.ogg"),
            Filter.Broadcast(), true);

        Timer.Spawn(TimeSpan.FromSeconds(90), () => CompleteRitual(rune, runeComp));
    }

    private void AnnounceRitualActivation(EntityUid rune)
    {
        var xform = Transform(rune);
        var msg = Loc.GetString("blood-ritual-activate-warning",
            ("location", FormattedMessage.RemoveMarkupOrThrow(_navMap.GetNearestBeaconString((rune, xform)))));
        _chat.DispatchGlobalAnnouncement(msg, playSound: false, colorOverride: Color.Red);
    }

    private void CompleteRitual(EntityUid rune, BloodRitualDimensionalRendingComponent runeComp)
    {
        if (!runeComp.Activate)
        {
            NotifyRitualFailed();
            return;
        }

        SpawnGodAndTransformCultists(rune);
    }

    private void NotifyRitualFailed()
    {
        var cultists = EntityQueryEnumerator<BloodCultistComponent>();
        while (cultists.MoveNext(out var cultist, out _))
        {
            _popup.PopupEntity(Loc.GetString("ritual-failed"), cultist, cultist, PopupType.LargeCaution);
        }
    }

    private void SpawnGodAndTransformCultists(EntityUid rune)
    {
        var coords = Transform(rune).Coordinates;

        QueueDel(rune);
        Spawn("BloodCultDistortedEffect", coords);

        Spawn(GetGodPrototype(), coords);

        RaiseLocalEvent(new GodCalledEvent());
        TransformNearbyCultists(coords);
    }

    private EntProtoId GetGodPrototype()
    {
        return GetCurrentGod() switch
        {
            BloodCultGod.NarSi => "MobNarsieSpawn",
            BloodCultGod.Reaper => "MobReaperSpawn",
            BloodCultGod.Kharin => "MobKharinSpawn",
            _ => "MobNarsieSpawn"
        };
    }

    private void TransformNearbyCultists(EntityCoordinates coords)
    {
        var nearbyCultists = _entityLookup.GetEntitiesInRange<BloodCultistComponent>(coords, 6f).ToList();

        foreach (var target in nearbyCultists)
        {
            var harvester = Spawn("MobConstructHarvester", Transform(target).Coordinates);
            if (TryComp<MindContainerComponent>(target, out var mindContainer) && mindContainer.Mind != null)
                _mind.TransferTo(mindContainer.Mind.Value, harvester);

            _body.GibBody(target);
        }
    }

    #endregion

    private void OnEmpoweringSelected(Entity<BloodRuneComponent> rune, ref EmpoweringRuneSelectSpellMessage args)
    {
        if (!HasComp<BloodCultistComponent>(args.Actor))
            return;

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.Actor, TimeSpan.FromSeconds(4f), new EmpoweringDoAfterEvent(args.Spell), args.Actor)
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

        ExtractBloodCost(cultist, 5);
    }

    private void OnSummoningSelected(Entity<BloodRuneComponent> rune, ref SummoningRuneSelectCultistMessage args)
    {
        var target = GetEntity(args.CultistUid);
        Spawn("BloodCultOutEffect", Transform(target).Coordinates);
        _transform.SetCoordinates(target, Transform(rune).Coordinates);
        Spawn("BloodCultInEffect", Transform(rune).Coordinates);

        QueueDel(rune);
    }

    private void ExtractBloodCost(EntityUid cultist, int amount)
    {
        if (HasComp<BloodstreamComponent>(cultist) && _blood.GetBloodLevel(cultist) > 0)
            _blood.TryModifyBloodLevel(cultist, -amount);
        else
        {
            var damage = new DamageSpecifier { DamageDict = { { "Slash", amount } } };
            _damage.TryChangeDamage(cultist, damage, true);
        }
    }

    private bool CheckRuneActivate(MapCoordinates coords, int needCount)
    {
        var constructsCount = _entityLookup.GetEntitiesInRange<BloodCultConstructComponent>(coords, 2f).Count();
        var aliveCultistsCount = _entityLookup.GetEntitiesInRange<BloodCultistComponent>(coords, 2f)
            .Count(cultist => !_mobState.IsDead(cultist));
        return aliveCultistsCount + constructsCount >= needCount;
    }

    private bool CheckRitual(MapCoordinates coords, int needCount)
    {
        var aliveCultistsCount = _entityLookup.GetEntitiesInRange<BloodCultistComponent>(coords, 6f)
            .Count(cultist => !_mobState.IsDead(cultist));
        return aliveCultistsCount >= needCount;
    }

    private void SendCultistMessage(EntityUid cultist, BloodCultRune type)
    {
        string message = type switch
        {
            BloodCultRune.Offering => Loc.GetString("blood-cultist-offering-message"),
            BloodCultRune.Teleport => Loc.GetString("blood-cultist-teleport-message"),
            BloodCultRune.Empowering => Loc.GetString("blood-cultist-empowering-message"),
            BloodCultRune.Revive => Loc.GetString("blood-cultist-revive-message"),
            BloodCultRune.Barrier => Loc.GetString("blood-cultist-barrier-message"),
            BloodCultRune.Summoning => Loc.GetString("blood-cultist-summoning-message"),
            BloodCultRune.Bloodboil => Loc.GetString("blood-cultist-bloodboil-message"),
            BloodCultRune.Spiritrealm => Loc.GetString("blood-cultist-spiritrealm-message"),
            BloodCultRune.Ritual => Loc.GetString("blood-cultist-ritual-message"),
            _ => Loc.GetString("blood-cultist-default-message")
        };

        _chat.TrySendInGameICMessage(cultist, message, InGameICChatType.Whisper, ChatTransmitRange.Normal, checkRadioPrefix: false);
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

        _ui.OpenUi(ent.Owner, BloodRunesUiKey.Key, user);

        var cult = _bloodCult.GetActiveRule();
        if (cult != null && cult.RitualStage)
        {
            var state = new BloodRitualBoundUserInterfaceState();
            _ui.SetUiState(ent.Owner, BloodRunesUiKey.Key, state);
        }

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
        if (!TryComp<BloodstreamComponent>(cultist, out var bloodStreamComponent))
            return Color.FromHex("#880000");

        string? bloodReagentPrototypeId = null;
        if (bloodStreamComponent.BloodReferenceSolution.Contents.Count > 0)
        {
            var reagentQuantity = bloodStreamComponent.BloodReferenceSolution.Contents[0];
            bloodReagentPrototypeId = reagentQuantity.Reagent.Prototype;
        }

        if (bloodReagentPrototypeId == null)
            return Color.FromHex("#880000");

        if (!_prototypeManager.TryIndex(bloodReagentPrototypeId, out ReagentPrototype? reagentPrototype))
            return Color.FromHex("#880000");

        return reagentPrototype.SubstanceColor;
    }

    private void DoAfterInteractRune(BloodRuneCleaningDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        QueueDel(args.Target);
    }

    private void DoAfterInteractRune(EntityUid cultist, BloodCultistComponent component, BloodRuneCleaningDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        QueueDel(args.Target);
    }
}
