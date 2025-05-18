using System.Linq;
using System.Text;
using Content.Server.Body.Components;
using Content.Server.Disease;
using Content.Server.Pain;
using Content.Shared.Body.Part;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.Jittering;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Surgery;
using Content.Shared.Surgery.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Surgery;

public sealed partial class SurgerySystem
{
    [Dependency] private readonly PainSystem _pain = default!;
    [Dependency] private readonly SharedJitteringSystem _jittering = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly DiseaseSystem _disease = default!;

    private void InternalDamageInitialize()
    {
        SubscribeLocalEvent<OperatedComponent, DamageChangedEvent>(OnDamage);
        SubscribeLocalEvent<OperatedComponent, ExaminedEvent>(OnExamined);
    }

    #region Process damage

    private void OnDamage(Entity<OperatedComponent> ent, ref DamageChangedEvent args)
    {
        if (args.DamageDelta == null || args.DamageDelta.Empty || !args.DamageIncreased)
            return;

        ProcessDamageTypes(ent, args.DamageDelta);
    }

    private void ProcessDamageTypes(Entity<OperatedComponent> ent, DamageSpecifier damageDelta)
    {
        foreach (var (typeId, damage) in damageDelta.DamageDict)
        {
            if (damage <= 0)
                continue;

            var possibleDamages = GetMatchingDamagePrototypes(typeId);
            TryAddInternalDamages(ent, possibleDamages);
        }
    }

    private List<InternalDamagePrototype> GetMatchingDamagePrototypes(string id)
    {
        return _proto.EnumeratePrototypes<InternalDamagePrototype>()
            .Where(p => p.SupportedTypes.Contains(id))
            .ToList();
    }

    private void TryAddInternalDamages(Entity<OperatedComponent> ent, List<InternalDamagePrototype> possibleDamages)
    {
        var component = ent.Comp;
        foreach (var damageProto in possibleDamages)
        {
            if (!ShouldAddDamage(damageProto))
                continue;

            var bodyPart = SelectBodyPart(ent.Owner, damageProto);
            if (bodyPart != null)
            {
                AddInternalDamage(component, damageProto.ID, bodyPart);
            }
        }
    }

    private bool ShouldAddDamage(InternalDamagePrototype damageProto)
        => _random.Prob(damageProto.Chance);

    private string? SelectBodyPart(EntityUid patient, InternalDamagePrototype damageProto)
    {
        var bodyParts = _body.GetBodyChildren(patient).ToList();

        if (bodyParts.Count == 0)
            return null;

        var availableParts = damageProto.Blacklist != null
            ? FilterByBlacklist(bodyParts, damageProto.Blacklist)
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

    private void OnExamined(Entity<OperatedComponent> entity, ref ExaminedEvent args)
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
                    message.Append($"\n{Loc.GetString(damageProto.BodyVisuals)}");
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
        foreach (var (damageId, bodyParts) in operated.InternalDamages)
        {
            if (!_proto.TryIndex<InternalDamagePrototype>(damageId, out var damageProto))
                continue;

            if (!_random.Prob(0.10f))
                continue;

            ApplyDamageEffects(uid, damageProto, bodyParts);
        }
    }

    private void ApplyDamageEffects(EntityUid patient, InternalDamagePrototype damageProto, List<string> bodyParts)
    {
        if (bodyParts.Count == 0)
            return;

        var severityMod = _random.NextFloat(0.5f, 1.5f);
        var severity = bodyParts.Count * damageProto.Severity * severityMod;

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

            if (part.Contains("arm") && _random.Prob(0.3f * severity))
            {
                var dropEvent = new DropHandItemsEvent();
                RaiseLocalEvent(patient, ref dropEvent);
            }

            if (part.Contains("leg"))
            {
                _stun.TrySlowdown(patient, TimeSpan.FromSeconds(5 * severity), true, 0.5f, 0.3f);

                if (bodyParts.Count(p => p.Contains("leg")) >= 2)
                {
                    _stun.TryKnockdown(patient, TimeSpan.FromSeconds(3 * severity), true);
                }
            }
        }
    }

    private void HandleInternalBleeding(EntityUid patient, float severity, List<string> bodyParts)
    {
        if (TryComp<BloodstreamComponent>(patient, out _))
        {
            _bloodstream.TryModifyBleedAmount(patient, 0.75f * severity);

            if (_random.Prob(0.3f * severity))
            {
                _bloodstream.TryModifyBloodLevel(patient, -0.1f * severity);
            }
        }

        _pain.AdjustPain(patient, "Internal", 12 * severity);
    }

    private void HandleCriticalBurns(EntityUid patient, float severity, List<string> bodyParts)
    {
        _pain.AdjustPain(patient, "CriticalBurn", 25 * severity);

        if (_random.Prob(0.15f * severity))
        {
            _stun.TryStun(patient, TimeSpan.FromSeconds(3 * severity), true);
            _jittering.DoJitter(patient, TimeSpan.FromSeconds(15), true);
        }
    }

    private void HandleForeignObjects(EntityUid patient, float severity, List<string> bodyParts)
    {
        _pain.AdjustPain(patient, "ForeignObject", 15 * severity);

        if (_random.Prob(0.05f * severity))
        {
            _disease.TryAddDisease(patient, "BloodInfection");
        }

        if (_random.Prob(0.4f * severity))
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
