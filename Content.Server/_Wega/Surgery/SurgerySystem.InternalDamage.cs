using System.Linq;
using System.Text;
using Content.Server.Body.Components;
using Content.Server.Pain;
using Content.Shared.Armor;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Surgery;
using Content.Shared.Surgery.Components;
using Content.Shared.Traits.Assorted;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Surgery;

public sealed partial class SurgerySystem
{
    [Dependency] private readonly PainSystem _pain = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly MovementModStatusSystem _movementMod = default!;

    private static readonly SoundSpecifier GibSound = new SoundPathSpecifier("/Audio/Effects/gib3.ogg");

    private void InternalDamageInitialize()
    {
        SubscribeLocalEvent<OperatedComponent, DamageChangedEvent>(OnDamage);
        SubscribeLocalEvent<OperatedComponent, ExaminedEvent>(OnOperatedExamined);
    }

    #region Process damage

    private void OnDamage(Entity<OperatedComponent> ent, ref DamageChangedEvent args)
    {
        if (HasComp<GodmodeComponent>(ent))
            return;

        if (args.DamageDelta == null || args.DamageDelta.Empty || !args.DamageIncreased
            || args.Origin == null)
            return;

        ProcessDamageTypes(ent, args.DamageDelta);
        if (args.DamageDelta.DamageDict.TryGetValue(SlashDamage, out var slashDamage))
            TryLoseRandomLimb(ent, args.Origin.Value, slashDamage.Float());
    }

    private void ProcessDamageTypes(Entity<OperatedComponent> ent, DamageSpecifier damageDelta)
    {
        foreach (var (typeId, damage) in damageDelta.DamageDict)
        {
            if (damage <= 0)
                continue;

            var possibleDamages = GetMatchingDamagePrototypes(typeId);
            if (possibleDamages.Count == 0)
                continue;

            TryAddInternalDamages(ent, _random.Pick(possibleDamages));
        }
    }

    private void TryLoseRandomLimb(Entity<OperatedComponent> patient, EntityUid damager, float slashDamage)
    {
        if (slashDamage < 15f)
            return;

        if (_random.Prob(0.005f * patient.Comp.LimbLossChance))
        {
            _inventory.TryGetSlotEntity(patient, "head", out var headItem);
            if (!headItem.HasValue || !HasComp<ArmorComponent>(headItem))
            {
                TryDecapitate(patient, damager);
                return;
            }
        }

        float baseChance = Math.Min(slashDamage * 0.005f, 0.1f);
        if (TryComp<BloodstreamComponent>(patient, out var bloodstream))
            baseChance += Math.Min(bloodstream.BleedAmount * 0.005f, 0.05f);

        if (!_random.Prob(baseChance * patient.Comp.LimbLossChance))
            return;

        var limbs = _body.GetBodyChildren(patient)
            .Where(p => p.Component.PartType switch
            {
                BodyPartType.Arm => true,
                BodyPartType.Hand => true,
                BodyPartType.Leg => true,
                BodyPartType.Foot => true,
                _ => false
            })
            .ToList();

        if (limbs.Count == 0)
            return;

        var (limbId, limbComp) = _random.Pick(limbs);
        var parentSlot = _body.GetParentPartAndSlotOrNull(limbId);
        if (parentSlot == null)
            return;

        var (parentId, slotId) = parentSlot.Value;
        if (!TryComp<BodyPartComponent>(parentId, out var parentPart))
            return;

        var containerId = SharedBodySystem.GetPartSlotContainerId(slotId);
        if (_container.TryGetContainer(parentId, containerId, out var container))
        {
            _container.Remove(limbId, container);
            _popup.PopupEntity(Loc.GetString("surgery-limb-torn-off", ("limb", Name(limbId))), patient, PopupType.SmallCaution);

            _audio.PlayPvs(GibSound, patient);
            if (!_mobState.IsDead(patient) && !HasComp<PainNumbnessComponent>(patient))
                _chat.TryEmoteWithoutChat(patient, _proto.Index<EmotePrototype>("Scream"), true);

            _pain.AdjustPain(patient, "Physical", 250f);
            if (HasComp<BloodstreamComponent>(patient))
                _bloodstream.TryModifyBleedAmount(patient.Owner, 5f);

            var xform = Transform(patient);
            _transform.SetCoordinates(limbId, xform.Coordinates);
            _physics.ApplyLinearImpulse(limbId, _random.NextVector2() * 20f);

            _admin.Add(LogType.Damaged, LogImpact.High, $"{ToPrettyString(damager):user} cuts off a {Name(limbId)} from {ToPrettyString(patient):target}");
        }
    }

