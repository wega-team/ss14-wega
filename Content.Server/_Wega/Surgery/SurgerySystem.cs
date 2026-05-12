using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Systems;
using Content.Server.Disease;
using Content.Shared.Body;
using Content.Shared.Buckle.Components;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Jittering;
using Content.Shared.Mobs.Systems;
using Content.Shared.Modular.Suit;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Content.Shared.Stunnable;
using Content.Shared.Surgery.Components;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Content.Shared.Tools;
using Content.Shared.Tools.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Surgery;

public sealed partial class SurgerySystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _admin = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly DiseaseSystem _disease = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedJitteringSystem _jittering = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    private static readonly ProtoId<EmotePrototype> Scream = "Scream";
    private static readonly ProtoId<DamageTypePrototype> BluntDamage = "Blunt";
    private static readonly ProtoId<DamageTypePrototype> SlashDamage = "Slash";
    private static readonly ProtoId<DamageTypePrototype> PiercingDamage = "Piercing";
    private static readonly ProtoId<DamageTypePrototype> HeatDamage = "Heat";

    private static readonly List<ProtoId<ToolQualityPrototype>> SurgeryTools = new()
    {
        "Scalpel",
        "Hemostat",
        "Retractor",
        "Cautery",
        "Sawing",
        "Drilling",
        "FixOVein",
        "BoneGel",
        "BoneSetter"
    };

    private static readonly List<ProtoId<TagPrototype>> Organs = new()
    {
        "BaseOrgan"
    };

    private static readonly List<ProtoId<TagPrototype>> Parts = new()
    {
        "BaseBodyPart",
        "SubdermalImplant",
        "SubdermalHeadImplant"
    };

    public override void Initialize()
    {
        base.Initialize();

        GraphsInitialize();
        InternalDamageInitialize();
        UiInitialize();

        SubscribeLocalEvent<OperatedComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<OperatedComponent, StandUpAttemptEvent>(OnStandUpAttempt);
        SubscribeLocalEvent<OperatedComponent, IsEquippingAttemptEvent>(OnIsEquipping);

        SubscribeLocalEvent<SterileComponent, ExaminedEvent>(OnSterileExamined);
        SubscribeLocalEvent<SterileComponent, ThrownEvent>(OnThrow);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<OperatedComponent>();
        while (query.MoveNext(out var uid, out var operated))
        {
            if (operated.OperatedPart)
                continue;

            if (operated.NextUpdateTick <= 0)
            {
                if (operated.InternalDamages.Count != 0 && !_mobState.IsDead(uid))
                {
                    ProcessInternalDamages(uid, operated);
                }

                if (operated.IsOperating)
                {
                    UpdateOperationSterility(uid, operated);
                }

                operated.NextUpdateTick = 5f;
            }
            operated.NextUpdateTick -= frameTime;

            if (operated.LimbRegeneration && !_mobState.IsDead(uid))
            {
                if (operated.NextRegenerationTick <= 0)
                {
                    RegenerateMissingLimbs((uid, operated));
                    operated.NextRegenerationTick = operated.RegenerationInterval;
                }
                operated.NextRegenerationTick -= frameTime;
            }
        }

        var sterileQuery = EntityQueryEnumerator<SterileComponent>();
        while (sterileQuery.MoveNext(out var uid, out var sterile))
        {
            if (sterile.AlwaysSterile)
                continue;

            if (sterile.NextUpdateTick <= 0)
            {
                sterile.NextUpdateTick = 5f;
                sterile.Amount -= sterile.DecayRate;
                if (sterile.Amount <= 0)
                {
                    RemComp<SterileComponent>(uid);
                }
            }
            sterile.NextUpdateTick -= frameTime;
        }
    }

    private void OnRejuvenate(Entity<OperatedComponent> ent, ref RejuvenateEvent args)
    {
        ent.Comp.InternalDamages.Clear();
        ent.Comp.ResetOperationState("Default");
        RestoreMissingOrgans(ent);
    }

    private void OnStandUpAttempt(Entity<OperatedComponent> ent, ref StandUpAttemptEvent args)
    {
        if (!TryComp<BodyComponent>(ent, out var body) || body.Organs == null)
            return;

        if (!TryComp<InitialBodyComponent>(ent, out var initialBody))
            return;

        var existingOrgans = new HashSet<ProtoId<OrganCategoryPrototype>>();
        foreach (var organ in body.Organs.ContainedEntities)
        {
            if (TryComp<OrganComponent>(organ, out var organComp) && organComp.Category != null)
                existingOrgans.Add(organComp.Category.Value);
        }

        var requiredLegs = new List<string>();
        foreach (var (category, _) in initialBody.Organs)
        {
            var categoryId = category.Id;
            if (categoryId.Contains("Leg"))
            {
                requiredLegs.Add(categoryId);
            }
        }

        if (requiredLegs.Count == 0)
            return;

        int missingLegs = 0;
        foreach (var leg in requiredLegs)
        {
            if (!existingOrgans.Contains(leg))
                missingLegs++;
        }

        if (missingLegs >= 1)
        {
            args.Cancelled = true;
            args.Autostand = false;
        }
    }

    private void RestoreMissingOrgans(Entity<OperatedComponent> entity)
    {
        if (!TryComp<BodyComponent>(entity, out var body) || body.Organs == null)
            return;

        // Get all current organs
        var existingOrgans = new HashSet<ProtoId<OrganCategoryPrototype>>();
        foreach (var organ in body.Organs.ContainedEntities)
        {
            if (TryComp<OrganComponent>(organ, out var organComp) && organComp.Category != null)
                existingOrgans.Add(organComp.Category.Value);
        }

        // Get the initial body configuration
        if (!TryComp<InitialBodyComponent>(entity, out var initialBody))
            return;

        foreach (var (organCategory, organProto) in initialBody.Organs)
        {
            if (existingOrgans.Contains(organCategory))
                continue;

            // Spawn missing organ
            var newOrgan = Spawn(organProto, Transform(entity).Coordinates);
            _container.Insert(newOrgan, body.Organs);

            var insertedEvent = new OrganInsertedIntoEvent(newOrgan);
            RaiseLocalEvent(entity, ref insertedEvent);

            var gotInsertedEvent = new OrganGotInsertedEvent(entity);
            RaiseLocalEvent(newOrgan, ref gotInsertedEvent);
        }
    }

    private void RegenerateMissingLimbs(Entity<OperatedComponent> entity)
    {
        if (!TryComp<BodyComponent>(entity, out var body) || body.Organs == null)
            return;

        if (!TryComp<InitialBodyComponent>(entity, out var initialBody))
            return;

        // Get current organs
        var existingOrgans = new HashSet<ProtoId<OrganCategoryPrototype>>();
        foreach (var organ in body.Organs.ContainedEntities)
        {
            if (TryComp<OrganComponent>(organ, out var organComp) && organComp.Category != null)
                existingOrgans.Add(organComp.Category.Value);
        }

        var missingLimbs = initialBody.Organs
            .Where(kvp => !existingOrgans.Contains(kvp.Key))
            .ToList();

        if (missingLimbs.Count == 0)
            return;

        var limbsToRegenerate = missingLimbs
            .OrderBy(_ => _random.Next())
            .Take(entity.Comp.MaxLimbsPerCycle)
            .ToList();

        foreach (var (organCategory, organProto) in limbsToRegenerate)
        {
            var newOrgan = Spawn(organProto, Transform(entity).Coordinates);
            _container.Insert(newOrgan, body.Organs);

            var insertedEvent = new OrganInsertedIntoEvent(newOrgan);
            RaiseLocalEvent(entity, ref insertedEvent);

            var gotInsertedEvent = new OrganGotInsertedEvent(entity);
            RaiseLocalEvent(newOrgan, ref gotInsertedEvent);

            _popup.PopupEntity(Loc.GetString("surgery-limb-regenerated"), entity, entity);
        }
    }

    private void OnIsEquipping(Entity<OperatedComponent> ent, ref IsEquippingAttemptEvent args)
    {
        if (HasComp<ModularSuitPartComponent>(args.Equipment))
            return;

        if ((args.SlotFlags == SlotFlags.FEET || args.SlotFlags == SlotFlags.SOCKS)
            && (!HasRequiredOrgans(ent, "FootLeft", "FootRight") && !HasRequiredOrgans(ent, "LegLeft", "LegRight")))
        {
            args.Cancel();
            return;
        }

        if (args.SlotFlags == SlotFlags.GLOVES
            && (!HasRequiredOrgans(ent, "HandLeft", "HandRight") && !HasRequiredOrgans(ent, "ArmLeft", "ArmRight")))
        {
            args.Cancel();
            return;
        }

        if ((args.SlotFlags == SlotFlags.HEAD || args.SlotFlags == SlotFlags.EYES || args.SlotFlags == SlotFlags.EARS ||
            args.SlotFlags == SlotFlags.MASK) && !HasRequiredOrgans(ent, "Head"))
            args.Cancel();
    }

    public void CheckAndRemoveInvalidClothing(Entity<OperatedComponent> ent)
    {
        if (!HasRequiredOrgans(ent, "LeftLeg", "RightLeg") && !HasRequiredOrgans(ent, "LeftFoot", "RightFoot"))
        {
            _inventory.TryUnequip(ent, "shoes", force: true);
            _inventory.TryUnequip(ent, "socks", force: true);
        }

        if (!HasRequiredOrgans(ent, "LeftArm", "RightArm") && !HasRequiredOrgans(ent, "LeftHand", "RightHand"))
            _inventory.TryUnequip(ent, "gloves", force: true);

        if (!HasRequiredOrgans(ent, "Head"))
        {
            string[] headSlots = { "head", "mask", "eyes", "ears" };
            foreach (var slot in headSlots)
            {
                _inventory.TryUnequip(ent, slot, force: true);
            }
        }
    }

    private bool HasRequiredOrgans(EntityUid uid, params string[] organCategories)
    {
        if (!TryComp<BodyComponent>(uid, out var body) || body.Organs == null)
            return false;

        var existingCategories = new HashSet<ProtoId<OrganCategoryPrototype>>();
        foreach (var organ in body.Organs.ContainedEntities)
        {
            if (TryComp<OrganComponent>(organ, out var organComp) && organComp.Category != null)
                existingCategories.Add(organComp.Category.Value);
        }

        foreach (var category in organCategories)
        {
            if (!existingCategories.Contains(category))
                return false;
        }
        return true;
    }

    private void OnSterileExamined(Entity<SterileComponent> entity, ref ExaminedEvent args)
    {
        if (args.IsInDetailsRange)
            args.AddMarkup(Loc.GetString("surgery-sterile-examined") + "\n");
    }

    private void OnThrow(Entity<SterileComponent> entity, ref ThrownEvent args)
        => RemCompDeferred<SterileComponent>(entity);

    private bool TryGetOperatingTable(EntityUid patient, out float tableModifier)
    {
        tableModifier = 1f;
        if (!TryComp<BuckleComponent>(patient, out var buckle) || buckle.BuckledTo == null
            || HasComp<SyntheticOperatedComponent>(patient))
            return false;

        return TryComp<OperatingTableComponent>(buckle.BuckledTo.Value, out var operating) &&
            (tableModifier = operating.Modifier) > 0;
    }
}
