using System.Linq;
using Content.Server.Body.Systems;
using Content.Shared.Bed.Sleep;
using Content.Shared.Body.Components;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.DirtVisuals;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Surgery;
using Content.Shared.Surgery.Components;
using Content.Shared.Traits.Assorted;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Surgery;

public sealed partial class SurgerySystem
{
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDirtSystem _dirt = default!;
    [Dependency] private readonly SharedSubdermalImplantSystem _implant = default!;
    [Dependency] private readonly SharedInternalStorageSystem _internal = default!;

    private void PerformSurgeryEffect(SurgeryActionType action, string? requiredPart, ProtoId<InternalDamagePrototype>? damageType, float successChance, List<SurgeryFailedType>? failureEffect, EntityUid patient, EntityUid? item)
    {
        if (!TryComp<OperatedComponent>(patient, out var comp))
            return;

        switch (action)
        {
            // Organic Start
            case SurgeryActionType.Cut:
                PerformCut((patient, comp), successChance, failureEffect);
                break;

            case SurgeryActionType.Retract:
                PerformRetract((patient, comp), successChance, failureEffect);
                break;

            case SurgeryActionType.ClampBleeding:
                PerformClamp((patient, comp), successChance, failureEffect);
                break;

            case SurgeryActionType.DrillThrough:
                PerformDrill((patient, comp), successChance, failureEffect);
                break;

            case SurgeryActionType.HealInternalDamage:
                PerformHealInternalDamage((patient, comp), requiredPart, damageType, successChance, failureEffect);
                break;

            case SurgeryActionType.RemoveOrgan:
                PerformRemoveOrgan((patient, comp), requiredPart, successChance, failureEffect);
                break;

            case SurgeryActionType.InsertOrgan:
                PerformInsertOrgan((patient, comp), item, requiredPart, successChance, failureEffect);
                break;

            case SurgeryActionType.RemovePart:
                PerformRemovePart((patient, comp), requiredPart, successChance, failureEffect);
                break;

            case SurgeryActionType.AttachPart:
                PerformAttachPart((patient, comp), item, requiredPart, successChance, failureEffect);
                break;

            case SurgeryActionType.Implanting:
                PerformImplant((patient, comp), item, requiredPart, successChance, failureEffect);
                break;

            case SurgeryActionType.RemoveImplant:
                PerformRemoveImplant((patient, comp), requiredPart, successChance, failureEffect);
                break;

            case SurgeryActionType.StoreItem:
                PerformStoreItem((patient, comp), item, requiredPart, successChance, failureEffect);
                break;

            case SurgeryActionType.RetrieveItems:
                PerformRetrieveItems((patient, comp), requiredPart, successChance, failureEffect);
                break;
            // Organic End

            // Synthetic Start
            case SurgeryActionType.Unscrew:
                PerformUnscrew((patient, comp));
                break;

            case SurgeryActionType.Screw:
                PerformScrew((patient, comp));
                break;

            case SurgeryActionType.Pulse:
                PerformPulse((patient, comp));
                break;

            case SurgeryActionType.Weld:
                PerformWeld((patient, comp));
                break;

            case SurgeryActionType.CutWire:
                PerformCutWire((patient, comp));
                break;

            case SurgeryActionType.StripWire:
                PerformStripWire((patient, comp));
                break;

            case SurgeryActionType.MendWire:
                PerformMendWire((patient, comp));
                break;

            case SurgeryActionType.Pry:
                PerformPry((patient, comp));
                break;

            case SurgeryActionType.Anchor:
                PerformAnchor((patient, comp));
                break;

            case SurgeryActionType.Unanchor:
                PerformUnanchor((patient, comp));
                break;
            // Synthetic End

            default: break;
        }

        // Any action without anesthesia will cause pain.
        if (!HasComp<SleepingComponent>(patient) && !HasComp<PainNumbnessComponent>(patient) && !comp.OperatedPart
            && !_mobState.IsDead(patient) && !HasComp<SyntheticOperatedComponent>(patient))
            _chat.TryEmoteWithoutChat(patient, _proto.Index(Scream), true);
    }

