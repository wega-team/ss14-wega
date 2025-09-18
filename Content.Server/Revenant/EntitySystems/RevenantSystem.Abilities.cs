using Content.Shared.Popups;
using Content.Shared.Damage;
using Content.Shared.Revenant;
using Robust.Shared.Random;
using Content.Shared.Tag;
using Content.Server.Storage.Components;
using Content.Server.Light.Components;
using Content.Server.Ghost;
using Robust.Shared.Physics;
using Content.Shared.Throwing;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Bed.Sleep;
using System.Linq;
using System.Numerics;
using Content.Server.Revenant.Components;
using Content.Shared.Physics;
using Content.Shared.DoAfter;
using Content.Shared.Emag.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Light.Components;
using Content.Shared.Maps;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Revenant.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Utility;
using Robust.Shared.Map.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
// Corvax-Wega-Revenant-start
using Content.Server.Administration;
using Content.Server.Disease;
using Content.Server.Hallucinations;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Server.Prayer;
using Content.Shared.Body.Components;
using Content.Shared.CombatMode;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Weapons.Melee;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Content.Shared.Disease.Components;
using Content.Shared.NullRod.Components;
using Robust.Server.Containers;
using Content.Shared.Weapons.Ranged.Components;
// Corvax-Wega-Revenant-end

namespace Content.Server.Revenant.EntitySystems;

public sealed partial class RevenantSystem
{
    [Dependency] private readonly EmagSystem _emagSystem = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly MobThresholdSystem _mobThresholdSystem = default!;
    [Dependency] private readonly GhostSystem _ghost = default!;
    [Dependency] private readonly TileSystem _tile = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly DiseaseSystem _disease = default!; // Corvax-Wega-Disease
    // Corvax-Wega-Revenant-start
    [Dependency] private readonly HallucinationsSystem _hallucinations = default!;
    [Dependency] private readonly HTNSystem _htn = default!;
    [Dependency] private readonly SharedPointLightSystem _light = default!;
    [Dependency] private readonly NPCSystem _npc = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly PrayerSystem _prayerSystem = default!;
    [Dependency] private readonly QuickDialogSystem _quickDialog = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    // Corvax-Wega-Revenant-end

    private static readonly ProtoId<HTNCompoundPrototype> HauntRootTask = "SimpleRangedHostileCompound"; // Corvax-Wega-Revenant

    private static readonly ProtoId<TagPrototype> WindowTag = "Window";

    private void InitializeAbilities()
    {
        SubscribeLocalEvent<RevenantComponent, UserActivateInWorldEvent>(OnInteract);
        SubscribeLocalEvent<RevenantComponent, SoulEvent>(OnSoulSearch);
        SubscribeLocalEvent<RevenantComponent, HarvestEvent>(OnHarvest);

        SubscribeLocalEvent<RevenantComponent, RevenantDefileActionEvent>(OnDefileAction);
        SubscribeLocalEvent<RevenantComponent, RevenantOverloadLightsActionEvent>(OnOverloadLightsAction);
        SubscribeLocalEvent<RevenantComponent, RevenantBlightActionEvent>(OnBlightAction);
        SubscribeLocalEvent<RevenantComponent, RevenantMalfunctionActionEvent>(OnMalfunctionAction);
        // Corvax-Wega-Revenant-start
        SubscribeLocalEvent<RevenantComponent, RevenantTransmitActionEvent>(OnTransmitAction);
        SubscribeLocalEvent<RevenantComponent, RevenantHauntActionEvent>(OnHauntAction);
        SubscribeLocalEvent<RevenantComponent, RevenantHallucinationActionEvent>(OnHallucinationAction);
        // Corvax-Wega-Revenant-end
    }

