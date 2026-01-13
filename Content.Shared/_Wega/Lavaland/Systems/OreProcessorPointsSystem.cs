using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Lavaland.Components;
using Content.Shared.Materials;
using Content.Shared.Popups;
using Content.Shared.Stacks;

namespace Content.Shared.Lavaland;

public sealed partial class OreProcessorPointsSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OreProcessorPointsComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<OreProcessorPointsComponent, MaterialEntityInsertedEvent>(OnMaterialInserted);
        SubscribeLocalEvent<OreProcessorPointsComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnExamined(Entity<OreProcessorPointsComponent> entity, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.AddMarkup(Loc.GetString("ore-processor-points", ("points", entity.Comp.AccumulatedPoints)) + "\n");
    }

    private void OnMaterialInserted(EntityUid uid, OreProcessorPointsComponent component, ref MaterialEntityInsertedEvent args)
    {
        if (!TryComp<StackComponent>(args.MaterialComp.Owner, out var stack))
            return;

        var pointsEarned = CalculatePointsFromMaterials(stack, component);

        if (pointsEarned > 0)
        {
            component.AccumulatedPoints += pointsEarned;
            Dirty(uid, component);
        }
    }

    private double CalculatePointsFromMaterials(StackComponent stack, OreProcessorPointsComponent component)
    {
        var totalPoints = 0f;
        if (component.PointMultipliers.TryGetValue(stack.StackTypeId, out var multiplier))
            totalPoints += stack.Count * multiplier;

        return Math.Floor(totalPoints);
    }

    private void OnInteractUsing(Entity<OreProcessorPointsComponent> entity, ref InteractUsingEvent args)
    {
        if (!HasComp<PointsCardComponent>(args.Used))
            return;

        args.Handled = TransferPointsToCard(entity, args.Used, args.User);
    }

    public bool TransferPointsToCard(Entity<OreProcessorPointsComponent> entity, EntityUid card, EntityUid user)
    {
        if (!TryComp<PointsCardComponent>(card, out var pointsCard))
            return false;

        if (entity.Comp.AccumulatedPoints <= 0)
            return false;

        var points = entity.Comp.AccumulatedPoints;
        pointsCard.Points += points;
        entity.Comp.AccumulatedPoints = 0;

        Dirty(entity.Owner, entity.Comp);
        Dirty(card, pointsCard);

        _popup.PopupClient(Loc.GetString("ore-processor-add-points", ("points", points)), user, user);

        return true;
    }
}
