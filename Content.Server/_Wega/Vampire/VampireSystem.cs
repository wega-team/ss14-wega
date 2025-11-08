using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Rotting;
using Content.Server.Bible.Components;
using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Content.Server.Polymorph.Systems;
using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Atmos.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.NullRod.Components;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Vampire;
using Content.Shared.Vampire.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Content.Shared.Genetics;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Surgery.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.Inventory;

namespace Content.Server.Vampire;

public sealed partial class VampireSystem : SharedVampireSystem
{
    [Dependency] private readonly IAdminLogManager _admin = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly BloodstreamSystem _blood = default!;
    [Dependency] private readonly FlammableSystem _flammable = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly RottingSystem _rotting = default!;
    [Dependency] private readonly StomachSystem _stomach = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IMapManager _mapMan = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    private static readonly ProtoId<EmotePrototype> Scream = "Scream";

    private readonly Dictionary<EntityUid, Dictionary<EntityUid, FixedPoint2>> _bloodConsumedTracker = new();
    private bool _isDamageBeingHandled = false;

    public override void Initialize()
    {
        base.Initialize();

        // Start
        SubscribeLocalEvent<VampireComponent, ComponentStartup>(OnStartup);

        // Drinking Blood
        SubscribeLocalEvent<VampireComponent, VampireDrinkingBloodActionEvent>(OnDrinkBlood);
        SubscribeLocalEvent<VampireComponent, VampireDrinkingBloodDoAfterEvent>(DrinkDoAfter);

        // Distribute Damage
        SubscribeLocalEvent<VampireComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<ThrallComponent, DamageChangedEvent>(OnDamageChanged);

        // Thralls
        SubscribeLocalEvent<MindShieldComponent, ComponentStartup>(MindShieldImplanted);

        InitializePowers();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var vampireQuery = EntityQueryEnumerator<VampireComponent>();
        while (vampireQuery.MoveNext(out var uid, out var vampireComponent))
        {
            if (IsInSpace(uid))
            {
                if (vampireComponent.NextSpaceDamageTick <= 0)
                {
                    vampireComponent.NextSpaceDamageTick = 1;
                    DoSpaceDamage((uid, vampireComponent));
                }
                vampireComponent.NextSpaceDamageTick -= frameTime;
            }

            if (vampireComponent.NullDamage > 0)
            {
                if (vampireComponent.NextNullDamageTick <= 0)
                {
                    vampireComponent.NextNullDamageTick = 2;
                    vampireComponent.NullDamage -= FixedPoint2.New(2);
                    if (vampireComponent.NullDamage < 0)
                    {
                        vampireComponent.NullDamage = FixedPoint2.Zero;
                    }
                }
                vampireComponent.NextNullDamageTick -= frameTime;
            }
        }

        var holyPointQuery = EntityQueryEnumerator<HolyPointComponent>();
        while (holyPointQuery.MoveNext(out var uid, out var holyPoint))
        {
            if (holyPoint.NextTimeTick <= 0)
            {
                holyPoint.NextTimeTick = 10;
                var vampires = _entityLookup.GetEntitiesInRange<VampireComponent>(Transform(uid).Coordinates, holyPoint.Range);
                foreach (var vampire in vampires)
                {
                    if (vampire.Comp.TruePowerActive) continue;

                    if (TryComp(vampire.Owner, out FlammableComponent? flammable))
                    {
                        flammable.FireStacks = flammable.MaximumFireStacks;
                        _flammable.Ignite(vampire.Owner, uid);
                        _chat.TryEmoteWithoutChat(vampire, _prototypeManager.Index(Scream), true);
                        _popup.PopupEntity(Loc.GetString("vampire-holy-point"), vampire.Owner, vampire.Owner, PopupType.LargeCaution);
                    }
                }
            }
            holyPoint.NextTimeTick -= frameTime;
        }
    }

    // Update Alerts
    private void OnStartup(EntityUid uid, VampireComponent component, ComponentStartup args)
    {
        _alerts.ShowAlert(uid, component.BloodAlert);
    }

