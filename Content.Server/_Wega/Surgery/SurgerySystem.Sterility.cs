using System.Linq;
using Content.Shared.Body;
using Content.Shared.Clothing.Components;
using Content.Shared.DirtVisuals;
using Content.Shared.Ghost;
using Content.Shared.Shuttles.Components;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Surgery;
using Content.Shared.Surgery.Components;

namespace Content.Server.Surgery;

public sealed partial class SurgerySystem
{
    [Dependency] private EntityLookupSystem _entityLookup = default!;

    #region Sterility

    private void UpdateOperationSterility(EntityUid patient, OperatedComponent operated)
    {
        if (operated.Surgeon == null || HasComp<SyntheticOperatedComponent>(patient))
            return;

        float sterility = 1f;

        // Важные слоты
        CheckClothingSlot(operated.Surgeon.Value, "gloves", ref sterility, 0.15f, true);
        CheckClothingSlot(operated.Surgeon.Value, "mask", ref sterility, 0.15f, true);

        // Средние слоты
        CheckClothingSlot(operated.Surgeon.Value, "head", ref sterility, 0.05f);
        CheckClothingSlot(operated.Surgeon.Value, "jumpsuit", ref sterility, 0.05f);
        CheckClothingSlot(operated.Surgeon.Value, "outerClothing", ref sterility, 0.05f, ingnoreSlot: true);

        // Нежелательные слоты
        CheckClothingSlot(operated.Surgeon.Value, "back", ref sterility, 0.02f, ingnoreSlot: true);
        CheckClothingSlot(operated.Surgeon.Value, "belt", ref sterility, 0.02f, ingnoreSlot: true);

        var garbageCount = _entityLookup.GetEntitiesInRange<SpaceGarbageComponent>(
            Transform(patient).Coordinates, 1.5f).Count;

        sterility *= Math.Max(0.7f, 1f - garbageCount * 0.05f);

        var item = _hands.GetActiveItemOrSelf(operated.Surgeon.Value);
        if (!HasComp<SterileComponent>(item))
            sterility *= 0.85f;

        var bystanders = _entityLookup.GetEntitiesInRange<BodyComponent>(Transform(patient).Coordinates, 2f)
            .Where(e => e.Owner != patient && e.Owner != operated.Surgeon
                && !_mobState.IsDead(e.Owner) && !HasComp<GhostComponent>(e.Owner));

        float bystanderModifier = bystanders.Count() switch
        {
            <= 2 => 1f,
            <= 4 => 0.97f,
            <= 6 => 0.94f,
            _ => 0.9f
        };
        sterility *= bystanderModifier;

        var corpses = _entityLookup.GetEntitiesInRange<BodyComponent>(Transform(patient).Coordinates, 2f)
            .Where(e => e.Owner != patient && e.Owner != operated.Surgeon
                && _mobState.IsDead(e.Owner) && !HasComp<GhostComponent>(e.Owner));

        sterility *= 1f - corpses.Count() * 0.03f;

        operated.Sterility = Math.Clamp(sterility, 0.2f, 1f);
        SendSterilityUpdateToUi(patient, operated.Surgeon.Value);
    }

    private void CheckClothingSlot(EntityUid surgeon, string slot, ref float sterility, float penaltyModifier,
        bool isCritical = false, bool ingnoreSlot = false)
    {
        if (HasComp<BorgChassisComponent>(surgeon))
            return;

        if (_inventory.TryGetSlotEntity(surgeon, slot, out var clothing))
        {
            bool isMaskOff = false;
            if (TryComp(clothing, out MaskComponent? mask))
                isMaskOff = mask.IsToggled;

            bool isDirty = false;
            if (TryComp<DirtableComponent>(clothing, out var dirtable))
            {
                var dirtLevel = Math.Clamp(dirtable.CurrentDirtLevel.Float() / SharedDirtSystem.MaxDirtLevel * 100f, 0f, 100f);
                if (dirtable.IsDirty && dirtLevel >= 50f)
                    isDirty = true;
            }

            if (TryComp<ClothingSterilityComponent>(clothing, out var sterilityComp) && !isMaskOff)
            {
                sterility *= sterilityComp.Modifier * (isDirty ? 0.95f : 1f);
            }
            else
            {
                sterility *= (1f - penaltyModifier) * (isDirty ? 0.98f : 1f);
            }
        }
        else if (isCritical)
        {
            sterility *= 0.85f;
        }
        else if (!ingnoreSlot)
        {
            sterility *= 1f - penaltyModifier * 0.5f;
        }
    }

    #endregion

    #region UI Info

