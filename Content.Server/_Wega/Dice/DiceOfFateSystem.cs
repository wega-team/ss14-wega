using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Antag;
using Content.Server.Body.Systems;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Polymorph.Systems;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Administration.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.Dice;
using Content.Shared.Disease;
using Content.Shared.Explosion;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.PDA;
using Content.Shared.Polymorph;
using Content.Shared.Popups;
using Content.Shared.Roles.Components;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Dice;

public sealed class DiceOfFateSystem : EntitySystem
{
    [Dependency] private readonly SharedAccessSystem _access = default!;
    [Dependency] private readonly IAdminLogManager _admin = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly SharedDiseaseSystem _disease = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speed = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RejuvenateSystem _rejuvenate = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DiceOfFateComponent, UseInHandEvent>(OnUseInHand, after: [typeof(SharedDiceSystem)]);
        SubscribeLocalEvent<DiceOfFateComponent, LandEvent>(OnLand, after: [typeof(SharedDiceSystem)]);
    }

    private static readonly ProtoId<DamageModifierSetPrototype> DamageMod = "DiceOfFateMod";
    private static readonly ProtoId<PolymorphPrototype> Monkey = "Monkey";
    private static readonly ProtoId<DiseasePrototype> Cold = "SpaceCold";
    private static readonly ProtoId<DamageTypePrototype> Damage = "Asphyxiation";
    private static readonly EntProtoId RandomAgressive = "RandomAgressiveAnimal";
    private static readonly EntProtoId RandomSpellbook = "RandomSpellbook";
    private static readonly EntProtoId Revolver = "WeaponRevolverInspector";
    private static readonly EntProtoId DefaultWizardRule = "Wizard";
    private static readonly EntProtoId Cookie = "FoodBakedCookie";
    private static readonly EntProtoId Servant = "PlushieLizard";
    private static readonly EntProtoId Toolbox = "ToolboxThief";
    private static readonly EntProtoId Cash = "SpaceCash10000";

    private void OnUseInHand(Entity<DiceOfFateComponent> entity, ref UseInHandEvent args)
    {
        if (!TryComp<DiceComponent>(entity, out var dice) || entity.Comp.Used)
            return;

        entity.Comp.Used = true;
        RollFate(args.User, dice.CurrentValue);
        Timer.Spawn(TimeSpan.FromSeconds(1), () => { QueueDel(entity); }); // So that you can see the number
    }

    private void OnLand(Entity<DiceOfFateComponent> entity, ref LandEvent args)
    {
        if (args.User == null || !TryComp<DiceComponent>(entity, out var dice)
            || entity.Comp.Used)
            return;

        entity.Comp.Used = true;
        RollFate(args.User.Value, dice.CurrentValue);
        Timer.Spawn(TimeSpan.FromSeconds(1), () => { QueueDel(entity); }); // So that you can see the number
    }

    public void RollFate(EntityUid user, int value)
    {
        var success = value switch
        {
            1 => CompleteAnnihilation(user),
            2 => InstantDeath(user),
            3 => SummonAggressiveCreatures(user),
            4 => DestroyAllEquippedItems(user),
            5 => TransformIntoMonkey(user),
            6 => PermanentMovementSpeedReduction(user),
            7 => StunAndDamage(user),
            8 => ExplosionUser(user),
            9 => CommonCold(user),
            10 => NothingHappens(user),
            11 => SpawnCookie(user),
            12 => FullHealthRestoration(user),
            13 => SpawnMoney(user),
            14 => SpawnRevolver(user),
            15 => SpawnSpellbook(user),
            16 => SummonServant(user),
            17 => SuspiciousBeacon(user),
            18 => FullAccess(user),
            19 => PermanentDamageReduction(user),
            20 => BecomeWizard(user),
            _ => NothingHappens(user)
        };

        _admin.Add(LogType.Action, LogImpact.Extreme, $"{ToPrettyString(user):user} rools fade is '{success}', got nimber {value}.");
    }

    private bool CompleteAnnihilation(EntityUid user)
    {
        _body.GibBody(user, true, splatModifier: 10f);
        return true;
    }

    private bool InstantDeath(EntityUid user)
    {
        var damage = new DamageSpecifier { DamageDict = { { Damage, 400 } } };
        _damage.ChangeDamage(user, damage, true);
        return true;
    }

    private bool SummonAggressiveCreatures(EntityUid user)
    {
        var count = _random.Next(3, 6);
        for (var i = 0; i < count; i++)
        {
            Spawn(RandomAgressive, Transform(user).Coordinates);
        }

        return true;
    }

    private bool DestroyAllEquippedItems(EntityUid user)
    {
        if (_inventory.TryGetSlots(user, out var slots))
        {
            foreach (var slot in slots)
            {
                if (_inventory.TryGetSlotEntity(user, slot.Name, out var ent))
                    QueueDel(ent);
            }
        }

        return true;
    }

    private bool TransformIntoMonkey(EntityUid user)
    {
        _polymorph.PolymorphEntity(user, Monkey);
        return true;
    }

    private bool PermanentMovementSpeedReduction(EntityUid user)
    {
        if (TryComp(user, out MovementSpeedModifierComponent? speedmod))
        {
            var originalWalkSpeed = speedmod.BaseWalkSpeed;
            var originalSprintSpeed = speedmod.BaseSprintSpeed;

            var multiplier = _random.NextFloat(0.3f, 0.95f);
            _speed.ChangeBaseSpeed(user, originalWalkSpeed * multiplier, originalSprintSpeed * multiplier, speedmod.Acceleration, speedmod);
        }

        return true;
    }

    private bool StunAndDamage(EntityUid user)
    {
        _stun.TryKnockdown(user, TimeSpan.FromSeconds(30));
        var damage = new DamageSpecifier { DamageDict = { { Damage, 50 } } };
        _damage.ChangeDamage(user, damage, true);

        return true;
    }

    private bool ExplosionUser(EntityUid user)
    {
        if (!_prototype.TryIndex(ExplosionSystem.DefaultExplosionPrototypeId, out ExplosionPrototype? type))
            return false;

        _explosion.QueueExplosion(user, type.ID, 5000f, 3f, 10f);
        return true;
    }

    private bool CommonCold(EntityUid user)
    {
        _disease.TryAddDisease(user, Cold);
        return true;
    }

    private bool NothingHappens(EntityUid user)
    {
        // I'm Lazy
        _popup.PopupEntity(Loc.GetString("reagent-desc-nothing"), user, user);
        return true;
    }

    private bool SpawnCookie(EntityUid user)
    {
        var cookie = Spawn(Cookie, Transform(user).Coordinates);
        _hands.TryForcePickupAnyHand(user, cookie);

        return true;
    }

    private bool FullHealthRestoration(EntityUid user)
    {
        _rejuvenate.PerformRejuvenate(user);
        return true;
    }

    private bool SpawnMoney(EntityUid user)
    {
        var cash = Spawn(Cash, Transform(user).Coordinates);
        _hands.TryForcePickupAnyHand(user, cash);

        return true;
    }

    private bool SpawnRevolver(EntityUid user)
    {
        var revolver = Spawn(Revolver, Transform(user).Coordinates);
        _hands.TryForcePickupAnyHand(user, revolver);

        return true;
    }

    private bool SpawnSpellbook(EntityUid user)
    {
        var spellbook = Spawn(RandomSpellbook, Transform(user).Coordinates);
        _hands.TryForcePickupAnyHand(user, spellbook);

        return true;
    }

    // Very goodluck
    private bool SummonServant(EntityUid user)
    {
        var servant = Spawn(Servant, Transform(user).Coordinates);
        _hands.TryForcePickupAnyHand(user, servant);

        return true;
    }

    private bool SuspiciousBeacon(EntityUid user)
    {
        var toolbox = Spawn(Toolbox, Transform(user).Coordinates);
        _hands.TryForcePickupAnyHand(user, toolbox);

        return true;
    }

    private bool FullAccess(EntityUid user)
    {
        var ent = FindActiveId(user);
        if (ent == null)
            return false;

        GiveAllAccess(ent.Value);
        return true;
    }

    private EntityUid? FindActiveId(EntityUid target)
    {
        if (_inventory.TryGetSlotEntity(target, "id", out var slotEntity))
        {
            if (HasComp<AccessComponent>(slotEntity))
            {
                return slotEntity.Value;
            }
            else if (TryComp<PdaComponent>(slotEntity, out var pda)
                && HasComp<IdCardComponent>(pda.ContainedId))
            {
                return pda.ContainedId;
            }
        }
        else if (TryComp<HandsComponent>(target, out var hands))
        {
            foreach (var held in _hands.EnumerateHeld((target, hands)))
            {
                if (HasComp<AccessComponent>(held))
                {
                    return held;
                }
            }
        }

        return null;
    }

    private void GiveAllAccess(EntityUid entity)
    {
        var allAccess = _prototype
            .EnumeratePrototypes<AccessLevelPrototype>()
            .Select(p => new ProtoId<AccessLevelPrototype>(p.ID)).ToArray();

        _access.TrySetTags(entity, allAccess);
    }

    private bool PermanentDamageReduction(EntityUid user)
    {
        _damage.SetDamageModifierSetId(user, DamageMod);
        return true;
    }

    private bool BecomeWizard(EntityUid user)
    {
        if (!TryComp<ActorComponent>(user, out var actor))
            return false;

        _antag.ForceMakeAntag<WizardRoleComponent>(actor.PlayerSession, DefaultWizardRule);
        return true;
    }
}