    private void OnInteract(EntityUid uid, RevenantComponent component, UserActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        if (args.Target == args.User)
            return;
        var target = args.Target;

        if (HasComp<PoweredLightComponent>(target))
        {
            args.Handled = _ghost.DoGhostBooEvent(target);
            return;
        }

        if (!HasComp<MobStateComponent>(target) || !HasComp<HumanoidAppearanceComponent>(target) || HasComp<RevenantComponent>(target))
            return;

        args.Handled = true;
        if (!TryComp<EssenceComponent>(target, out var essence) || !essence.SearchComplete)
        {
            EnsureComp<EssenceComponent>(target);
            BeginSoulSearchDoAfter(uid, target, component);
        }
        else
        {
            BeginHarvestDoAfter(uid, target, component, essence);
        }

        args.Handled = true;
    }

    private void BeginSoulSearchDoAfter(EntityUid uid, EntityUid target, RevenantComponent revenant)
    {
        var searchDoAfter = new DoAfterArgs(EntityManager, uid, revenant.SoulSearchDuration, new SoulEvent(), uid, target: target)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            DistanceThreshold = 2
        };

        if (!_doAfter.TryStartDoAfter(searchDoAfter))
            return;

        _popup.PopupEntity(Loc.GetString("revenant-soul-searching", ("target", target)), uid, uid, PopupType.Medium);
    }

    private void OnSoulSearch(EntityUid uid, RevenantComponent component, SoulEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (!TryComp<EssenceComponent>(args.Args.Target, out var essence))
            return;
        essence.SearchComplete = true;

        string message;
        switch (essence.EssenceAmount)
        {
            case <= 45:
                message = "revenant-soul-yield-low";
                break;
            case >= 90:
                message = "revenant-soul-yield-high";
                break;
            default:
                message = "revenant-soul-yield-average";
                break;
        }
        _popup.PopupEntity(Loc.GetString(message, ("target", args.Args.Target)), args.Args.Target.Value, uid, PopupType.Medium);

        args.Handled = true;
    }

    private void BeginHarvestDoAfter(EntityUid uid, EntityUid target, RevenantComponent revenant, EssenceComponent essence)
    {
        if (essence.Harvested)
        {
            _popup.PopupEntity(Loc.GetString("revenant-soul-harvested"), target, uid, PopupType.SmallCaution);
            return;
        }

        if (TryComp<MobStateComponent>(target, out var mobstate) && mobstate.CurrentState == MobState.Alive && !HasComp<SleepingComponent>(target)
            || HasComp<NullRodOwnerComponent>(target)) // Corvax-Wega-NullRod
        {
            _popup.PopupEntity(Loc.GetString("revenant-soul-too-powerful"), target, uid);
            return;
        }

        if(_physics.GetEntitiesIntersectingBody(uid, (int) CollisionGroup.Impassable).Count > 0)
        {
            _popup.PopupEntity(Loc.GetString("revenant-in-solid"), uid, uid);
            return;
        }

        var doAfter = new DoAfterArgs(EntityManager, uid, revenant.HarvestDebuffs.X, new HarvestEvent(), uid, target: target)
        {
            DistanceThreshold = 2,
            BreakOnMove = true,
            BreakOnDamage = true,
            RequireCanInteract = false, // stuns itself
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        _appearance.SetData(uid, RevenantVisuals.Harvesting, true);

        _popup.PopupEntity(Loc.GetString("revenant-soul-begin-harvest", ("target", target)),
            target, PopupType.Large);

        TryUseAbility(uid, revenant, 0, revenant.HarvestDebuffs);
    }

    private void OnHarvest(EntityUid uid, RevenantComponent component, HarvestEvent args)
    {
        if (args.Cancelled)
        {
            _appearance.SetData(uid, RevenantVisuals.Harvesting, false);
            return;
        }

        if (args.Handled || args.Args.Target == null)
            return;

        _appearance.SetData(uid, RevenantVisuals.Harvesting, false);

        if (!TryComp<EssenceComponent>(args.Args.Target, out var essence))
            return;

        _popup.PopupEntity(Loc.GetString("revenant-soul-finish-harvest", ("target", args.Args.Target)),
            args.Args.Target.Value, PopupType.LargeCaution);

        essence.Harvested = true;
        ChangeEssenceAmount(uid, essence.EssenceAmount, component);
        _store.TryAddCurrency(new Dictionary<string, FixedPoint2>
            { {component.StolenEssenceCurrencyPrototype, essence.EssenceAmount} }, uid);

        if (!HasComp<MobStateComponent>(args.Args.Target))
            return;

        if (_mobState.IsAlive(args.Args.Target.Value) || _mobState.IsCritical(args.Args.Target.Value))
        {
            _popup.PopupEntity(Loc.GetString("revenant-max-essence-increased"), uid, uid);
            component.EssenceRegenCap += component.MaxEssenceUpgradeAmount;
        }

        //KILL THEMMMM

        if (!_mobThresholdSystem.TryGetThresholdForState(args.Args.Target.Value, MobState.Dead, out var damage))
            return;
        DamageSpecifier dspec = new();
        dspec.DamageDict.Add("Cold", damage.Value);
        _damage.TryChangeDamage(args.Args.Target, dspec, true, origin: uid);

        args.Handled = true;
    }

    private void OnDefileAction(EntityUid uid, RevenantComponent component, RevenantDefileActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryUseAbility(uid, component, component.DefileCost, component.DefileDebuffs))
            return;

        args.Handled = true;

        //var coords = Transform(uid).Coordinates;
        //var gridId = coords.GetGridUid(EntityManager);
        var xform = Transform(uid);
        if (!TryComp<MapGridComponent>(xform.GridUid, out var map))
            return;
        var tiles = _mapSystem.GetTilesIntersecting(
            xform.GridUid.Value,
            map,
            Box2.CenteredAround(_transformSystem.GetWorldPosition(xform),
            new Vector2(component.DefileRadius * 2, component.DefileRadius)))
            .ToArray();

        _random.Shuffle(tiles);

        for (var i = 0; i < component.DefileTilePryAmount; i++)
        {
            if (!tiles.TryGetValue(i, out var value))
                continue;
            _tile.PryTile(value);
        }

        var lookup = _lookup.GetEntitiesInRange(uid, component.DefileRadius, LookupFlags.Approximate | LookupFlags.Static);
        var tags = GetEntityQuery<TagComponent>();
        var entityStorage = GetEntityQuery<EntityStorageComponent>();
        var items = GetEntityQuery<ItemComponent>();
        var lights = GetEntityQuery<PoweredLightComponent>();

        foreach (var ent in lookup)
        {
            //break windows
            if (tags.HasComponent(ent) && _tag.HasTag(ent, WindowTag))
            {
                //hardcoded damage specifiers til i die.
                var dspec = new DamageSpecifier();
                dspec.DamageDict.Add("Structural", 60);
                _damage.TryChangeDamage(ent, dspec, origin: uid);
            }

            if (!_random.Prob(component.DefileEffectChance))
                continue;

            //randomly opens some lockers and such.
            if (entityStorage.TryGetComponent(ent, out var entstorecomp))
                _entityStorage.OpenStorage(ent, entstorecomp);

            //chucks shit
            if (items.HasComponent(ent) &&
                TryComp<PhysicsComponent>(ent, out var phys) && phys.BodyType != BodyType.Static)
                _throwing.TryThrow(ent, _random.NextAngle().ToWorldVec());

            //flicker lights
            if (lights.HasComponent(ent))
                _ghost.DoGhostBooEvent(ent);
        }
    }

    private void OnOverloadLightsAction(EntityUid uid, RevenantComponent component, RevenantOverloadLightsActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryUseAbility(uid, component, component.OverloadCost, component.OverloadDebuffs))
            return;

        args.Handled = true;

        var xform = Transform(uid);
        var poweredLights = GetEntityQuery<PoweredLightComponent>();
        var mobState = GetEntityQuery<MobStateComponent>();
        var lookup = _lookup.GetEntitiesInRange(uid, component.OverloadRadius);
        //TODO: feels like this might be a sin and a half
        foreach (var ent in lookup)
        {
            if (!mobState.HasComponent(ent) || !_mobState.IsAlive(ent))
                continue;

            var nearbyLights = _lookup.GetEntitiesInRange(ent, component.OverloadZapRadius)
                .Where(e => poweredLights.HasComponent(e) && !HasComp<RevenantOverloadedLightsComponent>(e) &&
                            _interact.InRangeUnobstructed(e, uid, -1)).ToArray();

            if (!nearbyLights.Any())
                continue;

            //get the closest light
            var allLight = nearbyLights.OrderBy(e =>
                Transform(e).Coordinates.TryDistance(EntityManager, xform.Coordinates, out var dist) ? component.OverloadZapRadius : dist);
            var comp = EnsureComp<RevenantOverloadedLightsComponent>(allLight.First());
            comp.Target = ent; //who they gon fire at?
        }
    }

    private void OnBlightAction(EntityUid uid, RevenantComponent component, RevenantBlightActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryUseAbility(uid, component, component.BlightCost, component.BlightDebuffs))
            return;

        args.Handled = true;
        // Corvax-Wega-Disease-start
        var emo = GetEntityQuery<DiseaseCarrierComponent>();
        foreach (var ent in _lookup.GetEntitiesInRange(uid, component.BlightRadius))
        {
            if (HasComp<NullRodOwnerComponent>(ent))
                continue;

            if (emo.TryGetComponent(ent, out var comp))
                _disease.TryAddDisease(ent, component.BlightDiseasePrototypeId, comp);
        }
        // Corvax-Wega-Disease-end
    }

    private void OnMalfunctionAction(EntityUid uid, RevenantComponent component, RevenantMalfunctionActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryUseAbility(uid, component, component.MalfunctionCost, component.MalfunctionDebuffs))
            return;

        args.Handled = true;

        foreach (var ent in _lookup.GetEntitiesInRange(uid, component.MalfunctionRadius))
        {
            if (_whitelistSystem.IsWhitelistFail(component.MalfunctionWhitelist, ent) ||
                _whitelistSystem.IsBlacklistPass(component.MalfunctionBlacklist, ent))
                continue;

            _emagSystem.TryEmagEffect(uid, uid, ent);
        }
    }

    // Corvax-Wega-Revenant-start
    private void OnTransmitAction(EntityUid uid, RevenantComponent component, RevenantTransmitActionEvent args)
    {
        var target = args.Target;
        if (args.Handled || !HasComp<HumanoidAppearanceComponent>(target)
            || !TryComp<ActorComponent>(uid, out var playerActor))
            return;

        args.Handled = true;

        var playerSession = playerActor.PlayerSession;
        _quickDialog.OpenDialog(playerSession, Loc.GetString("revenant-transmit-title"), Loc.GetString("revenant-transmit-prompt"),
            (string message) =>
            {
                var finalMessage = string.IsNullOrWhiteSpace(message)
                    ? Loc.GetString("revenant-transmit-default-message")
                    : message;

                if (!TryComp<ActorComponent>(target, out var targetActor) || HasComp<NullRodOwnerComponent>(target))
                    return;

                _prayerSystem.SendSubtleMessage(targetActor.PlayerSession, targetActor.PlayerSession, finalMessage, Loc.GetString("revenant-transmit-default-message"));

                _popup.PopupEntity(Loc.GetString("revenant-transmit-sent"), uid, args.Performer, PopupType.Medium);
            });
    }

    private void OnHauntAction(EntityUid uid, RevenantComponent component, RevenantHauntActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryUseAbility(uid, component, component.HauntCost, component.HauntDebuffs))
            return;

        args.Handled = true;
        var itemsInRange = _lookup.GetEntitiesInRange<ItemComponent>(Transform(uid).Coordinates, component.HauntRadius)
            .ToList();

        if (itemsInRange.Count == 0)
            return;

        itemsInRange = itemsInRange
            .Where(item => !_container.TryGetContainingContainer(item.Owner, out _))
            .ToList();

        var randomItems = itemsInRange
            .OrderBy(_ => _random.Next())
            .Take(_random.Next(3, 8))
            .ToList();

        foreach (var item in randomItems)
        {
            var itemEntity = item.Owner;
            if (HasComp<HTNComponent>(itemEntity))
                continue;

            var npcFaction = EnsureComp<NpcFactionMemberComponent>(itemEntity);
            _npcFaction.ClearFactions((itemEntity, npcFaction), false);
            _npcFaction.AddFaction((itemEntity, npcFaction), component.HauntFaction);

            EnsureComp<HTNComponent>(itemEntity, out var htn);
            htn.RootTask = new HTNCompoundTask { Task = HauntRootTask };
            _npc.WakeNPC(itemEntity, htn);
            _htn.Replan(htn);

            EnsureComp<CombatModeComponent>(itemEntity);
            EnsureComp<InputMoverComponent>(itemEntity);
            EnsureComp<MobMoverComponent>(itemEntity);
            EnsureComp<MovementSpeedModifierComponent>(itemEntity);
            EnsureComp<MovementAlwaysTouchingComponent>(itemEntity);

            bool addedLight = false;
            if (!HasComp<PointLightComponent>(itemEntity))
            {
                EnsureComp<PointLightComponent>(itemEntity, out var light);
                var itemColor = new Color(147, 112, 219, 255);
                _light.SetColor(itemEntity, itemColor, light);
                _light.SetSoftness(itemEntity, 2f, light);
                addedLight = true;
            }

            bool addedWeapon = false;
            if (!HasComp<MeleeWeaponComponent>(itemEntity))
            {
                EnsureComp<MeleeWeaponComponent>(itemEntity, out var meleeWeaponComponent);
                var damage = new DamageSpecifier { DamageDict = { { "Blunt", 5 } } };
                meleeWeaponComponent.Damage = damage;
                addedWeapon = true;
            }

            bool removedGunWield = false;
            if (HasComp<GunRequiresWieldComponent>(itemEntity))
            {
                RemComp<GunRequiresWieldComponent>(itemEntity);
                removedGunWield = true;
            }

            var name = Name(itemEntity);
            _popup.PopupEntity(Loc.GetString("revenant-haunt-alive", ("name", name)), itemEntity, PopupType.Small);

            Timer.Spawn(20000, () =>
            {
                if (!Exists(itemEntity))
                    return;

                var componentsToRemove = new[]
                {
                    typeof(HTNComponent),
                    typeof(CombatModeComponent),
                    typeof(NpcFactionMemberComponent),
                    typeof(InputMoverComponent),
                    typeof(MobMoverComponent),
                    typeof(MovementSpeedModifierComponent),
                    typeof(MovementAlwaysTouchingComponent)
                };

                foreach (var compType in componentsToRemove)
                {
                    if (HasComp(itemEntity, compType))
                        RemComp(itemEntity, compType);
                }

                if (addedLight)
                    RemComp<PointLightComponent>(itemEntity);
                if (addedWeapon)
                    RemComp<MeleeWeaponComponent>(itemEntity);
                if (removedGunWield)
                    EnsureComp<GunRequiresWieldComponent>(itemEntity);

                _popup.PopupEntity(Loc.GetString("revenant-haunt-end", ("name", name)), itemEntity, PopupType.Small);
            });
        }
    }

    private void OnHallucinationAction(EntityUid uid, RevenantComponent component, RevenantHallucinationActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryUseAbility(uid, component, component.HallucinationCost, component.HallucinationDebuffs))
            return;

        args.Handled = true;
        var victimInRange = _lookup.GetEntitiesInRange<BodyComponent>(Transform(uid).Coordinates, component.HallucinationRadius)
            .Where(entity => entity.Owner != uid)
            .ToList();
        foreach (var victimEntity in victimInRange)
        {
            if (HasComp<NullRodOwnerComponent>(victimEntity))
                continue;

            _hallucinations.StartHallucinations(victimEntity, "Hallucinations", TimeSpan.FromSeconds(30f), true, "MindBreaker");
        }
    }
    // Corvax-Wega-Revenant-end
}
