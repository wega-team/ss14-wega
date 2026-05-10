using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Content.Shared.Vampire.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Vampire;

public abstract class SharedVampireSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;

    #region Blood Management

    public bool CheckBloodEssence(VampireComponent component, FixedPoint2 requiredAmount, FixedPoint2? nullDamage = null)
    {
        var adjustedQuantity = requiredAmount * (1 + (nullDamage?.Float() ?? 0) / 100);
        return component.CurrentBlood >= adjustedQuantity;
    }

    public bool TryAddBlood(VampireComponent component, FixedPoint2 quantity, out FixedPoint2 newAmount)
    {
        newAmount = component.CurrentBlood + quantity;
        if (quantity <= 0)
            return false;

        component.CurrentBlood = newAmount;
        component.TotalBloodDrank += (float)quantity;
        return true;
    }

    public bool TrySubtractBlood(VampireComponent component, FixedPoint2 quantity, FixedPoint2? nullDamage = null)
    {
        var adjustedQuantity = quantity * (1 + (nullDamage?.Float() ?? 0) / 100);
        if (adjustedQuantity <= 0 || component.CurrentBlood < adjustedQuantity)
            return false;

        component.CurrentBlood -= adjustedQuantity;
        return true;
    }

    #endregion

    #region Skill Management

    public bool TryGetThresholdsForClass(VampireComponent component, [NotNullWhen(true)] out Dictionary<float, List<EntProtoId>>? thresholds)
    {
        thresholds = null;
        if (component.CurrentEvolution == VampireClassEnum.NonSelected)
            return false;

        return component.ClassThresholds.TryGetValue(component.CurrentEvolution, out thresholds);
    }

    public bool HasSkill(VampireComponent component, EntProtoId skill)
    {
        return component.AcquiredSkills.ContainsKey(skill);
    }

    public List<EntProtoId> GetNewSkillsToAdd(VampireComponent component)
    {
        var newSkills = new List<EntProtoId>();
        if (component.CurrentEvolution == VampireClassEnum.NonSelected)
            return newSkills;

        if (!TryGetThresholdsForClass(component, out var thresholds))
            return newSkills;

        foreach (var (threshold, skills) in thresholds.OrderBy(kv => kv.Key))
        {
            if (component.CurrentBlood >= threshold)
            {
                foreach (var skill in skills)
                {
                    if (!HasSkill(component, skill))
                        newSkills.Add(skill);
                }
            }
        }

        return newSkills;
    }

    #endregion

    #region Thrall Management

    public bool CanAddThrall(ThrallOwnerComponent owner)
    {
        return owner.ThrallCount < owner.MaxThrallCount;
    }

    public bool TryAddThrall(ThrallOwnerComponent owner, EntityUid thrallUid)
    {
        if (!CanAddThrall(owner))
            return false;

        if (!owner.ThrallOwned.Contains(thrallUid))
        {
            owner.ThrallOwned.Add(thrallUid);
            owner.ThrallCount++;
            return true;
        }

        return false;
    }

    public bool TryRemoveThrall(ThrallOwnerComponent owner, EntityUid thrallUid)
    {
        if (owner.ThrallOwned.Remove(thrallUid))
        {
            owner.ThrallCount--;
            return true;
        }

        return false;
    }

    public List<EntityUid> GetAliveThralls(ThrallOwnerComponent owner)
    {
        return owner.ThrallOwned.Where(Exists)
            .Where(t => !_mobState.IsDead(t))
            .ToList();
    }

    #endregion

    #region True Power Management

    public bool ShouldHaveTruePower(FixedPoint2 currentBlood)
    {
        return currentBlood >= 1000;
    }

    public bool HasTruePower(EntityUid uid)
    {
        return HasComp<SupremeVampireComponent>(uid);
    }

    public SupremeVampireComponent? GetTruePower(EntityUid uid)
    {
        return CompOrNull<SupremeVampireComponent>(uid);
    }

    #endregion

    #region Night Vision

    public Color GetNightVisionColorForClass(VampireClassEnum vampireClass)
    {
        return vampireClass switch
        {
            VampireClassEnum.Umbrae => Color.FromHex("#663ca3"),
            _ => Color.FromHex("#adadad")
        };
    }

    public float GetNightVisionRadiusForClass(VampireClassEnum vampireClass, bool hasTruePower = false)
    {
        if (hasTruePower)
            return 15;

        return vampireClass switch
        {
            VampireClassEnum.Umbrae => 12,
            _ => 8
        };
    }

    #endregion

    #region Space Damage

    public bool ShouldTakeSpaceDamage(VampireComponent component, float frameTime, out bool shouldDamage)
    {
        shouldDamage = false;

        if (component.NextSpaceDamageTick <= 0)
        {
            shouldDamage = true;
            component.NextSpaceDamageTick = 1;
            return true;
        }

        component.NextSpaceDamageTick -= frameTime;
        return false;
    }

    #endregion

    #region Damage Sharing

    public DamageSpecifier GetSharedDamage(DamageSpecifier originalDamage, int participantCount)
    {
        if (participantCount <= 0)
            return originalDamage;

        return originalDamage / participantCount;
    }

    #endregion
}
