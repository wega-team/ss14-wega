using System.Linq;
using Content.Server.Audio;
using Content.Server.GameTicking.Rules;
using Content.Server.Pinpointer;
using Content.Server.Bible.Components;
using Content.Shared.Veil.Cult;
using Content.Shared.Veil.Cult.UI;
using Content.Shared.Veil.Cult.Components;
using Content.Shared.Blood.Cult.Components;
using Content.Shared.Body.Components;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Power.Components;
using Content.Shared.Humanoid;
using Content.Shared.Administration.Systems;
using Content.Shared.Construction.Components;
using Content.Shared.Mindshield.Components;
using Content.Shared.NullRod.Components;
using Content.Shared.Lathe;
using Content.Shared.Mobs;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Spawners;
using Robust.Shared.Utility;


namespace Content.Server.Veil.Cult;

public sealed partial class VeilCultSystem : SharedVeilCultSystem
{
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private VeilCultRuleSystem _veilCult = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private ServerGlobalSoundSystem _sound = default!;
    [Dependency] private SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private RejuvenateSystem _rejuvenate = default!;
    [Dependency] private NavMapSystem _navMap = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeVeilAbilities();
        InitializeEnchantments();

        SubscribeLocalEvent<VeilCultistComponent, ShotAttemptedEvent>(OnShotAttempted);
        SubscribeLocalEvent<VeilCultistHandsComponent, ExaminedEvent>(OnCultistHandsExamined);
        SubscribeLocalEvent<VeilCultBeaconComponent, ComponentInit>(OnInit);

        SubscribeLocalEvent<VeilCultAltarComponent, VeilAltarSelectEnergyMessage>(OnSelectEnergy);
        SubscribeLocalEvent<VeilCultAltarComponent, VeilAltarSelectOfferMessage>(OnSelectOffer);
        SubscribeLocalEvent<VeilCultAltarComponent, ActivateInWorldEvent>(UseVeilAltar);
        SubscribeLocalEvent<VeilCultLatheComponent, ActivateInWorldEvent>(UseVeilLathe);

        SubscribeLocalEvent<VeilCultBeaconComponent, AnchorAttemptEvent>(OnAnchor);

        SubscribeLocalEvent<VeilCultistComponent, StrangeShardDoAfterEvent>(DoAfterInteractShardCultist);
        SubscribeLocalEvent<VeilCultAltarComponent, StrangeShardDoAfterEvent>(DoAfterInteractShardAltar);
        SubscribeLocalEvent<VeilCultConstructComponent, MobStateChangedEvent>(OnConstructMobStateChange);
        SubscribeLocalEvent<SoulVesselComponent, AfterInteractEvent>(OnSoulVesselInserted);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var beaconQuery = EntityQueryEnumerator<VeilCultBeaconComponent, TransformComponent>();
        while (beaconQuery.MoveNext(out var beacon, out var beaconComponent, out var transform))
        {
            if (beaconComponent.NextTimeTick <= 0 && transform.Anchored)
            {
                beaconComponent.NextTimeTick = 5;
                var nearbyCultists = _entityLookup.GetEntitiesInRange<VeilCultistComponent>(transform.Coordinates, 11f)
                    .Where(cultist => !_mobState.IsDead(cultist))
                    .ToList();

                var nearbyConstruct = _entityLookup.GetEntitiesInRange<VeilCultConstructComponent>(transform.Coordinates, 11f)
                    .Where(cultist => !_mobState.IsDead(cultist))
                    .ToList();

                foreach (var target in nearbyCultists)
                {
                    var heal = new DamageSpecifier { DamageDict = { { "Blunt", -5 }, { "Slash", -5 }, { "Piercing", -10 }, { "Heat", -10 } } };
                    _damage.TryChangeDamage(target.Owner, heal, true);

                    _blood.TryModifyBloodLevel(target.Owner, +5);
                    if (TryComp<BloodstreamComponent>(target.Owner, out var bloodstream))
                        _blood.TryModifyBleedAmount((target.Owner, bloodstream), -3f);
                }

                foreach (var target in nearbyConstruct)
                {
                    var heal = new DamageSpecifier { DamageDict = { { "Blunt", -5 }, { "Slash", -5 }, { "Piercing", -10 }, { "Heat", -10 } } };
                    _damage.TryChangeDamage(target.Owner, heal, true);

                    _blood.TryModifyBloodLevel(target.Owner, +5);
                    if (TryComp<BloodstreamComponent>(target.Owner, out var bloodstream))
                        _blood.TryModifyBleedAmount((target.Owner, bloodstream), -3f);

                    if (TryComp<TimedDespawnComponent>(target, out var despawn))
                        despawn.Lifetime += 25;
                }

                var cult = _veilCult.GetActiveRule();
                if (cult != null && cult.Station != null && Transform(beacon).GridUid == cult.Station.Value)
                {
                    cult.EnergyCount += 25;
                }
            }

            beaconComponent.NextTimeTick -= frameTime;
        }

