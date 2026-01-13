using System.Linq;
using System.Numerics;
using Content.Server.Body.Systems;
using Content.Server.Cargo.Components;
using Content.Server.Chat.Systems;
using Content.Server.Dice;
using Content.Server.Economy.SlotMachine;
using Content.Server.Guardian;
using Content.Server.Hallucinations;
using Content.Server.Polymorph.Systems;
using Content.Server.Revolutionary.Components;
using Content.Server.Shuttles.Components;
using Content.Server.Stack;
using Content.Server.Station.Components;
using Content.Server.Surgery;
using Content.Shared.Administration.Systems;
using Content.Shared.Armor;
using Content.Shared.Blood.Cult.Components;
using Content.Shared.Body.Components;
using Content.Shared.Card.Tarot;
using Content.Shared.Card.Tarot.Components;
using Content.Shared.Chat;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Disease.Components;
using Content.Shared.EnergyShield;
using Content.Shared.FixedPoint;
using Content.Shared.Ghost;
using Content.Shared.Gravity;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Lock;
using Content.Shared.Mobs.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Pinpointer;
using Content.Shared.Polymorph;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.StatusEffectNew;
using Content.Shared.Storage.Components;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Content.Shared.Tiles;
using Content.Shared.Traits.Assorted;
using Content.Shared.Trigger.Components.Triggers;
using Content.Shared.Trigger.Systems;
using Content.Shared.VendingMachines;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Card.Tarot;