    #region Drinking blood
    private void OnDrinkBlood(EntityUid uid, VampireComponent component, VampireDrinkingBloodActionEvent args)
    {
        if (TryDrink(uid, component, args))
        {
            var doAfterDelay = TimeSpan.FromSeconds(3);
            var doAfterEventArgs = new DoAfterArgs(EntityManager, uid, doAfterDelay,
                new VampireDrinkingBloodDoAfterEvent() { Volume = 5f },
                eventTarget: uid,
                target: args.Target,
                used: args.Target)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
                MovementThreshold = 0.01f,
                DistanceThreshold = 0.5f,
                NeedHand = true
            };

            _doAfter.TryStartDoAfter(doAfterEventArgs);
            _popup.PopupEntity(Loc.GetString("vampire-blooddrink-countion"), uid, args.Target, PopupType.MediumCaution);
        }
    }

    private bool TryDrink(EntityUid uid, VampireComponent component, VampireDrinkingBloodActionEvent args)
    {
        if (args.Target == uid)
        {
            _popup.PopupEntity(Loc.GetString("vampire-blooddrink-self"), uid, uid, PopupType.SmallCaution);
            return false;
        }

        if (!_interaction.InRangeUnobstructed(uid, args.Target, popup: true) || HasComp<SyntheticOperatedComponent>(args.Target))
            return false;

        IngestionBlockerComponent? blocker;
        if (_inventory.TryGetSlotEntity(uid, "mask", out var maskUid) &&
            TryComp(maskUid, out blocker) && blocker.Enabled)
            return false;

        if (_inventory.TryGetSlotEntity(uid, "head", out var headUid) &&
            TryComp(headUid, out blocker) && blocker.Enabled)
            return false;

        if (_rotting.IsRotten(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("vampire-blooddrink-rotted"), uid, uid, PopupType.SmallCaution);
            return false;
        }

        if (TryComp<VampireComponent>(args.Target, out var targetVampireComponent))
        {
            _popup.PopupEntity(Loc.GetString("vampire-blooddrink-not-vampire"), uid, uid, PopupType.SmallCaution);
            return false;
        }

        if (TryComp<ThrallComponent>(args.Target, out var targetThrallComponent))
        {
            _popup.PopupEntity(Loc.GetString("vampire-blooddrink-not-thrall"), uid, uid, PopupType.SmallCaution);
            return false;
        }

        return true;
    }

    private void DrinkDoAfter(EntityUid uid, VampireComponent component, ref VampireDrinkingBloodDoAfterEvent args)
    {
        if (args.Cancelled || !TryComp<BloodstreamComponent>(args.Target, out var targetBloodstream)
            || targetBloodstream?.BloodSolution is null)
            return;

        if (_rotting.IsRotten(args.Target!.Value))
        {
            _popup.PopupEntity(Loc.GetString("vampire-blooddrink-rotted"), args.User, args.User, PopupType.SmallCaution);
            return;
        }

        var victimBloodRemaining = targetBloodstream.BloodSolution.Value.Comp.Solution.Volume;
        if (victimBloodRemaining <= 0)
        {
            _popup.PopupEntity(Loc.GetString("vampire-blooddrink-empty"), uid, uid, PopupType.SmallCaution);
            return;
        }

        var bloodAlreadyConsumed = GetBloodConsumedByVampire(uid, args.Target.Value);

        var maxBloodToConsume = 200;
        var maxAvailableBlood = (FixedPoint2)Math.Min((float)victimBloodRemaining, (float)(maxBloodToConsume - bloodAlreadyConsumed));

        if (maxAvailableBlood <= 0)
        {
            _popup.PopupEntity(Loc.GetString("vampire-blooddrink-maxed-out"), uid, uid, PopupType.SmallCaution);
            return;
        }

        var volumeToConsume = (FixedPoint2)Math.Min((float)victimBloodRemaining.Value, args.Volume * 2);

        _audio.PlayPvs(component.BloodDrainSound, uid, AudioParams.Default.WithVolume(-3f));
        _blood.TryModifyBloodLevel(args.Target.Value, -(byte)(volumeToConsume * 0.5f));

        if (HasComp<BibleUserComponent>(args.Target) && !component.TruePowerActive)
        {
            _damage.TryChangeDamage(uid, VampireComponent.HolyDamage, true);
            _popup.PopupEntity(Loc.GetString("vampire-ingest-holyblood"), uid, uid, PopupType.LargeCaution);
            _admin.Add(LogType.Damaged, LogImpact.Low, $"{ToPrettyString(uid):user} attempted to drink {volumeToConsume}u of {ToPrettyString(args.Target):target}'s holy blood");
            return;
        }
        else
        {
            var bloodSolution = _solution.SplitSolution(targetBloodstream.BloodSolution.Value, volumeToConsume);

            if (!TryIngestBlood(uid, component, bloodSolution))
            {
                _solution.AddSolution(targetBloodstream.BloodSolution.Value, bloodSolution);
                return;
            }

            _admin.Add(LogType.Damaged, LogImpact.Low, $"{ToPrettyString(uid):user} drank {volumeToConsume}u of {ToPrettyString(args.Target):target}'s blood");
            if (HasComp<HumanoidAppearanceComponent>(args.Target) && !HasComp<DnaModifiedComponent>(args.Target))
                AddBloodEssence(uid, volumeToConsume * 0.95);
            SetBloodConsumedByVampire(uid, args.Target.Value, bloodAlreadyConsumed + volumeToConsume);

            if (args.Target.HasValue)
                _popup.PopupEntity(Loc.GetString("vampire-blooddrink-countion-doafter"), uid, args.Target.Value, PopupType.SmallCaution);

            args.Repeat = true;
        }
    }

    private bool TryIngestBlood(EntityUid uid, VampireComponent component, Solution ingestedSolution, bool force = false)
    {
        if (TryComp<BodyComponent>(uid, out var body) && _body.TryGetBodyOrganEntityComps<StomachComponent>(uid, out var stomachs))
        {
            var firstStomach = stomachs.FirstOrNull(stomach => _stomach.CanTransferSolution(stomach.Owner, ingestedSolution, stomach));
            if (firstStomach is null)
            {
                _popup.PopupEntity(Loc.GetString("vampire-full-stomach"), uid, uid, PopupType.SmallCaution);
                return false;
            }
            return _stomach.TryTransferSolution(firstStomach.Value.Owner, ingestedSolution, firstStomach.Value);
        }

        return false;
    }

    private FixedPoint2 GetBloodConsumedByVampire(EntityUid vampireUid, EntityUid targetUid)
    {
        if (!_bloodConsumedTracker.ContainsKey(vampireUid))
            _bloodConsumedTracker[vampireUid] = new Dictionary<EntityUid, FixedPoint2>();

        return _bloodConsumedTracker[vampireUid].GetValueOrDefault(targetUid, 0);
    }

    private void SetBloodConsumedByVampire(EntityUid vampireUid, EntityUid targetUid, FixedPoint2 amount)
    {
        if (!_bloodConsumedTracker.ContainsKey(vampireUid))
            _bloodConsumedTracker[vampireUid] = new Dictionary<EntityUid, FixedPoint2>();

        _bloodConsumedTracker[vampireUid][targetUid] = amount;
    }
    #endregion

    #region Blood Manipulation
    private bool AddBloodEssence(EntityUid uid, FixedPoint2 quantity)
    {
        if (quantity < 0 || !TryComp<VampireComponent>(uid, out var vampireComponent))
            return false;

        vampireComponent.CurrentBlood += quantity;
        vampireComponent.TotalBloodDrank += (float)quantity;

        Dirty(uid, vampireComponent);
        _alerts.ShowAlert(uid, vampireComponent.BloodAlert);

        UpdatePowers(uid, vampireComponent);

        return true;
    }

    private bool SubtractBloodEssence(EntityUid uid, FixedPoint2 quantity)
    {
        if (!TryComp<VampireComponent>(uid, out var vampireComponent))
            return false;

        var adjustedQuantity = quantity * (1 + vampireComponent.NullDamage.Float() / 100);
        if (adjustedQuantity <= 0 || vampireComponent.CurrentBlood < adjustedQuantity)
            return false;

        vampireComponent.CurrentBlood -= adjustedQuantity;

        Dirty(uid, vampireComponent);
        _alerts.ShowAlert(uid, vampireComponent.BloodAlert);

        return true;
    }

    private bool CheckBloodEssence(EntityUid uid, FixedPoint2 quantity)
    {
        if (!TryComp<VampireComponent>(uid, out var vampireComponent))
            return false;

        var adjustedQuantity = quantity * (1 + vampireComponent.NullDamage.Float() / 100);
        return vampireComponent.CurrentBlood >= adjustedQuantity;
    }

    private void UpdatePowers(EntityUid uid, VampireComponent component)
    {
        if (component.CurrentEvolution == null)
            return;

        var currentBlood = component.CurrentBlood;
        var vampireClass = component.CurrentEvolution;
        var thresholds = GetThresholdsForClass(vampireClass);
        foreach (var threshold in thresholds)
        {
            if (currentBlood >= threshold.Key)
            {
                foreach (var skill in threshold.Value)
                {
                    if (!HasSkill(component, skill))
                    {
                        AddSkill(uid, component, skill);
                        _admin.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(uid)}: added {skill} for {vampireClass}.");
                    }
                }
            }
        }

        if (currentBlood >= 1000 && !component.TruePowerActive)
        {
            MakeImmuneToHoly(uid, component);
        }
    }

    private bool HasSkill(VampireComponent component, string skill)
    {
        return component.AcquiredSkills.Contains(skill);
    }

    private void AddSkill(EntityUid uid, VampireComponent component, string skill)
    {
        if (!HasSkill(component, skill))
        {
            component.AcquiredSkills.Add(skill);
            _action.AddAction(uid, skill);
        }
    }

    private Dictionary<float, List<string>> GetThresholdsForClass(string vampireClass)
    {
        switch (vampireClass)
        {
            case "Hemomancer":
                return new Dictionary<float, List<string>>
                {
                    { 150f, new List<string> { "ActionVampireClaws" } },
                    { 250f, new List<string> { "ActionVampireBloodTendrils", "ActionVampireBloodBarrier" } },
                    { 400f, new List<string> { "ActionVampireSanguinePool" } },
                    { 600f, new List<string> { "ActionVampirePredatorSenses" } },
                    { 800f, new List<string> { "ActionVampireBloodEruption" } },
                    { 1000f, new List<string> { "ActionVampireBloodBringersRite" } }
                };

            case "Umbrae":
                return new Dictionary<float, List<string>>
                {
                    { 150f, new List<string> { "ActionVampireCloakOfDarkness" } },
                    { 250f, new List<string> { "ActionVampireShadowSnare", "ActionVampireSoulAnchor" } },
                    { 400f, new List<string> { "ActionVampireDarkPassage" } },
                    { 600f, new List<string> { "ActionVampireExtinguish" } },
                    { 800f, new List<string> { "ActionVampireShadowBoxing" } },
                    { 1000f, new List<string> { "ActionVampireEternalDarkness" } }
                };

            case "Gargantua":
                return new Dictionary<float, List<string>>
                {
                    { 150f, new List<string> { "ActionVampireBloodSwell" } },
                    { 250f, new List<string> { "ActionVampireBloodRush", "ActionVampireSeismicStomp" } },
                    { 400f, new List<string> { "ActionVampireBloodSwellAdvanced" } },
                    { 600f, new List<string> { "ActionVampireOverwhelmingForce" } },
                    { 800f, new List<string> { "ActionDemonicGrasp" } },
                    { 1000f, new List<string> { "ActionVampireCharge" } }
                };

            case "Dantalion":
                return new Dictionary<float, List<string>>
                {
                    { 150f, new List<string> { "ActionEnthrall", "ActionCommune" } },
                    { 250f, new List<string> { "ActionPacify", "ActionSubspaceSwap" } },
                    { 400f, new List<string> { /*"ActionDeployDecoy",*/"ActionMaxThrallCountUpdate1" } },
                    { 600f, new List<string> { "ActionRallyThralls", "ActionMaxThrallCountUpdate2" } },
                    { 800f, new List<string> { "ActionBloodBond" } },
                    { 1000f, new List<string> { "ActionMassHysteria", "ActionMaxThrallCountUpdate3" } }
                };

            default:
                return new Dictionary<float, List<string>>();
        }
    }
    #endregion

    #region Space Damage
    private void DoSpaceDamage(Entity<VampireComponent> vampire)
    {
        _damage.TryChangeDamage(vampire, VampireComponent.SpaceDamage, true, origin: vampire);
        _popup.PopupEntity(Loc.GetString("vampire-startlight-burning"), vampire, vampire, PopupType.LargeCaution);
    }

    private bool IsInSpace(EntityUid vampireUid)
    {
        var vampirePosition = _transform.GetMapCoordinates(Transform(vampireUid));
        if (!_mapMan.TryFindGridAt(vampirePosition, out _, out _))
            return true;

        return false;
    }
    #endregion

    #region Distribute Damage
    private void OnDamageChanged(EntityUid uid, VampireComponent component, ref DamageChangedEvent args)
    {
        // Null Rode Damage
        if (args.Origin.HasValue && HasComp<BibleUserComponent>(args.Origin.Value) && !component.TruePowerActive)
        {
            var heldEntity = _hands.GetActiveItem(uid);
            if (TryComp<NullRodComponent>(heldEntity, out var nullRodComp))
            {
                var damageToApply = component.NullDamage > 0
                    ? nullRodComp.NullDamage
                    : nullRodComp.FirstNullDamage;

                component.NullDamage += damageToApply;
                component.NullDamage = FixedPoint2.Clamp(component.NullDamage, FixedPoint2.Zero, 120);
            }
        }

        // Distribute Damage
        if (_isDamageBeingHandled || !component.IsDamageSharingActive
            || component.ThrallOwned.Count == 0 || args.DamageDelta is null
            || IsNegativeDamage(args.DamageDelta))
            return;

        _isDamageBeingHandled = true;
        _damage.TryChangeDamage(uid, -args.DamageDelta, true);
        DistributeDamage(uid, component, args.DamageDelta, ref args);
        _isDamageBeingHandled = false;
    }

    private void OnDamageChanged(EntityUid uid, ThrallComponent component, ref DamageChangedEvent args)
    {
        if (_isDamageBeingHandled || !TryComp(component.VampireOwner, out VampireComponent? vampire)
            || !vampire.IsDamageSharingActive || args.DamageDelta is null
            || IsNegativeDamage(args.DamageDelta))
            return;

        _isDamageBeingHandled = true;
        _damage.TryChangeDamage(uid, -args.DamageDelta, true);
        DistributeDamage(component.VampireOwner.Value, vampire, args.DamageDelta, ref args);
        _isDamageBeingHandled = false;
    }

    private void DistributeDamage(
        EntityUid vampireUid,
        VampireComponent vampireComponent,
        DamageSpecifier damage,
        ref DamageChangedEvent args,
        EntityUid? excludedEntity = null)
    {
        if (damage == null)
            return;

        var participants = new List<EntityUid> { vampireUid };
        participants.AddRange(vampireComponent.ThrallOwned.Where(thrall => Exists(thrall) && thrall != excludedEntity));

        if (participants.Count == 0)
            return;

        var sharedDamage = damage / participants.Count;

        foreach (var participant in participants)
        {
            if (!TryComp<DamageableComponent>(participant, out var damageable))
                continue;

            _damage.TryChangeDamage(participant, sharedDamage, true, damageable: damageable);
        }
    }

    private bool IsNegativeDamage(DamageSpecifier damage)
    {
        var totalDamage = damage.DamageDict.Values.Aggregate(FixedPoint2.Zero, (sum, value) => sum + value);
        return totalDamage < FixedPoint2.Zero;
    }
    #endregion

    #region Thralls
    private void MindShieldImplanted(EntityUid uid, MindShieldComponent comp, ComponentStartup init)
    {
        if (TryComp<ThrallComponent>(uid, out var thrall))
        {
            var stunTime = TimeSpan.FromSeconds(4);
            var name = Identity.Entity(uid, EntityManager);
            if (TryComp<VampireComponent>(thrall.VampireOwner, out var vampire))
            {
                vampire.ThrallOwned.Remove(uid);
                vampire.ThrallCount--;
            }

            RemComp<ThrallComponent>(uid);
            _stun.TryUpdateParalyzeDuration(uid, stunTime);
            _popup.PopupEntity(Loc.GetString("thrall-break-control", ("name", name)), uid);
        }
    }
    #endregion

    #region True Power
    private void MakeImmuneToHoly(EntityUid vampire, VampireComponent component)
    {
        if (TryComp<ReactiveComponent>(vampire, out var reactive))
        {
            if (reactive.ReactiveGroups == null)
                return;

            reactive.ReactiveGroups.Remove("Unholy");
        }

        component.TruePowerActive = true;
        RemComp<UnholyComponent>(vampire);

        _popup.PopupEntity(Loc.GetString("vampire-true-power"), vampire, vampire, PopupType.Medium);
    }
    #endregion
}
