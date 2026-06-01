using Content.Server.Body.Systems;
using Content.Server.Ninja.Components;
using Content.Server.Stack;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Materials;
using Content.Shared.Ninja.Components;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Timing;

namespace Content.Server.Ninja.Systems;

public sealed class NinjaAdrenalineBurstSystem : EntitySystem
{
    private const string HyperzineReagent   = "Stimulants";
    private const string UraniumReagent     = "Uranium";
    private const string UraniumMaterial    = "Uranium";
    private const string SuitDisableDelayId = "suit_powers";

    [Dependency] private readonly BloodstreamSystem  _blood    = default!;
    [Dependency] private readonly SharedPopupSystem  _popup    = default!;
    [Dependency] private readonly UseDelaySystem     _useDelay = default!;
    [Dependency] private readonly StackSystem        _stack    = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NinjaSuitComponent, NinjaAdrenalineBurstEvent>(OnAdrenalineBurst);
        SubscribeLocalEvent<NinjaSuitComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(Entity<NinjaSuitComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<NinjaUraniumTankComponent>(ent, out var tank))
            return;

        var used = args.Used;

        if (!TryComp<PhysicalCompositionComponent>(used, out var physComp) ||
            !physComp.MaterialComposition.ContainsKey(UraniumMaterial))
            return;

        args.Handled = true;

        if (tank.UraniumStored >= tank.UraniumMax)
        {
            _popup.PopupEntity(Loc.GetString("ninja-uranium-full"), args.User, args.User);
            return;
        }

        var hasStack = TryComp<StackComponent>(used, out var stackComp);
        var count = hasStack ? stackComp!.Count : 1;
        var canAdd = Math.Min(count, tank.UraniumMax - tank.UraniumStored);
        tank.UraniumStored += canAdd;
        Dirty(ent.Owner, tank);

        if (hasStack)
            _stack.SetCount(used, count - canAdd);
        else
            QueueDel(used);

        _popup.PopupEntity(
            Loc.GetString("ninja-uranium-inserted", ("amount", canAdd), ("total", tank.UraniumStored), ("max", tank.UraniumMax)),
            args.User, args.User);
    }

    private void OnAdrenalineBurst(Entity<NinjaSuitComponent> ent, ref NinjaAdrenalineBurstEvent args)
    {
        args.Handled = true;

        var suit = ent.Owner;
        var user = args.Performer;

        if (!TryComp<NinjaUraniumTankComponent>(suit, out var tank))
            return;

        if (TryComp<UseDelayComponent>(suit, out var delay) &&
            _useDelay.IsDelayed((suit, delay), SuitDisableDelayId))
            return;

        if (tank.UraniumStored < tank.UraniumBurstCost)
        {
            _popup.PopupEntity(Loc.GetString("ninja-adrenaline-no-uranium"), user, user);
            return;
        }

        tank.UraniumStored -= tank.UraniumBurstCost;
        Dirty(suit, tank);

        var inject = new Solution();
        inject.AddReagent(HyperzineReagent, FixedPoint2.New(10f));
        inject.AddReagent(UraniumReagent,   FixedPoint2.New(5f));

        _blood.TryAddToBloodstream(user, inject);
        _popup.PopupEntity(Loc.GetString("ninja-adrenaline-activate"), user, user);
    }
}