    private void TryDecapitate(EntityUid patient, EntityUid damager)
    {
        var head = _body.GetBodyChildrenOfType(patient, BodyPartType.Head).FirstOrDefault();
        if (head == default)
            return;

        var parentSlot = _body.GetParentPartAndSlotOrNull(head.Id);
        if (parentSlot == null)
            return;

        var (parentId, slotId) = parentSlot.Value;
        var containerId = SharedBodySystem.GetPartSlotContainerId(slotId);
        if (_container.TryGetContainer(parentId, containerId, out var container))
        {
            _container.Remove(head.Id, container);
            _popup.PopupEntity(Loc.GetString("surgery-decapitated"), patient, PopupType.MediumCaution);

            _audio.PlayPvs(GibSound, patient);

            var damage = new DamageSpecifier { DamageDict = { { SlashDamage, 200 } } };
            _damage.TryChangeDamage(patient, damage, true);

            if (HasComp<BloodstreamComponent>(patient))
                _bloodstream.TryModifyBleedAmount(patient, 10f);

            _transform.SetCoordinates(head.Id, Transform(patient).Coordinates);
            _physics.ApplyLinearImpulse(head.Id, _random.NextVector2() * 40f);

            _admin.Add(LogType.Damaged, LogImpact.High, $"{ToPrettyString(damager):user} cuts off a HEAD from {ToPrettyString(patient):target}");
        }
    }

    public void ExplosionLimbLoss(Entity<BodyComponent> entity, FixedPoint2 damage)
    {
        if (!HasComp<OperatedComponent>(entity) || HasComp<GodmodeComponent>(entity))
            return;

        var limbs = _body.GetBodyChildren(entity)
            .Where(p => p.Component.PartType switch
            {
                BodyPartType.Arm => true,
                BodyPartType.Hand => true,
                BodyPartType.Leg => true,
                BodyPartType.Foot => true,
                _ => false
            })
            .ToList();

        if (limbs.Count == 0)
            return;

        int limbsToRemove = damage > 200f ? 2 : 1;
        for (int i = 0; i < limbsToRemove && limbs.Count > 0; i++)
        {
            var (limbId, limbComp) = _random.Pick(limbs);
            limbs.Remove((limbId, limbComp));

            var parentSlot = _body.GetParentPartAndSlotOrNull(limbId);
            if (parentSlot == null)
                continue;

            var (parentId, slotId) = parentSlot.Value;
            var containerId = SharedBodySystem.GetPartSlotContainerId(slotId);
            if (_container.TryGetContainer(parentId, containerId, out var container))
            {
                _container.Remove(limbId, container);
                _popup.PopupEntity(Loc.GetString("surgery-explosion-limb-torn-off", ("limb", Name(limbId).ToUpper())), entity, PopupType.MediumCaution);

                if (HasComp<BloodstreamComponent>(entity))
                    _bloodstream.TryModifyBleedAmount(entity.Owner, 5f);

                _audio.PlayPvs(GibSound, entity);
                if (!_mobState.IsDead(entity) && !HasComp<PainNumbnessComponent>(entity))
                    _chat.TryEmoteWithoutChat(entity, _proto.Index<EmotePrototype>("Scream"), true);

                _transform.SetCoordinates(limbId, Transform(entity).Coordinates);
                _physics.ApplyLinearImpulse(limbId, _random.NextVector2() * (50f + (float)damage));

                _admin.Add(LogType.Damaged, LogImpact.High, $"The limb {ToPrettyString(entity):target} '{Name(limbId)}' blown off by the explosion");
            }
        }
    }

    private List<InternalDamagePrototype> GetMatchingDamagePrototypes(string id)
    {
        return _proto.EnumeratePrototypes<InternalDamagePrototype>()
            .Where(p => p.SupportedTypes.Contains(id))
            .ToList();
    }

    private void TryAddInternalDamages(Entity<OperatedComponent> ent, InternalDamagePrototype possibleDamage)
    {
        if (TryComp<HumanoidAppearanceComponent>(ent, out var humanoidAppearance) && possibleDamage.BlacklistSpecies != null
            && possibleDamage.BlacklistSpecies.Contains(humanoidAppearance.Species))
            return;

        float armorModifier = 1f;
        if (_inventory.TryGetSlotEntity(ent, "outerClothing", out var clothing)
            && HasComp<ArmorComponent>(clothing))
            armorModifier = 0.6f;

        if (!_random.Prob(possibleDamage.Chance * armorModifier))
            return;

        var bodyPart = SelectBodyPart(ent.Owner, possibleDamage);
        if (bodyPart != null)
        {
            AddInternalDamage(ent.Comp, possibleDamage.ID, bodyPart);
        }
    }