    #region Organic

    private void PerformCut(Entity<OperatedComponent> patient, float successChance, List<SurgeryFailedType>? failureEffect)
    {
        if (patient.Comp.Surgeon == null)
            return;

        if (!RollSuccess(patient, patient.Comp.Surgeon.Value, successChance))
            HandleFailure(patient, failureEffect);

        if (!TryComp<BloodstreamComponent>(patient, out var bloodstream) || HasComp<SyntheticOperatedComponent>(patient))
            return;

        ApplyBloodToClothing(patient.Comp.Surgeon.Value, bloodstream.BloodReagent, 2f * SharedDirtSystem.MaxDirtLevel);
        _bloodstream.TryModifyBleedAmount(patient.Owner, 2f);
    }

    private void PerformRetract(Entity<OperatedComponent> patient, float successChance, List<SurgeryFailedType>? failureEffect)
    {
        if (patient.Comp.Surgeon == null)
            return;

        if (!RollSuccess(patient, patient.Comp.Surgeon.Value, successChance))
            HandleFailure(patient, failureEffect);
    }

    private void PerformClamp(Entity<OperatedComponent> patient, float successChance, List<SurgeryFailedType>? failureEffect)
    {
        if (patient.Comp.Surgeon == null)
            return;

        if (!RollSuccess(patient, patient.Comp.Surgeon.Value, successChance))
        {
            HandleFailure(patient, failureEffect);
            return;
        }

        if (!HasComp<BloodstreamComponent>(patient) || HasComp<SyntheticOperatedComponent>(patient))
            return;

        _bloodstream.TryModifyBleedAmount(patient.Owner, -10f);
    }

    private void PerformDrill(Entity<OperatedComponent> patient, float successChance, List<SurgeryFailedType>? failureEffect)
    {
        if (patient.Comp.Surgeon == null)
            return;

        if (!RollSuccess(patient, patient.Comp.Surgeon.Value, successChance))
            HandleFailure(patient, failureEffect);

        _damage.TryChangeDamage(patient, new DamageSpecifier { DamageDict = { { PiercingDamage, 2.5 } } }, true);
    }

    private void PerformHealInternalDamage(Entity<OperatedComponent> patient, string? requiredPart, ProtoId<InternalDamagePrototype>? damageType, float successChance, List<SurgeryFailedType>? failureEffect)
    {
        if (patient.Comp.Surgeon == null || string.IsNullOrEmpty(requiredPart) || damageType == null)
            return;

        if (!RollSuccess(patient, patient.Comp.Surgeon.Value, successChance))
            HandleFailure(patient, failureEffect, requiredPart);

        if (!patient.Comp.InternalDamages.TryGetValue(damageType.Value, out var damagedParts))
            return;

        damagedParts.Remove(requiredPart);
        if (damagedParts.Count == 0)
        {
            patient.Comp.InternalDamages.Remove(damageType.Value);
        }
    }

    private void PerformRemoveOrgan(Entity<OperatedComponent> patient, string? requiredOrgan, float successChance, List<SurgeryFailedType>? failureEffect)
    {
        if (patient.Comp.Surgeon == null || string.IsNullOrEmpty(requiredOrgan))
            return;

        if (!RollSuccess(patient, patient.Comp.Surgeon.Value, successChance))
            HandleFailure(patient, failureEffect);

        var organs = _body.GetBodyOrgans(patient)
            .Where(o => o.Component.OrganType == requiredOrgan)
            .ToList();

        if (organs.Count == 0)
            return;

        foreach (var (organId, _) in organs)
        {
            _body.RemoveOrgan(organId);
            _popup.PopupEntity(Loc.GetString("surgery-organ-removed"), patient);
            _hands.TryPickupAnyHand(patient.Comp.Surgeon.Value, organId);
        }

        if (TryComp<BloodstreamComponent>(patient, out var bloodstream) && !HasComp<SyntheticOperatedComponent>(patient))
        {
            ApplyBloodToClothing(patient.Comp.Surgeon.Value, bloodstream.BloodReagent, 2f * SharedDirtSystem.MaxDirtLevel);
            _bloodstream.TryModifyBleedAmount(patient.Owner, 2f);
        }
    }

