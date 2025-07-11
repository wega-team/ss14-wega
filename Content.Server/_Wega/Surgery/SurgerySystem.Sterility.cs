using System.Linq;
using Content.Shared.Body.Components;
using Content.Shared.Clothing.Components;
using Content.Shared.Ghost;
using Content.Shared.Shuttles.Components;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Surgery.Components;

namespace Content.Server.Surgery;

public sealed partial class SurgerySystem
{
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;

    private void UpdateOperationSterility(EntityUid patient, OperatedComponent operated)
    {
        if (operated.Surgeon == null)
            return;

        float sterility = 1f;

        // Важные слоты (сильное влияние)
        CheckClothingSlot(operated.Surgeon.Value, "gloves", ref sterility, 0.6f, true);
        CheckClothingSlot(operated.Surgeon.Value, "mask", ref sterility, 0.6f, true);

        // Средние слоты (умеренное влияние)
        CheckClothingSlot(operated.Surgeon.Value, "head", ref sterility, 0.2f);
        CheckClothingSlot(operated.Surgeon.Value, "jumpsuit", ref sterility, 0.2f);
        CheckClothingSlot(operated.Surgeon.Value, "outerClothing", ref sterility, 0.2f, ingnoreSlot: true);

        // Нежелательные слоты (небольшой дебафф)
        CheckClothingSlot(operated.Surgeon.Value, "back", ref sterility, 0.1f, ingnoreSlot: true);
        CheckClothingSlot(operated.Surgeon.Value, "belt", ref sterility, 0.1f, ingnoreSlot: true);

        var garbageCount = _entityLookup.GetEntitiesInRange<SpaceGarbageComponent>(
            Transform(patient).Coordinates, 2f).Count;

        sterility *= Math.Max(0.1f, 1f - garbageCount * 0.1f);

        var item = _hands.GetActiveItemOrSelf(operated.Surgeon.Value);
        if (!HasComp<SterileComponent>(item))
            sterility *= 0.4f;

        var bystanders = _entityLookup.GetEntitiesInRange<BodyComponent>(
            Transform(patient).Coordinates, 2f)
            .Where(e => e.Owner != patient && e.Owner != operated.Surgeon
                && !_mobState.IsDead(e.Owner) && !HasComp<GhostComponent>(e.Owner))
            .Count();

        float bystanderModifier = bystanders switch
        {
            <= 2 => 1f,
            <= 4 => 0.9f,
            <= 6 => 0.8f,
            _ => 0.7f
        };
        sterility *= bystanderModifier;

        var corpses = _entityLookup.GetEntitiesInRange<BodyComponent>(
            Transform(patient).Coordinates, 2f)
            .Where(e => e.Owner != patient && e.Owner != operated.Surgeon
                && _mobState.IsDead(e.Owner) && !HasComp<GhostComponent>(e.Owner))
            .Count();

        sterility *= 1f - corpses * 0.05f;

        operated.Sterility = Math.Clamp(sterility, 0f, 1f);
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

            if (TryComp<ClothingSterilityComponent>(clothing, out var sterilityComp) && !isMaskOff)
            {
                sterility *= sterilityComp.Modifier;
            }
            else
            {
                sterility *= 1f - penaltyModifier;
            }
        }
        else if (isCritical)
        {
            sterility *= 0.5f;
        }
        else if (!ingnoreSlot)
        {
            sterility *= 1f - penaltyModifier;
        }
    }
}