    private string? SelectBodyPart(EntityUid patient, InternalDamagePrototype damageProto)
    {
        var bodyParts = _body.GetBodyChildren(patient).ToList();

        if (bodyParts.Count == 0)
            return null;

        var availableParts = damageProto.BlacklistPart != null
            ? FilterByBlacklist(bodyParts, damageProto.BlacklistPart)
            : bodyParts.Select(b => GetBodyPartName(b.Component)).ToList();

        return availableParts.Count > 0 ? _random.Pick(availableParts) : null;
    }

    private List<string> FilterByBlacklist(List<(EntityUid Id, BodyPartComponent Component)> bodyParts, List<string> blacklist)
    {
        var result = new List<string>();
        foreach (var (_, component) in bodyParts)
        {
            var partName = GetBodyPartName(component);
            if (!blacklist.Contains(partName))
            {
                result.Add(partName);
            }
        }

        return result;
    }

    private string GetBodyPartName(BodyPartComponent component)
    {
        var symmetry = component.Symmetry;
        var partType = component.PartType;

        var symmetryPrefix = symmetry switch
        {
            BodyPartSymmetry.Left => "left_",
            BodyPartSymmetry.Right => "right_",
            _ => ""
        };

        return symmetryPrefix + partType.ToString().ToLower();
    }

    public bool TryAddInternalDamage(EntityUid target, string damageId, OperatedComponent? component = null, string? bodyPart = null)
    {
        if (!Resolve(target, ref component))
            return false;

        if (!_proto.TryIndex<InternalDamagePrototype>(damageId, out var damageProto))
            return false;

        if (TryComp<HumanoidAppearanceComponent>(target, out var humanoidAppearance) && damageProto.BlacklistSpecies != null
            && damageProto.BlacklistSpecies.Contains(humanoidAppearance.Species))
            return false;

        bodyPart ??= SelectBodyPart(target, damageProto);
        AddInternalDamage(component, damageId, bodyPart);

        return true;
    }

    private void AddInternalDamage(OperatedComponent component, ProtoId<InternalDamagePrototype> damageId, string? bodyPart)
    {
        if (!component.InternalDamages.TryGetValue(damageId, out var bodyParts))
        {
            bodyParts = new List<string>();
            component.InternalDamages.Add(damageId, bodyParts);
        }

        if (bodyPart != null && !bodyParts.Contains(bodyPart))
            bodyParts.Add(bodyPart);
    }

    #endregion

    #region Examine

    private void OnOperatedExamined(Entity<OperatedComponent> entity, ref ExaminedEvent args)
    {
        if (entity.Comp.InternalDamages.Count == 0)
            return;

        if (args.IsInDetailsRange)
        {
            var message = new StringBuilder();
            foreach (var (damageProtoId, _) in entity.Comp.InternalDamages)
            {
                if (!_proto.TryIndex(damageProtoId, out InternalDamagePrototype? damageProto))
                    continue;

                if (!string.IsNullOrEmpty(damageProto.BodyVisuals))
                {
                    message.Append($"{Loc.GetString(damageProto.BodyVisuals)}\n");
                }
            }

            if (message.Length > 0)
            {
                args.AddMarkup(message.ToString());
            }
        }
    }

    #endregion

    #region Taking damage

    private void ProcessInternalDamages(EntityUid uid, OperatedComponent operated)
    {
        var damagesToRemove = new List<(ProtoId<InternalDamagePrototype> DamageId, string? BodyPart)>();
        foreach (var (damageId, bodyParts) in operated.InternalDamages)
        {
            if (!_proto.TryIndex<InternalDamagePrototype>(damageId, out var damageProto))
                continue;

            if (damageProto.Category is DamageCategory.PhysicalTrauma or DamageCategory.Burns)
            {
                foreach (var bodyPart in bodyParts)
                {
                    if (_random.Prob(0.02f))
                    {
                        damagesToRemove.Add((damageId, bodyPart));
                    }
                }
            }

            if (!_random.Prob(0.10f))
                continue;

            ApplyDamageEffects(uid, damageProto, bodyParts);
        }

        foreach (var (damageId, bodyPart) in damagesToRemove)
        {
            if (bodyPart == null)
            {
                operated.InternalDamages.Remove(damageId);
            }
            else if (operated.InternalDamages.TryGetValue(damageId, out var parts))
            {
                parts.Remove(bodyPart);
                if (parts.Count == 0)
                {
                    operated.InternalDamages.Remove(damageId);
                }
            }
        }
    }