    private void PerformInsertOrgan(Entity<OperatedComponent> patient, EntityUid? item, string? requiredOrgan, float successChance, List<SurgeryFailedType>? failureEffect)
    {
        if (patient.Comp.Surgeon == null || item == null || string.IsNullOrEmpty(requiredOrgan))
            return;

        if (!RollSuccess(patient, patient.Comp.Surgeon.Value, successChance))
            HandleFailure(patient, failureEffect);

        var targetSlot = FindOrganSlot(patient, requiredOrgan);
        if (targetSlot == null)
            return;

        if (_body.InsertOrgan(targetSlot.Value.PartUid, item.Value, targetSlot.Value.SlotId))
        {
            if (!HasComp<SterileComponent>(item.Value) && _random.Prob(0.4f))
                _disease.TryAddDisease(patient, "SurgicalSepsis");

            _popup.PopupEntity(Loc.GetString("surgery-organ-inserted"), patient);
        }
    }

    private void PerformRemovePart(Entity<OperatedComponent> patient, string? requiredPart, float successChance, List<SurgeryFailedType>? failureEffect)
    {
        if (patient.Comp.Surgeon == null || string.IsNullOrEmpty(requiredPart))
            return;

        if (!RollSuccess(patient, patient.Comp.Surgeon.Value, successChance))
            HandleFailure(patient, failureEffect, requiredPart);

        var bodyParts = new List<(EntityUid Id, BodyPartComponent Component)>();

        if (requiredPart.Contains('_'))
        {
            var parts = requiredPart.Split('_');

            if (parts.Length == 2)
            {
                var symmetry = parts[0].ToLower() switch
                {
                    "left" => BodyPartSymmetry.Left,
                    "right" => BodyPartSymmetry.Right,
                    _ => BodyPartSymmetry.None
                };

                var partType = parts[1].ToLower() switch
                {
                    "arm" => BodyPartType.Arm,
                    "hand" => BodyPartType.Hand,
                    "leg" => BodyPartType.Leg,
                    "foot" => BodyPartType.Foot,
                    _ => BodyPartType.Other
                };

                bodyParts = _body.GetBodyChildren(patient)
                    .Where(p => p.Component.PartType == partType && p.Component.Symmetry == symmetry)
                    .ToList();
            }
        }
        else
        {
            var partType = requiredPart.ToLower() switch
            {
                "torso" => BodyPartType.Torso,
                "head" => BodyPartType.Head,
                "tail" => BodyPartType.Tail,
                // To account for primates.
                "hands" => BodyPartType.Hand,
                "legs" => BodyPartType.Leg,
                "feet" => BodyPartType.Foot,
                _ => BodyPartType.Other
            };

            bodyParts = _body.GetBodyChildren(patient)
                .Where(p => p.Component.PartType == partType)
                .ToList();
        }

        if (bodyParts.Count == 0)
            return;

        foreach (var (partId, part) in bodyParts)
        {
            var parentSlot = _body.GetParentPartAndSlotOrNull(partId);
            if (parentSlot == null)
                continue;

            var (parentId, slotId) = parentSlot.Value;
            if (TryComp<BodyPartComponent>(parentId, out var parentPart))
            {
                var containerId = SharedBodySystem.GetPartSlotContainerId(slotId);
                if (_container.TryGetContainer(parentId, containerId, out var container))
                {
                    _container.Remove(partId, container);
                    _popup.PopupEntity(Loc.GetString("surgery-part-removed"), patient);
                    _hands.TryPickupAnyHand(patient.Comp.Surgeon.Value, partId);
                }
            }
        }

        if (TryComp<BloodstreamComponent>(patient, out var bloodstream) && !HasComp<SyntheticOperatedComponent>(patient))
        {
            ApplyBloodToClothing(patient.Comp.Surgeon.Value, bloodstream.BloodReagent, 2f * SharedDirtSystem.MaxDirtLevel);
            _bloodstream.TryModifyBleedAmount(patient.Owner, 2f);
        }
    }

