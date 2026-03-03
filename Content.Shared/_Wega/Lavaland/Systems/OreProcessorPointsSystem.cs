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

        SubscribeLocalEvent<PointsCapitalComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnExamined(Entity<OreProcessorPointsComponent> entity, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.AddMarkup(Loc.GetString("ore-processor-points", ("points", entity.Comp.AccumulatedPoints)) + "\n");
    }

    private void OnMaterialInserted(EntityUid uid, OreProcessorPointsComponent component, ref MaterialEntityInsertedEvent args)
    {
        var stackEntity = args.MaterialComp.Owner;
        if (!TryComp<StackComponent>(stackEntity, out var stack))
            return;

        if (!TryComp<OreValueComponent>(stackEntity, out var oreValue) || !oreValue.Mined)
            return;

        var pointsEarned = CalculateOrePoints(stack, oreValue, component);
        if (pointsEarned > 0)
        {
            component.AccumulatedPoints += pointsEarned;
            Dirty(uid, component);
        }
    }

    private double CalculateOrePoints(StackComponent stack, OreValueComponent oreValue, OreProcessorPointsComponent component)
    {
        var totalPoints = stack.Count * oreValue.Points;
        return Math.Floor(totalPoints);
    }

    private void OnInteractUsing(Entity<OreProcessorPointsComponent> entity, ref InteractUsingEvent args)
    {
        if (TryComp<PointsCapitalComponent>(args.Used, out var capital) && capital.Points > 0 && !capital.CardTransfer)
        {
            entity.Comp.AccumulatedPoints += capital.Points;
            args.Handled = TryQueueDel(args.Used);
            return;
        }

        if (HasComp<PointsCardComponent>(args.Used))
        {
            args.Handled = TransferPointsToCard(entity, args.Used, args.User);
        }
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

        _popup.PopupEntity(Loc.GetString("ore-processor-add-points", ("points", points)), user, user);

        return true;
    }

    private void OnAfterInteract(Entity<PointsCapitalComponent> entity, ref AfterInteractEvent args)
    {
        if (!entity.Comp.CardTransfer || entity.Comp.Points <= 0)
            return;

        if (!TryComp<PointsCardComponent>(args.Target, out var pointsCard))
            return;

        var points = entity.Comp.Points;
        pointsCard.Points += points;
        _popup.PopupClient(Loc.GetString("points-capital-add-points", ("points", points)), args.User, args.User);
        Del(entity);
    }
}