    private void ApplyDamageEffects(EntityUid patient, InternalDamagePrototype damageProto, List<string> bodyParts)
    {
        if (bodyParts.Count == 0)
            return;

        var severityMod = _random.NextFloat(0.5f, 1.5f);
        var severity = Math.Min(bodyParts.Count * damageProto.Severity * severityMod, 3f);

        switch (damageProto.Category)
        {
            case DamageCategory.PhysicalTrauma:
                HandlePhysicalTrauma(patient, severity, bodyParts);
                break;

            case DamageCategory.Burns:
                HandleBurns(patient, severity, bodyParts);
                break;

            case DamageCategory.Fractures:
                HandleFractures(patient, severity, bodyParts);
                break;

            case DamageCategory.InternalBleeding:
                HandleInternalBleeding(patient, severity, bodyParts);
                break;

            case DamageCategory.CriticalBurns:
                HandleCriticalBurns(patient, severity, bodyParts);
                break;

            case DamageCategory.ForeignObjects:
                HandleForeignObjects(patient, severity, bodyParts);
                break;

            default: break;
        }
    }

    private void HandlePhysicalTrauma(EntityUid patient, float severity, List<string> bodyParts)
    {
        _pain.AdjustPain(patient, "Physical", 10 * severity);

        foreach (var part in bodyParts)
        {
            var painType = GetPainTypeForBodyPart(part);
            _pain.AdjustPain(patient, painType, 5 * severity);
        }
    }

    private void HandleBurns(EntityUid patient, float severity, List<string> bodyParts)
    {
        _pain.AdjustPain(patient, "Burn", 8 * severity);

        if (bodyParts.Any(p => p.Contains("head") || p.Contains("torso")))
        {
            _pain.AdjustPain(patient, "CriticalBurn", 5 * severity);
        }
    }

    private void HandleFractures(EntityUid patient, float severity, List<string> bodyParts)
    {
        foreach (var part in bodyParts)
        {
            var painType = part.Contains("arm") ? "ArmFracture" :
                        part.Contains("leg") ? "LegFracture" :
                        "BoneFracture";

            _pain.AdjustPain(patient, painType, 15 * severity);

            float dropProb = Math.Min(0.3f * severity, 1f);
            if (part.Contains("arm") && _random.Prob(dropProb))
            {
                var dropEvent = new DropHandItemsEvent();
                RaiseLocalEvent(patient, ref dropEvent);
            }

            if (part.Contains("leg"))
            {
                _movementMod.TryUpdateMovementSpeedModDuration(patient, MovementModStatusSystem.Slowdown, TimeSpan.FromSeconds(Math.Min(5 * severity, 10)),
                    0.5f, 0.3f);

                if (bodyParts.Count(p => p.Contains("leg")) >= 2)
                {
                    _stun.TryKnockdown(patient, TimeSpan.FromSeconds(Math.Min(3 * severity, 5)), true);
                }
            }
        }
    }

    private void HandleInternalBleeding(EntityUid patient, float severity, List<string> bodyParts)
    {
        if (TryComp<BloodstreamComponent>(patient, out _))
        {
            _bloodstream.TryModifyBleedAmount(patient, 0.75f * severity);

            float bloodLossProb = Math.Min(0.3f * severity, 1f);
            if (_random.Prob(bloodLossProb))
            {
                _bloodstream.TryModifyBloodLevel(patient, -0.1f * severity);
            }
        }

        _pain.AdjustPain(patient, "Internal", 12 * severity);
    }

    private void HandleCriticalBurns(EntityUid patient, float severity, List<string> bodyParts)
    {
        _pain.AdjustPain(patient, "CriticalBurn", 25 * severity);

        float stunProb = Math.Min(0.15f * severity, 1f);
        if (_random.Prob(stunProb))
        {
            _stun.TryUpdateStunDuration(patient, TimeSpan.FromSeconds(3 * severity));
            _jittering.DoJitter(patient, TimeSpan.FromSeconds(15), true);
        }
    }

    private void HandleForeignObjects(EntityUid patient, float severity, List<string> bodyParts)
    {
        _pain.AdjustPain(patient, "ForeignObject", 15 * severity);

        float infectionProb = Math.Min(0.05f * severity, 1f);
        if (_random.Prob(infectionProb))
        {
            _disease.TryAddDisease(patient, "BloodInfection");
        }

        float sharpPainProb = Math.Min(0.4f * severity, 1f);
        if (_random.Prob(sharpPainProb))
        {
            _pain.AdjustPain(patient, "SharpPain", 30);
        }
    }

    private string GetPainTypeForBodyPart(string bodyPart)
    {
        return bodyPart switch
        {
            var s when s.Contains("head") => "HeadTrauma",
            var s when s.Contains("torso") => "TorsoTrauma",
            var s when s.Contains("arm") => "ArmTrauma",
            var s when s.Contains("leg") => "LegTrauma",
            _ => "LocalizedPain"
        };
    }

    #endregion
}