    private void PerformAttachPart(Entity<OperatedComponent> patient, EntityUid? item, string? requiredPart, float successChance, List<SurgeryFailedType>? failureEffect)
    {
        if (patient.Comp.Surgeon == null || item == null || string.IsNullOrEmpty(requiredPart) || !HasComp<BodyPartComponent>(item))
            return;

        if (!RollSuccess(patient, patient.Comp.Surgeon.Value, successChance))
            HandleFailure(patient, failureEffect, requiredPart);

        var slotId = ParseSlotId(requiredPart.ToLower(), SharedBodySystem.PartSlotContainerIdPrefix);
        if (string.IsNullOrEmpty(slotId))
            return;

        var parentPart = _body.GetBodyPartsWithSlot(patient, slotId).FirstOrDefault();
        if (parentPart == EntityUid.Invalid)
            return;

        if (!_body.AttachPart(parentPart, slotId, item.Value))
            return;

        CreateChildSlotsForPart(item.Value, patient);

        if (!HasComp<SterileComponent>(item.Value) && _random.Prob(0.4f))
            _disease.TryAddDisease(patient, "SurgicalSepsis");

        _popup.PopupEntity(Loc.GetString("surgery-part-attached"), patient);
    }

    private void PerformImplant(Entity<OperatedComponent> patient, EntityUid? item, string? requiredPart, float successChance, List<SurgeryFailedType>? failureEffect)
    {
        if (patient.Comp.Surgeon == null || item == null)
            return;

        if (!RollSuccess(patient, patient.Comp.Surgeon.Value, successChance))
            HandleFailure(patient, failureEffect, requiredPart);

        if (!TryComp<SubdermalImplantComponent>(item.Value, out var implantComp))
            return;

        _implant.ForceImplant(patient.Owner, item.Value);
        _admin.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(patient.Comp.Surgeon.Value):user} successfully implanted {ToPrettyString(item.Value):implant} into {ToPrettyString(patient):target}");
    }

    private void PerformRemoveImplant(Entity<OperatedComponent> patient, string? requiredImplant, float successChance, List<SurgeryFailedType>? failureEffect)
    {
        if (patient.Comp.Surgeon == null || string.IsNullOrEmpty(requiredImplant))
            return;

        if (!RollSuccess(patient, patient.Comp.Surgeon.Value, successChance))
            HandleFailure(patient, failureEffect, requiredImplant);

        if (!TryComp<ImplantedComponent>(patient, out var implanted))
            return;

        foreach (var implant in implanted.ImplantContainer.ContainedEntities.ToArray())
        {
            var proto = MetaData(implant).EntityPrototype;
            if (proto != null && proto.ID == requiredImplant)
            {
                if (TryComp<SubdermalImplantComponent>(implant, out var implantComp) && implantComp.Permanent)
                    return;

                _implant.ForceRemove(patient.Owner, implant);
                _hands.TryPickupAnyHand(patient.Comp.Surgeon.Value, implant);
                var ev = new ImplantRemovedEvent(implant, patient.Owner);
                RaiseLocalEvent(patient.Owner, ref ev);

                _admin.Add(LogType.Action, LogImpact.Medium,
                    $"{ToPrettyString(patient.Comp.Surgeon.Value):user} successfully removed {ToPrettyString(implant):implant} from {ToPrettyString(patient):target}");
                break;
            }
        }
    }

    private void PerformStoreItem(Entity<OperatedComponent> patient, EntityUid? item, string? requiredPart, float successChance, List<SurgeryFailedType>? failureEffect)
    {
        if (patient.Comp.Surgeon == null || item == null || string.IsNullOrEmpty(requiredPart) || HasComp<BorgChassisComponent>(patient.Comp.Surgeon))
            return;

        if (!RollSuccess(patient, patient.Comp.Surgeon.Value, successChance))
            HandleFailure(patient, failureEffect);

        // You can't perform a mobs, body part, organ, or implant in this way
        if (HasComp<BodyComponent>(item) || HasComp<BodyPartComponent>(item) || HasComp<OrganComponent>(item)
            || HasComp<SubdermalImplantComponent>(item) || !_internal.TryStoreItem(patient.Owner, item.Value, requiredPart))
            _popup.PopupEntity(Loc.GetString("surgery-store-item-failed"), patient);
        else
            _admin.Add(LogType.Action, LogImpact.Medium,
                $"{ToPrettyString(patient.Comp.Surgeon.Value):user} stored {ToPrettyString(item.Value):item} in {requiredPart} of {ToPrettyString(patient):target}");
    }

    private void PerformRetrieveItems(Entity<OperatedComponent> patient, string? requiredPart, float successChance, List<SurgeryFailedType>? failureEffect)
    {
        if (patient.Comp.Surgeon == null || string.IsNullOrEmpty(requiredPart))
            return;

        if (!RollSuccess(patient, patient.Comp.Surgeon.Value, successChance))
            HandleFailure(patient, failureEffect);

        if (!_internal.TryRemoveItems(patient, requiredPart))
            _popup.PopupEntity(Loc.GetString("surgery-retrieve-items-failed"), patient);
        else
            _admin.Add(LogType.Action, LogImpact.Medium,
                $"{ToPrettyString(patient.Comp.Surgeon.Value):user} retrieved items from {requiredPart} of {ToPrettyString(patient):target}");
    }

    #endregion Organic

    #region Synthetic

    private void PerformUnscrew(Entity<OperatedComponent> patient)
    {
        if (patient.Comp.Surgeon == null || !HasComp<SyntheticOperatedComponent>(patient))
            return;

        _damage.TryChangeDamage(patient, new DamageSpecifier { DamageDict = { { BluntDamage, 2f } } }, true);
        if (TryComp<DamageableComponent>(patient, out var damageable) && damageable.TotalDamage >= 50)
            _audio.PlayPvs(new SoundCollectionSpecifier("sparks"), patient.Owner);
    }

    private void PerformScrew(Entity<OperatedComponent> patient)
    {
        if (patient.Comp.Surgeon == null || !HasComp<SyntheticOperatedComponent>(patient))
            return;

        _damage.TryChangeDamage(patient, new DamageSpecifier { DamageDict = { { BluntDamage, -2f } } }, true);
    }

    private void PerformPulse(Entity<OperatedComponent> patient)
    {
        if (patient.Comp.Surgeon == null || !HasComp<SyntheticOperatedComponent>(patient))
            return;

        _stun.TryAddStunDuration(patient.Owner, TimeSpan.FromSeconds(5));
    }

    private void PerformWeld(Entity<OperatedComponent> patient)
    {
        if (patient.Comp.Surgeon == null || !HasComp<SyntheticOperatedComponent>(patient))
            return;

        var healAmount = new DamageSpecifier { DamageDict = { { BluntDamage, -5f }, { SlashDamage, -5f } } };
        _damage.TryChangeDamage(patient, healAmount, true);

        if (HasComp<BloodstreamComponent>(patient))
            _bloodstream.TryModifyBleedAmount(patient.Owner, -10f);
    }

    private void PerformCutWire(Entity<OperatedComponent> patient)
    {
        if (patient.Comp.Surgeon == null || !HasComp<SyntheticOperatedComponent>(patient))
            return;

        _audio.PlayPvs(new SoundCollectionSpecifier("sparks"), patient.Owner);
        _stun.TryAddStunDuration(patient.Owner, TimeSpan.FromSeconds(1));
    }

    private void PerformStripWire(Entity<OperatedComponent> patient)
    {
        if (patient.Comp.Surgeon == null || !HasComp<SyntheticOperatedComponent>(patient))
            return;

        _damage.TryChangeDamage(patient, new DamageSpecifier { DamageDict = { { SlashDamage, -2f } } }, true);
    }

    private void PerformMendWire(Entity<OperatedComponent> patient)
    {
        if (patient.Comp.Surgeon == null || !HasComp<SyntheticOperatedComponent>(patient))
            return;

        var healAmount = new DamageSpecifier
        {
            DamageDict = { { BluntDamage, -6f }, { SlashDamage, -6f }, { PiercingDamage, -6f }, { HeatDamage, -6f } }
        };
        _damage.TryChangeDamage(patient, healAmount, true);
    }

    private void PerformPry(Entity<OperatedComponent> patient)
    {
        if (patient.Comp.Surgeon == null || !HasComp<SyntheticOperatedComponent>(patient))
            return;

        _damage.TryChangeDamage(patient, new DamageSpecifier { DamageDict = { { BluntDamage, 5f } } }, true);
    }

    private void PerformAnchor(Entity<OperatedComponent> patient)
    {
        if (patient.Comp.Surgeon == null || !HasComp<SyntheticOperatedComponent>(patient))
            return;

        _damage.TryChangeDamage(patient, new DamageSpecifier { DamageDict = { { BluntDamage, -8f } } }, true);
    }

    private void PerformUnanchor(Entity<OperatedComponent> patient)
    {
        if (patient.Comp.Surgeon == null || !HasComp<SyntheticOperatedComponent>(patient))
            return;

        _damage.TryChangeDamage(patient, new DamageSpecifier { DamageDict = { { BluntDamage, 4f } } }, true);
        if (_random.Prob(0.3f))
            _audio.PlayPvs(new SoundCollectionSpecifier("sparks"), patient.Owner);
    }

    #endregion Synthetic

    #region Other Logic

    private bool RollSuccess(Entity<OperatedComponent> ent, EntityUid surgeon, float baseChance)
    {
        var item = _hands.GetActiveItemOrSelf(surgeon);
        if (HasComp<SurgicalSkillComponent>(surgeon) && ent.Comp.Sterility == 1f && HasComp<SterileComponent>(item)
            && SurgeryTools.Any(tool => _tool.HasQuality(item, tool))
            || Organs.Any(tag => _tag.HasTag(item, tag))
            || Parts.Any(tag => _tag.HasTag(item, tag)))
        {
            return true;
        }

        var adjustedChance = baseChance * Math.Clamp(ent.Comp.Sterility, 0f, 1.5f);
        if (TryGetOperatingTable(ent, out var tableModifier))
            adjustedChance *= tableModifier;

        return _random.Prob(adjustedChance);
    }

    private (EntityUid PartUid, string SlotId)? FindOrganSlot(EntityUid bodyId, string organType)
    {
        foreach (var part in _body.GetBodyChildren(bodyId))
        {
            foreach (var (slotId, _) in part.Component.Organs)
            {
                if (slotId.Equals(organType, StringComparison.OrdinalIgnoreCase))
                {
                    return (part.Id, slotId);
                }
            }
        }
        return null;
    }

    private string? ParseSlotId(string? fullSlotId, string prefix)
    {
        if (string.IsNullOrEmpty(fullSlotId))
            return null;

        return fullSlotId.StartsWith(prefix)
            ? fullSlotId.Substring(prefix.Length)
            : fullSlotId;
    }

    private void CreateChildSlotsForPart(EntityUid attachedPart, Entity<OperatedComponent> patient)
    {
        if (!TryComp<BodyPartComponent>(attachedPart, out var partComp))
            return;

        var defaultSlots = GetDefaultSlotsForPartType(partComp.PartType, partComp.Symmetry);
        foreach (var slotId in defaultSlots)
        {
            var containerId = SharedBodySystem.GetPartSlotContainerId(slotId);
            if (!_container.TryGetContainer(attachedPart, containerId, out _))
            {
                CreateSlotInPart(attachedPart, slotId, GetChildPartTypeForSlot(slotId));
            }
        }
    }

    private void CreateSlotInPart(EntityUid part, string slotId, BodyPartType slotType)
    {
        if (!TryComp<BodyPartComponent>(part, out var partComp))
            return;

        _body.TryCreatePartSlot(part, slotId, slotType, out _, partComp);
    }

    private List<string> GetDefaultSlotsForPartType(BodyPartType partType, BodyPartSymmetry symmetry)
    {
        var slots = new List<string>();
        switch (partType)
        {
            case BodyPartType.Arm:
                slots.Add(symmetry == BodyPartSymmetry.Left ? "left_hand" : "right_hand");
                break;
            case BodyPartType.Leg:
                slots.Add(symmetry == BodyPartSymmetry.Left ? "left_foot" : "right_foot");
                break;
            case BodyPartType.Torso:
                slots.AddRange(new[] { "left_arm", "right_arm", "left_leg", "right_leg", "head" });
                break;
        }

        return slots;
    }

    private BodyPartType GetChildPartTypeForSlot(string slotId)
    {
        return slotId.ToLower() switch
        {
            var s when s.Contains("hand") => BodyPartType.Hand,
            var s when s.Contains("foot") => BodyPartType.Foot,
            var s when s.Contains("arm") => BodyPartType.Arm,
            var s when s.Contains("leg") => BodyPartType.Leg,
            var s when s.Contains("head") => BodyPartType.Head,
            _ => BodyPartType.Other
        };
    }

    public void ApplyBloodToClothing(EntityUid surgeon, string bloodReagentId, float bloodAmount)
    {
        var bloodSolution = new Solution();
        bloodSolution.AddReagent(bloodReagentId, FixedPoint2.New(bloodAmount));

        var clothingSlots = new[] { "outerClothing", "jumpsuit", "gloves" };
        _dirt.ApplyDirtToClothing(surgeon, bloodSolution, _random.Pick(clothingSlots));
    }

    private void HandleFailure(Entity<OperatedComponent> patient, List<SurgeryFailedType>? failureEffect, string? bodyPart = null)
    {
        if (failureEffect == null || failureEffect.Count == 0)
            return;

        var effect = _random.Pick(failureEffect);
        switch (effect)
        {
            case SurgeryFailedType.Empty:
                return;
            case SurgeryFailedType.Cut:
                _damage.TryChangeDamage(patient, new DamageSpecifier { DamageDict = { { SlashDamage, 5 } } }, true);
                break;
            case SurgeryFailedType.Bleeding:
                TryAddInternalDamage(patient, "ArterialBleeding", bodyPart: bodyPart);
                break;
            case SurgeryFailedType.Burn:
                _damage.TryChangeDamage(patient, new DamageSpecifier { DamageDict = { { HeatDamage, 5 } } }, true);
                break;
            case SurgeryFailedType.Fracture:
                TryAddInternalDamage(patient, "BoneFracture", bodyPart: bodyPart);
                break;
            case SurgeryFailedType.Pain:
                if (!HasComp<SleepingComponent>(patient) && !HasComp<PainNumbnessComponent>(patient) && !patient.Comp.OperatedPart && !_mobState.IsDead(patient) && !HasComp<SyntheticOperatedComponent>(patient))
                    _chat.TryEmoteWithoutChat(patient, _proto.Index(Scream), true);

                _jittering.DoJitter(patient, TimeSpan.FromSeconds(5), true);
                break;
        }

        if (effect != SurgeryFailedType.Empty && !_mobState.IsDead(patient))
            _popup.PopupPredicted(Loc.GetString($"surgery-handle-failed-{effect.ToString().ToLower()}", ("patient", Identity.Entity(patient, EntityManager))),
                patient, null, PopupType.MediumCaution);
    }

    #endregion
}