        var cogQuery = EntityQueryEnumerator<InteractionCogInfectedComponent>();
        while (cogQuery.MoveNext(out var cog, out var cogComponent))
        {
            if (cogComponent.NextTimeTick <= 0)
            {
                cogComponent.NextTimeTick = 5;
                if (TryComp<BatteryComponent>(cog, out var battery))
                {
                    if (_battery.TryUseCharge((cog, battery), cogComponent.PowerRate))
                    {
                        var cult = _veilCult.GetActiveRule();
                        if (cult != null)
                        {
                            cult.EnergyCount += 10;
                        }
                        _audio.PlayPvs(_audio.ResolveSound(cogComponent.Sound), cog);
                    }
                }
            }

            cogComponent.NextTimeTick -= frameTime;
        }

        var ritualQuery = EntityQueryEnumerator<VeilCultPortalComponent>();
        while (ritualQuery.MoveNext(out var portal, out var comp))
        {
            if (!comp.SoundPlayed && comp.NextTimeTick > 90)
            {
                _sound.PlayGlobalOnStation(portal, _audio.ResolveSound(comp.RitualMusic));
                comp.SoundPlayed = true;
            }

            comp.NextTimeTick += frameTime;
        }
    }

    private void OnShotAttempted(Entity<VeilCultistComponent> ent, ref ShotAttemptedEvent args)
    {
        if (HasComp<CultAllowedGunComponent>(args.Used))
            return;

        _popup.PopupEntity(Loc.GetString("gun-disabled"), ent, ent);
        args.Cancel();
    }

    private void OnAnchor(EntityUid uid, VeilCultBeaconComponent component, AnchorAttemptEvent args)
    {
        var beacons = _entityLookup.GetEntitiesInRange<VeilCultBeaconComponent>(Transform(uid).Coordinates, 20f);

        if (beacons.Count > 1)
        {
            _popup.PopupEntity(Loc.GetString("veil-cult-beacons-in-range"), uid, PopupType.Medium);
            if (args.Cancelled)
                return;

            args.Cancel();
        }
    }

    private void OnCultistHandsExamined(EntityUid uid, VeilCultistHandsComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (TryComp<InventoryComponent>(uid, out var inventory))
        {
            if (!_inventory.TryGetSlotEntity(uid, "gloves", out _, inventory))
                args.PushMarkup(Loc.GetString("veil-cultist-hands-glow-examined"));
        }
    }

    private void OnInit(EntityUid uid, VeilCultBeaconComponent component, ComponentInit args)
    {
        var beacons = _entityLookup.GetEntitiesInRange<VeilCultBeaconComponent>(
            Transform(uid).Coordinates, 20f);

        if (beacons.Count > 1)
        {
            _popup.PopupEntity(Loc.GetString("veil-cult-beacons-in-range"), uid, PopupType.Medium);
            Spawn("SheetChargedBrass6", Transform(uid).Coordinates);
            QueueDel(uid);
        }

        component.AssignedName = Loc.GetString("veil-cult-unknown-beacon");
    }

    private void UseVeilAltar(EntityUid uid, VeilCultAltarComponent component, ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        if (!HasComp<VeilCultistComponent>(args.User) && !HasComp<VeilCultConstructComponent>(args.User))
            return;

        _ui.OpenUi(uid, VeilAltarUiKey.Key, args.User);
        args.Handled = true;
    }

    private void OnSelectOffer(EntityUid uid, VeilCultAltarComponent component, VeilAltarSelectOfferMessage args)
    {
        var cult = _veilCult.GetActiveRule();
        if (cult == null)
            return;

        _audio.PlayPvs(component.Sound, uid);
        Timer.Spawn(TimeSpan.FromSeconds(2), () =>
        {
            var targets = _entityLookup.GetEntitiesInRange<HumanoidProfileComponent>(Transform(uid).Coordinates, 1f);
            foreach (var target in targets)
            {
                if (HasComp<VeilCultistComponent>(target) || HasComp<VeilCultConstructComponent>(target) ||
                    HasComp<NullRodOwnerComponent>(target))
                    continue;

                if (HasComp<MindShieldComponent>(target) || HasComp<BibleUserComponent>(target) || HasComp<BloodCultistComponent>(target))
                {
                    if (_mobState.IsDead(target))
                    {
                        if (TryComp<MindContainerComponent>(target, out var mindContainer) && mindContainer.Mind != null)
                        {
                            var soulStone = Spawn("VeilCultSoulVessel", Transform(target).Coordinates);
                            _mind.TransferTo(mindContainer.Mind.Value, soulStone);
                            EnsureComp<AbsorbedByVeilComponent>(target);
                        }
                        else continue;
                    }
                }
                else
                {
                    if (HasComp<MindShieldComponent>(target) || HasComp<BibleUserComponent>(target))
                        continue;

                    if (!TryComp<MindContainerComponent>(target, out var mindContainer) || mindContainer.Mind == null)
                        continue;

                    EnsureComp<AutoVeilCultistComponent>(target);
                    _rejuvenate.PerformRejuvenate(target);
                    EnsureComp<AbsorbedByVeilComponent>(target);
                }

                if (!HasComp<AbsorbedByVeilComponent>(target))
                    cult.EnergyCount += 100;

                break;
            }
        });
    }

    private void OnSelectEnergy(EntityUid uid, VeilCultAltarComponent component, VeilAltarSelectEnergyMessage args)
    {
        var cult = _veilCult.GetActiveRule();
        if (cult != null)
            _popup.PopupEntity(Loc.GetString("veil-cult-energy-amount", ("energy", cult.EnergyCount)), uid, PopupType.Medium);
    }

    private void UseVeilLathe(EntityUid uid, VeilCultLatheComponent component, ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        if (!HasComp<VeilCultistComponent>(args.User) && !HasComp<VeilCultConstructComponent>(args.User))
            return;

        _ui.OpenUi(uid, LatheUiKey.Key, args.User);
        args.Handled = true;
    }

    private void DoAfterInteractShardAltar(EntityUid uid, VeilCultAltarComponent component, StrangeShardDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        var cult = _veilCult.GetActiveRule();
        if (cult == null || args.Target == null)
            return;

        if (cult.RitualGoing)
        {
            _popup.PopupEntity(Loc.GetString("veil-cult-ritual-going"), uid, PopupType.Medium);
            return;
        }

        if (!cult.SecondTriggered)
        {
            _popup.PopupEntity(Loc.GetString("veil-cult-too-weak"), uid, PopupType.Medium);
            return;
        }

        var walls = _entityLookup.GetEntitiesInRange(uid, 3f, LookupFlags.Static);
        if (walls.Count > 1)
        {
            _popup.PopupEntity(Loc.GetString("veil-cult-walls"), uid, PopupType.Medium);
            return;
        }

        if (!_veilCult.TryUseEnergy(500))
        {
            _popup.PopupEntity(Loc.GetString("veil-cult-not-enough-energy"), uid, PopupType.Medium);
            return;
        }

        AnnounceRitualActivation(uid);
        var portal = Spawn("VeilCultPortal", Transform(uid).Coordinates);
        Timer.Spawn(TimeSpan.FromSeconds(180), () => CompleteRitual(portal));
        QueueDel(args.Target.Value);
        cult.RitualGoing = true;
    }

    private void AnnounceRitualActivation(EntityUid uid)
    {
        var xform = Transform(uid);
        var msg = Loc.GetString("veil-ritual-activate-warning",
            ("location", FormattedMessage.RemoveMarkupOrThrow(_navMap.GetNearestBeaconString((uid, xform)))));
        _chat.DispatchGlobalAnnouncement(msg, playSound: true, colorOverride: Color.Red);
    }

    private void CompleteRitual(EntityUid uid)
    {
        if (!Exists(uid))
        {
            NotifyRitualFailed();
            var cult = _veilCult.GetActiveRule();
            if (cult != null) cult.RitualGoing = false;
            return;
        }

        SpawnGod(uid);
    }

    private void NotifyRitualFailed()
    {
        var cultists = EntityQueryEnumerator<VeilCultistComponent>();
        while (cultists.MoveNext(out var cultist, out _))
        {
            _popup.PopupEntity(Loc.GetString("ritual-failed"), cultist, cultist, PopupType.LargeCaution);
        }
    }

    private void SpawnGod(EntityUid uid)
    {
        Spawn("MobRatvarSpawn", Transform(uid).Coordinates);
        RaiseLocalEvent(new VeilGodCalledEvent());
    }

    private void DoAfterInteractShardCultist(EntityUid uid, VeilCultistComponent component, StrangeShardDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (!HasComp<VeilCultistComponent>(uid) || !HasComp<HumanoidProfileComponent>(uid) ||
            !_mobState.IsDead(uid))
            return;

        if (args.Target != null)
        {
            _rejuvenate.PerformRejuvenate(uid);
            QueueDel(args.Target.Value);
        }
    }

    private void OnSoulVesselInserted(EntityUid uid, SoulVesselComponent comp, AfterInteractEvent args)
    {
        if (_mind.TryGetMind(uid, out var mindId, out var mindComp))
        {
            if (args.Target is { } target && TryComp<MindContainerComponent>(target, out var mindContainer) && !mindContainer.HasMind)
            {
                if (HasComp<CogscarabComponent>(target) || HasComp<BorgChassisComponent>(target))
                    return;

                _mind.TransferTo(mindId, target, ghostCheckOverride: true, createGhost: true, mind: mindComp);
                QueueDel(uid);
            }
        }
    }

    private void OnConstructMobStateChange(EntityUid uid, VeilCultConstructComponent comp, MobStateChangedEvent args)
    {
        if (_mobState.IsDead(uid) && !HasComp<CogscarabComponent>(uid))
        {
            if (TryComp<MindContainerComponent>(uid, out var mindContainer) && mindContainer.Mind != null)
            {
                var soulStone = Spawn("VeilCultSoulVessel", Transform(uid).Coordinates);
                _mind.TransferTo(mindContainer.Mind.Value, soulStone);
            }
        }
    }
}