    private SurgerySterilityInfo GetSterilityInfo(EntityUid patient, EntityUid surgeon)
    {
        if (HasComp<SyntheticOperatedComponent>(patient))
            return new SurgerySterilityInfo(1f, new List<string>(), new List<string>());

        float sterility = 1f;
        var negativeFactors = new List<string>();
        var positiveFactors = new List<string>();

        // Важные слоты
        CheckClothingSlotWithFactors(surgeon, "gloves", ref sterility, 0.15f, true, negativeFactors, positiveFactors);
        CheckClothingSlotWithFactors(surgeon, "mask", ref sterility, 0.15f, true, negativeFactors, positiveFactors);

        // Средние слоты
        CheckClothingSlotWithFactors(surgeon, "head", ref sterility, 0.05f, false, negativeFactors, positiveFactors);
        CheckClothingSlotWithFactors(surgeon, "jumpsuit", ref sterility, 0.05f, false, negativeFactors, positiveFactors);
        CheckClothingSlotWithFactors(surgeon, "outerClothing", ref sterility, 0.05f, true, negativeFactors, positiveFactors);

        // Нежелательные слоты
        CheckClothingSlotWithFactors(surgeon, "back", ref sterility, 0.02f, true, negativeFactors, positiveFactors);
        CheckClothingSlotWithFactors(surgeon, "belt", ref sterility, 0.02f, true, negativeFactors, positiveFactors);

        var garbageCount = _entityLookup.GetEntitiesInRange<SpaceGarbageComponent>(
            Transform(patient).Coordinates, 1.5f).Count;

        if (garbageCount > 0)
        {
            var garbageModifier = Math.Max(0.7f, 1f - garbageCount * 0.05f);
            sterility *= garbageModifier;
            negativeFactors.Add(Loc.GetString("surgery-sterility-garbage", ("count", garbageCount)));
        }

        var item = _hands.GetActiveItemOrSelf(surgeon);
        if (!HasComp<SterileComponent>(item))
        {
            sterility *= 0.85f;
            negativeFactors.Add(Loc.GetString("surgery-sterility-non-sterile-tool"));
        }
        else
        {
            positiveFactors.Add(Loc.GetString("surgery-sterility-sterile-tool"));
        }

        var bystanders = _entityLookup.GetEntitiesInRange<BodyComponent>(Transform(patient).Coordinates, 2f)
            .Where(e => e.Owner != patient && e.Owner != surgeon
                && !_mobState.IsDead(e.Owner) && !HasComp<GhostComponent>(e.Owner));

        int bystanderCount = bystanders.Count();
        if (bystanderCount > 2)
        {
            float bystanderModifier = bystanderCount switch
            {
                <= 4 => 0.97f,
                <= 6 => 0.94f,
                _ => 0.9f
            };
            sterility *= bystanderModifier;
            negativeFactors.Add(Loc.GetString("surgery-sterility-bystanders", ("count", bystanderCount)));
        }

        var corpses = _entityLookup.GetEntitiesInRange<BodyComponent>(Transform(patient).Coordinates, 2f)
            .Where(e => e.Owner != patient && e.Owner != surgeon
                && _mobState.IsDead(e.Owner) && !HasComp<GhostComponent>(e.Owner));

        int corpseCount = corpses.Count();
        if (corpseCount > 0)
        {
            sterility *= 1f - corpseCount * 0.03f;
            negativeFactors.Add(Loc.GetString("surgery-sterility-corpses", ("count", corpseCount)));
        }

        if (TryGetOperatingTable(patient, out _))
        {
            positiveFactors.Add(Loc.GetString("surgery-sterility-operating-table"));
        }

        sterility = Math.Clamp(sterility, 0.2f, 1f);
        return new SurgerySterilityInfo(sterility, negativeFactors, positiveFactors);
    }

    private void CheckClothingSlotWithFactors(EntityUid surgeon, string slot, ref float sterility, float penaltyModifier,
        bool ignoreSlot, List<string> negativeFactors, List<string> positiveFactors)
    {
        if (HasComp<BorgChassisComponent>(surgeon))
            return;

        if (_inventory.TryGetSlotEntity(surgeon, slot, out var clothing))
        {
            bool isMaskOff = false;
            if (TryComp(clothing, out MaskComponent? mask))
                isMaskOff = mask.IsToggled;

            bool isDirty = false;
            if (TryComp<DirtableComponent>(clothing, out var dirtable))
            {
                var dirtLevel = Math.Clamp(dirtable.CurrentDirtLevel.Float() / SharedDirtSystem.MaxDirtLevel * 100f, 0f, 100f);
                if (dirtable.IsDirty && dirtLevel >= 50f)
                    isDirty = true;
            }

            if (TryComp<ClothingSterilityComponent>(clothing, out var sterilityComp) && !isMaskOff)
            {
                var modifier = sterilityComp.Modifier * (isDirty ? 0.95f : 1f);
                sterility *= modifier;
                if (modifier > 1f)
                    positiveFactors.Add(Loc.GetString($"surgery-sterility-sterile-{slot}"));
                else if (modifier < 1f)
                    negativeFactors.Add(Loc.GetString($"surgery-sterility-non-sterile-{slot}"));
            }
            else
            {
                var modifier = (1f - penaltyModifier) * (isDirty ? 0.98f : 1f);
                sterility *= modifier;
                if (modifier < 1f)
                    negativeFactors.Add(Loc.GetString($"surgery-sterility-no-sterile-{slot}"));
            }
        }
        else if (slot == "gloves" || slot == "mask")
        {
            sterility *= 0.85f;
            negativeFactors.Add(Loc.GetString($"surgery-sterility-no-{slot}"));
        }
        else if (!ignoreSlot)
        {
            var modifier = 1f - penaltyModifier * 0.5f;
            sterility *= modifier;
            negativeFactors.Add(Loc.GetString($"surgery-sterility-no-{slot}"));
        }
    }

    #endregion
}
