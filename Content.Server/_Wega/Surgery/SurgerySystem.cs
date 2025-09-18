using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Systems;
using Content.Server.Disease;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Prototypes;
using Content.Shared.Body.Systems;
using Content.Shared.Buckle.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Jittering;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Content.Shared.Surgery.Components;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Content.Shared.Tools;
using Content.Shared.Tools.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Surgery;

public sealed partial class SurgerySystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _admin = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly DiseaseSystem _disease = default!;
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
        SubscribeLocalEvent<OperatedComponent, IsEquippingAttemptEvent>(OnIsEquipping);
        SubscribeLocalEvent<OperatedComponent, BodyPartRemovedEvent>(OnBodyPartsChanged);

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
        RestoreMissingLimbs(ent);
    }

    private void RestoreMissingLimbs(Entity<OperatedComponent> entity)
    {
        if (!TryComp<BodyComponent>(entity, out var body) || body.Prototype == null)
            return;

        var rootPart = _body.GetRootPartOrNull(entity, body);
        if (rootPart == null)
            return;

        var prototype = _proto.Index(body.Prototype.Value);
        foreach (var (slotId, slot) in prototype.Slots)
        {
            if (slotId == prototype.Root)
                continue;

            bool partExists = false;
            foreach (var part in _body.GetBodyChildren(entity, body))
            {
                var parentAndSlot = _body.GetParentPartAndSlotOrNull(part.Id);
                if (parentAndSlot != null && parentAndSlot.Value.Slot == slotId)
                {
                    partExists = true;
                    break;
                }
            }

            if (!partExists && slot.Part != null)
            {
                var newPart = Spawn(slot.Part, Transform(entity).Coordinates);
                var newPartComp = Comp<BodyPartComponent>(newPart);

                var parentSlot = prototype.Slots.FirstOrDefault(s => s.Value.Connections.Contains(slotId));
                if (parentSlot.Value != null)
                {
                    foreach (var parentPart in _body.GetBodyChildren(entity, body))
                    {
                        if (_body.CanAttachPart(parentPart.Id, slotId, newPart, parentPart.Component, newPartComp))
                        {
                            _body.AttachPart(parentPart.Id, slotId, newPart, parentPart.Component, newPartComp);
                            break;
                        }
                    }
                }

                if (slot.Organs != null)
                {
                    foreach (var (organSlot, organPrototype) in slot.Organs)
                    {
                        var newOrgan = Spawn(organPrototype, Transform(entity).Coordinates);
                        _body.InsertOrgan(newOrgan, newPart, organSlot);
                    }
                }
            }
        }
    }

    private void OnIsEquipping(Entity<OperatedComponent> ent, ref IsEquippingAttemptEvent args)
    {
        if ((args.SlotFlags == SlotFlags.FEET || args.SlotFlags == SlotFlags.SOCKS) &&
            (!HasRequiredLimbs(ent, BodyPartType.Leg) || !HasRequiredLimbs(ent, BodyPartType.Foot)))
        {
            args.Cancel();
            return;
        }

        if (args.SlotFlags == SlotFlags.GLOVES &&
            (!HasRequiredLimbs(ent, BodyPartType.Arm) || !HasRequiredLimbs(ent, BodyPartType.Hand)))
        {
            args.Cancel();
            return;
        }

        if ((args.SlotFlags == SlotFlags.HEAD || args.SlotFlags == SlotFlags.EYES || args.SlotFlags == SlotFlags.EARS ||
            args.SlotFlags == SlotFlags.MASK) && !_body.GetBodyChildrenOfType(ent, BodyPartType.Head).Any())
            args.Cancel();
    }

    private void OnBodyPartsChanged(Entity<OperatedComponent> ent, ref BodyPartRemovedEvent args)
    {
        OnBodyPartRemoved(ent, args.Part.Comp.PartType);
        CheckAndRemoveInvalidClothing(ent);
    }

    public void CheckAndRemoveInvalidClothing(Entity<OperatedComponent> ent)
    {
        if (!HasRequiredLimbs(ent, BodyPartType.Leg) || !HasRequiredLimbs(ent, BodyPartType.Foot))
        {
            _inventory.TryUnequip(ent, "shoes", force: true);
            _inventory.TryUnequip(ent, "socks", force: true);
        }

        if (!HasRequiredLimbs(ent, BodyPartType.Arm) || !HasRequiredLimbs(ent, BodyPartType.Hand))
            _inventory.TryUnequip(ent, "gloves", force: true);

        if (!_body.GetBodyChildrenOfType(ent, BodyPartType.Head).Any())
        {
            string[] headSlots = { "head", "mask", "eyes", "ears" };
            foreach (var slot in headSlots)
            {
                _inventory.TryUnequip(ent, slot, force: true);
            }
        }
    }

    private bool HasRequiredLimbs(EntityUid uid, BodyPartType partType)
    {
        var parts = _body.GetBodyChildrenOfType(uid, partType).ToList();

        bool hasLeft = parts.Any(p => p.Component.Symmetry == BodyPartSymmetry.Left);
        bool hasRight = parts.Any(p => p.Component.Symmetry == BodyPartSymmetry.Right);

        if (partType == BodyPartType.Head)
            return parts.Count > 0;

        return hasLeft && hasRight;
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