public sealed class CardTarotSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly BloodstreamSystem _blood = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly DiceOfFateSystem _dice = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedGravitySystem _gravity = default!;
    [Dependency] private readonly HallucinationsSystem _hallucinations = default!;
    [Dependency] private readonly IngestionSystem _ingestion = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly LockSystem _lock = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RejuvenateSystem _rejuvenate = default!;
    [Dependency] private readonly SlotMachineSystem _slotMachine = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly StackSystem _stack = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SurgerySystem _surgery = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly TriggerSystem _trigger = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    // 200,000 static variables are ready, and another million is on the way
    private static readonly EntProtoId Ash = "Ash";
    private static readonly EntProtoId ClusterBang = "ClusterBangFull";
    private static readonly EntProtoId ClusterGrenade = "ClusterGrenade";
    private static readonly EntProtoId CursedSlotMachine = "CursedSlotMachine";
    private static readonly EntProtoId Drunk = "StatusEffectDrunk";
    private static readonly EntProtoId EmptyCardTarot = "CardTarotNotEnchanted";
    private static readonly EntProtoId Pill = "StrangePill";
    private static readonly EntProtoId RandomContainer = "RandomContainerBlank";
    private static readonly EntProtoId RandomVending = "RandomVending";
    private static readonly EntProtoId Rock = "WallRock";
    private static readonly EntProtoId Smoke = "AdminInstantEffectSmoke30";
    private static readonly EntProtoId SpaceCash = "SpaceCash";
    private static readonly EntProtoId Stand = "MobHoloparasiteGuardian";

    private static readonly List<EntProtoId> DeathEnt = new() {
        "BloodCultConstruct", "BloodCultSoulStone"
    };

    private static readonly List<EntProtoId> JusticeEnt = new() {
        "MedkitFilled", "TearGasGrenade", "WeaponWandPolymorphDoor", "SpaceCash100"
    };

    private static readonly ProtoId<DamageTypePrototype> BluntDamage = "Blunt";
    private static readonly ProtoId<DamageTypePrototype> CellularDamage = "Cellular";
    private static readonly ProtoId<DamageTypePrototype> HeatDamage = "Heat";
    private static readonly ProtoId<DamageTypePrototype> PoisonDamage = "Poison";
    private static readonly ProtoId<DamageTypePrototype> RadiationDamage = "Blunt";

    private static readonly ProtoId<PolymorphPrototype> ChariotStatue = "ChariotStatue";

    private static readonly ProtoId<TagPrototype> Grenade = "HandGrenade";
    private static readonly ProtoId<TagPrototype> SlowImmune = "SlowImmune";
    private static readonly ProtoId<TagPrototype> StunImmune = "StunImmune";

    private static readonly string Drug = "Desoxyephedrine";
    private static readonly string NotHeal = "Puncturase"; // LoL

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CardTarotComponent, AfterInteractEvent>(OnTarotInteract);
        SubscribeLocalEvent<CardTarotComponent, UseInHandEvent>(OnUseTarot);
    }

    #region Card Tarot
    private void OnTarotInteract(Entity<CardTarotComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { Valid: true } target)
            return;

        if (ent.Comp.Card == CardTarot.NotEnchanted)
        {
            _popup.PopupEntity("tarot-card-not-enchanted", args.User, args.User);
            return;
        }

        PerformTarotEffect(target, args.User, ent);
        args.Handled = true;
    }

    private void OnUseTarot(Entity<CardTarotComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.Card == CardTarot.NotEnchanted)
        {
            _popup.PopupEntity("tarot-card-not-enchanted", args.User, args.User);
            return;
        }

        PerformTarotEffect(args.User, args.User, ent);
        args.Handled = true;
    }

    private void PerformTarotEffect(EntityUid target, EntityUid user, Entity<CardTarotComponent> card)
    {
        var isReversed = card.Comp.CardType == CardTarotType.Reversed;

        switch (card.Comp.Card)
        {
            case CardTarot.NotEnchanted:
                _popup.PopupEntity("tarot-card-not-enchanted", user, user);
                return;
            case CardTarot.Fool:
                PerformFool(target, isReversed);
                break;
            case CardTarot.Magician:
                PerformMagician(target, isReversed);
                break;
            case CardTarot.HighPriestess:
                PerformHighPriestess(target, isReversed);
                break;
            case CardTarot.Empress:
                PerformEmpress(target, isReversed);
                break;
            case CardTarot.Emperor:
                PerformEmperor(target, isReversed);
                break;
            case CardTarot.Hierophant:
                PerformHierophant(target, isReversed);
                break;
            case CardTarot.Lovers:
                PerformLovers(target, isReversed);
                break;
            case CardTarot.Chariot:
                PerformChariot(target, isReversed);
                break;
            case CardTarot.Justice:
                PerformJustice(target, isReversed);
                break;
            case CardTarot.Hermit:
                PerformHermit(target, isReversed);
                break;
            case CardTarot.WheelOfFortune:
                PerformWheelOfFortune(target, isReversed);
                break;
            case CardTarot.Strength:
                PerformStrength(target, isReversed);
                break;
            case CardTarot.HangedMan:
                PerformHangedMan(target, isReversed);
                break;
            case CardTarot.Death:
                PerformDeath(user, target, isReversed);
                break;
            case CardTarot.Temperance:
                PerformTemperance(target, isReversed);
                break;
            case CardTarot.Devil:
                PerformDevil(target, isReversed);
                break;
            case CardTarot.Tower:
                PerformTower(target, isReversed);
                break;
            case CardTarot.Stars:
                PerformStars(target, isReversed);
                break;
            case CardTarot.Moon:
                PerformMoon(target, isReversed);
                break;
            case CardTarot.Sun:
                PerformSun(target, isReversed);
                break;
            case CardTarot.Judgement:
                PerformJudgement(target, isReversed);
                break;
            case CardTarot.TheWorld:
                PerformTheWorld(target, isReversed);
                break;
            default: break;
        }

        var coords = Transform(user).Coordinates;

        var ash = Spawn(Ash, coords);
        _throwing.TryThrow(ash, _random.NextVector2());
        _audio.PlayPredicted(card.Comp.UseSound, coords, user);
        _popup.PopupEntity(Loc.GetString("tarot-used", ("name", Identity.Name(user, EntityManager)),
            ("type", Loc.GetString($"tarot-card-{card.Comp.Card.ToString().ToLower()}"))),
            user, PopupType.Medium);

        QueueDel(card);
    }

    #region Card Effects
    private void PerformFool(EntityUid target, bool reversed)
    {
        if (reversed)
        {
            if (_inventory.TryGetSlots(target, out var slots))
            {
                foreach (var slot in slots)
                {
                    _inventory.TryUnequip(target, slot.Name, force: true);
                }
            }
        }
        else
        {
            EntityUid? shuttle = null;
            var shuttles = EntityQueryEnumerator<ArrivalsShuttleComponent>();
            while (shuttles.MoveNext(out var uid, out _))
            {
                shuttle = uid;
                break;
            }

            if (shuttle == null)
                return;

            _transform.SetCoordinates(target, Transform(shuttle.Value).Coordinates);
        }
    }

    private void PerformMagician(EntityUid target, bool reversed)
    {
        if (reversed)
        {
            var nearbyEntity = _entityLookup.GetEntitiesInRange<TransformComponent>(Transform(target).Coordinates, 6f, LookupFlags.Dynamic)
                .Where(e => !e.Comp.Anchored).ToList();

            foreach (var entity in nearbyEntity)
            {
                var entityUid = entity.Owner;
                if (entityUid == target)
                    continue;

                var targetPosition = _transform.GetWorldPosition(target);
                var entityPosition = _transform.GetWorldPosition(entityUid);
                var direction = (entityPosition - targetPosition).Normalized();

                _physics.ApplyLinearImpulse(entityUid, direction * 2000f);
            }
        }
        else
        {
            _statusEffects.TrySetStatusEffectDuration(target, "StatusEffectPainNumbness", TimeSpan.FromMinutes(2));
        }
    }

    private void PerformHighPriestess(EntityUid target, bool reversed)
    {
        if (reversed)
        {
            /// Temporarily empty
        }
        else
        {
            var time = TimeSpan.FromSeconds(20);
            _stun.TryAddParalyzeDuration(target, time);
            Timer.Spawn(time, () =>
            {
                var damage = new DamageSpecifier { DamageDict = { { BluntDamage, 60 } } };
                _damage.TryChangeDamage(target, damage);
            });
        }
    }

    private void PerformEmpress(EntityUid target, bool reversed)
    {
        if (reversed)
        {
            var nearbyEntity = _entityLookup.GetEntitiesInRange<MobStateComponent>(Transform(target).Coordinates, 6f)
                .Where(e => e.Owner != target).ToList();

            foreach (var entity in nearbyEntity)
            {
                var entityUid = entity.Owner;
                if (HasComp<PacifiedComponent>(entityUid))
                    return;

                EnsureComp<PacifiedComponent>(entityUid);
                Timer.Spawn(TimeSpan.FromSeconds(40), () => { RemComp<PacifiedComponent>(entityUid); });
            }
        }
        else
        {
            if (!TryComp<BloodstreamComponent>(target, out var bloodstream) || bloodstream.BloodSolution == null)
                return;

            var drugQuantity = new ReagentQuantity(Drug, FixedPoint2.New(4.5));
            var notHealQuantity = new ReagentQuantity(NotHeal, FixedPoint2.New(12));

            _solution.TryAddReagent(bloodstream.BloodSolution.Value, drugQuantity, out _);
            _solution.TryAddReagent(bloodstream.BloodSolution.Value, notHealQuantity, out _);
        }
    }

    private void PerformEmperor(EntityUid target, bool reversed)
    {
        if (reversed)
        {
            var selected = new List<EntityUid>();
            var commandQuery = EntityQueryEnumerator<CommandStaffComponent>();
            while (commandQuery.MoveNext(out var uid, out _))
                selected.Add(uid);

            if (selected.Count == 0)
                return;

            _transform.SetCoordinates(target, Transform(_random.Pick(selected)).Coordinates);
        }
        else
        {
            EntityUid? bridgeUid = null;
            var beaconQuery = EntityQueryEnumerator<NavMapBeaconComponent>();
            while (beaconQuery.MoveNext(out var uid, out var beacon))
            {
                if (beacon.DefaultText == "station-beacon-bridge")
                {
                    bridgeUid = uid;
                    break;
                }
            }

            if (bridgeUid == null)
                return;

            _transform.SetCoordinates(target, Transform(bridgeUid.Value).Coordinates);
        }
    }

    private void PerformHierophant(EntityUid target, bool reversed)
    {
        if (reversed)
        {
            /// Temporarily empty
        }
        else
        {
            var slot = "jumpsuit";
            if (!_inventory.TryGetSlotEntity(target, slot, out var clothing))
                return;

            if (!HasComp<BloodShieldActivaebleComponent>(clothing))
            {
                EnsureComp<BloodShieldActivaebleComponent>(clothing.Value).CurrentSlot = slot;

                var shield = EnsureComp<EnergyShieldOwnerComponent>(target);
                shield.ShieldEntity = Spawn("BloodCultShieldEffect", Transform(target).Coordinates);
                _transform.SetParent(shield.ShieldEntity.Value, target);
            }
        }
    }

    private void PerformLovers(EntityUid target, bool reversed)
    {
        if (reversed)
        {
            var damage = new DamageSpecifier { DamageDict = { { BluntDamage, 20 }, { HeatDamage, 20 } } };
            _damage.TryChangeDamage(target, damage, true);
            _blood.TryModifyBloodLevel(target, -120);
        }
        else
        {
            var damage = new DamageSpecifier { DamageDict = { { BluntDamage, -40 }, { HeatDamage, -40 }, { PoisonDamage, -40 } } };
            _damage.TryChangeDamage(target, damage, true);
            _blood.TryModifyBloodLevel(target, 100);
        }
    }

    private void PerformChariot(EntityUid target, bool reversed)
    {
        if (reversed)
        {
            _polymorph.PolymorphEntity(target, ChariotStatue);
        }
        else
        {
            var time = TimeSpan.FromSeconds(10);
            _statusEffects.TrySetStatusEffectDuration(target, "StatusEffectDesoxyStamina", time);

            bool isStunImmuned = false;
            bool isSlowImmuned = false;
            bool isPacified = false;
            if (!_tag.HasTag(target, StunImmune))
            {
                isStunImmuned = _tag.TryAddTag(target, StunImmune);
            }

            if (!_tag.HasTag(target, SlowImmune))
            {
                isSlowImmuned = _tag.TryAddTag(target, SlowImmune);
            }

            if (!HasComp<PacifiedComponent>(target))
            {
                EnsureComp<PacifiedComponent>(target);
                isPacified = true;
            }

            Timer.Spawn(time, () =>
            {
                if (isStunImmuned) _tag.RemoveTag(target, StunImmune);
                if (isSlowImmuned) _tag.RemoveTag(target, SlowImmune);
                if (isPacified) RemComp<PacifiedComponent>(target);
            });
        }
    }

    private void PerformJustice(EntityUid target, bool reversed)
    {
        if (reversed)
        {
            Spawn(RandomContainer, Transform(target).Coordinates);
        }
        else
        {
            var coords = Transform(target).Coordinates;
            foreach (var ent in JusticeEnt)
                Spawn(ent, coords);
        }
    }

    private void PerformHermit(EntityUid target, bool reversed)
    {
        if (reversed)
        {
            var allEnt = new HashSet<EntityUid>();
            var nearbyGuns = _entityLookup.GetEntitiesInRange<GunComponent>(Transform(target).Coordinates, 6f);
            var nearbyMelees = _entityLookup.GetEntitiesInRange<MeleeWeaponComponent>(Transform(target).Coordinates, 6f)
                .Where(e => !HasComp<MobStateComponent>(e) && !Transform(e).Anchored);
            var nearbyArmor = _entityLookup.GetEntitiesInRange<ArmorComponent>(Transform(target).Coordinates, 6f);
            var nearbyGrenades = _entityLookup.GetEntitiesInRange<TriggerOnUseComponent>(Transform(target).Coordinates, 6f)
                .Where(e => _tag.HasTag(e, Grenade));

            allEnt.UnionWith(nearbyGuns.Select(e => e.Owner));
            allEnt.UnionWith(nearbyMelees.Select(e => e.Owner));
            allEnt.UnionWith(nearbyArmor.Select(e => e.Owner));
            allEnt.UnionWith(nearbyGrenades.Select(e => e.Owner));

            foreach (var ent in allEnt)
            {
                var cash = Spawn(SpaceCash, Transform(ent).Coordinates);
                if (TryComp<StackComponent>(cash, out var stack) && TryComp<StaticPriceComponent>(ent, out var price))
                    _stack.SetCount((cash, stack), (int)price.Price);

                QueueDel(ent);
            }
        }
        else
        {
            var map = Transform(target).MapID;

            var selected = new List<EntityUid>();
            var vendigsQuery = EntityQueryEnumerator<VendingMachineComponent>();
            while (vendigsQuery.MoveNext(out var uid, out _))
            {
                if (map == Transform(uid).MapID)
                    selected.Add(uid);
            }

            if (selected.Count == 0)
                return;

            _transform.SetCoordinates(target, Transform(_random.Pick(selected)).Coordinates);
        }
    }

    private void PerformWheelOfFortune(EntityUid target, bool reversed)
    {
        if (reversed)
        {
            var luck = _random.Next(1, 21);
            _dice.RollFate(target, luck);
        }
        else
        {
            Spawn(RandomVending, Transform(target).Coordinates);
        }
    }

    private void PerformStrength(EntityUid target, bool reversed)
    {
        if (reversed)
        {
            var nearbyEntity = _entityLookup.GetEntitiesInRange<MobStateComponent>(Transform(target).Coordinates, 6f);

            foreach (var entity in nearbyEntity)
            {
                var entityUid = entity.Owner;
                _hallucinations.StartHallucinations(entityUid, "Hallucinations", TimeSpan.FromMinutes(2), true, "MindBreaker");
            }
        }
        else
        {
            if (!TryComp<DamageableComponent>(target, out var damageable))
                return;

            var oldMod = damageable.DamageModifierSetId;
            _damage.SetDamageModifierSetId(target, "VampireBloodSwell");

            Timer.Spawn(TimeSpan.FromSeconds(30), () => { _damage.SetDamageModifierSetId(target, oldMod); });
        }
    }

    private void PerformHangedMan(EntityUid target, bool reversed)
    {
        if (reversed)
        {
            var slotMachine = Spawn(CursedSlotMachine, Transform(target).Coordinates);
            _slotMachine.FreeSpeen(slotMachine, target);
        }
        else
        {
            _gravity.RefreshWeightless(target, false);
            Timer.Spawn(TimeSpan.FromMinutes(1), () => { _gravity.RefreshWeightless(target, true); });
        }
    }

    private void PerformDeath(EntityUid user, EntityUid target, bool reversed)
    {
        if (reversed)
        {
            var coords = Transform(target).Coordinates;
            foreach (var ent in DeathEnt)
                Spawn(ent, coords);
        }
        else
        {
            var nearbyEntity = _entityLookup.GetEntitiesInRange<MobStateComponent>(Transform(target).Coordinates, 6f)
                .Where(e => e.Owner != user);

            var damage = new DamageSpecifier { DamageDict = { { BluntDamage, 20 }, { HeatDamage, 20 } } };
            foreach (var entity in nearbyEntity)
            {
                _damage.TryChangeDamage(entity.Owner, damage, true);
            }
        }
    }

    private void PerformTemperance(EntityUid target, bool reversed)
    {
        if (reversed)
        {
            for (var i = 0; i < 5; i++)
            {
                var pill = Spawn(Pill, Transform(target).Coordinates);
                if (!_ingestion.TryIngest(target, pill))
                    break;
            }
        }
        else
        {
            _statusEffects.TryRemoveStatusEffect(target, Drunk);
            if (TryComp<DiseaseCarrierComponent>(target, out var disease))
                disease.Diseases.Clear();

            if (TryComp<DamageableComponent>(target, out var damageable))
            {
                var damageTypes = new[] { RadiationDamage, PoisonDamage };
                foreach (var damageType in damageTypes)
                {
                    if (damageable.Damage.DamageDict.TryGetValue(damageType, out var currentDamage) && currentDamage > 0)
                    {
                        var healSpecifier = new DamageSpecifier { DamageDict = { { damageType, -currentDamage } } };
                        _damage.TryChangeDamage(target, healSpecifier, true);
                    }
                }
            }
        }
    }

    private void PerformDevil(EntityUid target, bool reversed)
    {
        if (reversed)
        {
            var grenade = Spawn(ClusterBang, Transform(target).Coordinates);
            _trigger.Trigger(grenade);
        }
        else
        {
            var nearbyEntity = _entityLookup.GetEntitiesInRange<DamageableComponent>(Transform(target).Coordinates, 6f)
                .Where(e => e.Owner != target && HasComp<MobStateComponent>(e)).Select(e => e.Owner).ToList();

            var heal = new DamageSpecifier { DamageDict = { { BluntDamage, -45 }, { HeatDamage, -45 } } };
            _damage.TryChangeDamage(target, heal, true);
            _popup.PopupEntity(Loc.GetString("tarot-devil-healed"), target, target);

            Timer.Spawn(TimeSpan.FromSeconds(3), () =>
            {
                if (!Exists(target))
                    return;

                StartDamagePhase(nearbyEntity);
            });
        }
    }

    private void StartDamagePhase(List<EntityUid> initialNearby)
    {
        var validTargets = initialNearby.Where(Exists).ToList();
        if (validTargets.Count == 0)
            return;

        var damageTicks = 30;
        var currentDamageTick = 0;

        var totalBluntPerTarget = 22;
        var totalHeatPerTarget = 23;

        var bluntPerSecondPerTarget = totalBluntPerTarget / (float)damageTicks;
        var heatPerSecondPerTarget = totalHeatPerTarget / (float)damageTicks;

        var damageDealt = new Dictionary<EntityUid, (float Blunt, float Heat)>();
        foreach (var target in validTargets)
        {
            damageDealt[target] = (0, 0);
        }

        void DamageTick()
        {
            validTargets = validTargets.Where(Exists).ToList();
            if (validTargets.Count == 0)
                return;

            foreach (var target in validTargets)
            {
                var expectedBlunt = bluntPerSecondPerTarget * (currentDamageTick + 1);
                var expectedHeat = heatPerSecondPerTarget * (currentDamageTick + 1);

                var (dealtBlunt, dealtHeat) = damageDealt[target];
                var bluntThisTick = (int)(expectedBlunt - dealtBlunt);
                var heatThisTick = (int)(expectedHeat - dealtHeat);

                if (bluntThisTick > 0 || heatThisTick > 0)
                {
                    var damageSpec = new DamageSpecifier();

                    if (bluntThisTick > 0)
                        damageSpec.DamageDict[BluntDamage] = bluntThisTick;

                    if (heatThisTick > 0)
                        damageSpec.DamageDict[HeatDamage] = heatThisTick;

                    _damage.TryChangeDamage(target, damageSpec, true);

                    if (_random.Prob(0.2f))
                    {
                        _popup.PopupEntity(Loc.GetString("tarot-devil-damaged"), target, target);
                    }
                }
            }

            currentDamageTick++;
            if (currentDamageTick < damageTicks)
            {
                Timer.Spawn(TimeSpan.FromSeconds(1), DamageTick);
            }
        }

        DamageTick();
    }

    private void PerformTower(EntityUid target, bool reversed)
    {
        if (reversed)
        {
            var radius = 6;
            var center = Transform(target).Coordinates;
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    if (x * x + y * y <= radius * radius)
                    {
                        if (_random.Prob(0.25f))
                        {
                            var spawnCoords = center.Offset(new Vector2(x, y));
                            Spawn(Rock, spawnCoords);
                        }
                    }
                }
            }
        }
        else
        {
            var grenade = Spawn(ClusterGrenade, Transform(target).Coordinates);
            _trigger.Trigger(grenade);
        }
    }

    private void PerformStars(EntityUid target, bool reversed)
    {
        if (reversed)
        {
            var damage = new DamageSpecifier { DamageDict = { { CellularDamage, 50 } } };
            _damage.TryChangeDamage(target, damage, true);

            var damageTypes = new[] { "ClosedFracture", "ArterialBleeding", "MildBurns" };
            _surgery.TryAddInternalDamage(target, _random.Pick(damageTypes));

            for (var i = 0; i < 2; i++)
            {
                var card = Spawn(EmptyCardTarot, Transform(target).Coordinates);
                var tarot = EnsureComp<CardTarotComponent>(card);

                var allCards = Enum.GetValues<CardTarot>();
                tarot.Card = (CardTarot)_random.Next(1, allCards.Length);

                bool reversedCard = _random.Prob(0.5f);
                if (reversed) tarot.CardType = CardTarotType.Reversed;

                _appearance.SetData(card, CardTarotVisuals.State, tarot.Card);
                _appearance.SetData(card, CardTarotVisuals.Reversed, reversedCard);

                _meta.SetEntityName(card, Loc.GetString("tarot-card-name"));
                _meta.SetEntityDescription(card, Loc.GetString("tarot-card-desc"));

                _throwing.TryThrow(card, _random.NextVector2());
            }
        }
        else
        {
            var lockers = new List<EntityUid>();
            var lockersQuery = EntityQueryEnumerator<EntityStorageComponent, LockComponent, MetaDataComponent>();
            while (lockersQuery.MoveNext(out var uid, out _, out _, out var meta))
            {
                if (meta.EntityPrototype != null && meta.EntityPrototype.ID == "LockerEvidence")
                    lockers.Add(uid);
            }

            if (lockers.Count == 0)
                return;

            var locker = _random.Pick(lockers);
            var lockerCoords = Transform(locker).Coordinates;
            var centerTile = Transform(locker).LocalPosition;

            for (int radiusStep = 1; radiusStep <= 3; radiusStep++)
            {
                for (int i = 0; i < 8; i++)
                {
                    var angle = (float)i / 8 * MathHelper.TwoPi;
                    var offset = new Vector2(
                        (float)Math.Cos(angle) * radiusStep,
                        (float)Math.Sin(angle) * radiusStep);

                    var testPos = centerTile + offset;
                    var testCoords = new EntityCoordinates(lockerCoords.EntityId, testPos);

                    var mapCoords = _transform.ToMapCoordinates(testCoords);
                    var intersecting = _entityLookup.GetEntitiesIntersecting(mapCoords, LookupFlags.Dynamic)
                        .Where(e => e != target).ToList();

                    if (intersecting.Count == 0)
                    {
                        _transform.SetCoordinates(target, testCoords);
                        _lock.Unlock(locker, null);
                        return;
                    }
                }
            }
        }
    }

    private void PerformMoon(EntityUid target, bool reversed)
    {
        if (reversed)
        {
            // Well, like i couldn't think of anything smarter
            var message = Loc.GetString("tarot-moon-m-message");
            if (TryComp<HumanoidAppearanceComponent>(target, out var humanoid) && humanoid.Gender == Gender.Female)
                message = Loc.GetString("tarot-moon-f-message");

            _chat.TrySendInGameICMessage(target, message, InGameICChatType.Speak, false);

        }
        else
        {
            var grids = _mapManager.GetAllGrids(Transform(target).MapID)
                .Where(g => !HasComp<BecomesStationComponent>(g) && !HasComp<ProtectedGridComponent>(g)).ToList();

            if (grids.Count == 0)
                return;

            var randomGrid = _random.Pick(grids);
            _transform.SetCoordinates(target, Transform(randomGrid).Coordinates);
        }
    }

    private void PerformSun(EntityUid target, bool reversed)
    {
        if (reversed)
        {
            if (HasComp<PermanentBlindnessComponent>(target))
                return;

            EnsureComp<PermanentBlindnessComponent>(target).Blindness = 4;
            Timer.Spawn(TimeSpan.FromMinutes(1), () => { RemComp<PermanentBlindnessComponent>(target); });
        }
        else
        {
            _rejuvenate.PerformRejuvenate(target);
        }
    }

    private void PerformJudgement(EntityUid target, bool reversed)
    {
        if (reversed)
        {
            /// Temporarily empty
        }
        else
        {
            // ALL GHOSTS BE MINE!!!
            var ghosts = new List<EntityUid>();
            var ghostsQuery = EntityQueryEnumerator<GhostComponent>();
            while (ghostsQuery.MoveNext(out var uid, out _))
                ghosts.Add(uid);

            foreach (var ghost in ghosts)
                _transform.SetCoordinates(ghost, Transform(target).Coordinates);
        }
    }

    private void PerformTheWorld(EntityUid target, bool reversed)
    {
        if (reversed)
        {
            // He should be doing something else, but that means "Temporarily empty." So it's a reference to JoJo
            var host = EnsureComp<GuardianHostComponent>(target);
            var guardian = Spawn(Stand, Transform(target).Coordinates);

            _container.Insert(guardian, host.GuardianContainer);
            host.HostedGuardian = guardian;

            if (TryComp<GuardianComponent>(guardian, out var guardianComp))
                guardianComp.Host = target;
        }
        else
        {
            Spawn(Smoke, Transform(target).Coordinates);
        }
    }
    #endregion
    #endregion
}
