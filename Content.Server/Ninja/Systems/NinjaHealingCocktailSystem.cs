using Content.Server.Body.Systems;
using Content.Server.Ninja.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Ninja.Components;
using Content.Shared.Popups;
using Content.Shared.Timing;

namespace Content.Server.Ninja.Systems;

public sealed class NinjaHealingCocktailSystem : EntitySystem
{
    private const string ChiurizinReagent = "Chiurizin";
    private const string SuitDisableDelayId = "suit_powers";

    [Dependency] private readonly NinjaCloakSystem _cloak = default!;
    [Dependency] private readonly BloodstreamSystem _blood = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NinjaSuitComponent, NinjaHealingCocktailEvent>(OnHealingCocktail);
    }

    private void OnHealingCocktail(Entity<NinjaSuitComponent> ent, ref NinjaHealingCocktailEvent args)
    {
        var suit = ent.Owner;
        var user = args.Performer;

        if (_cloak.TryRevealCloak(user))
            return;

        args.Handled = true;

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
        inject.AddReagent(ChiurizinReagent, FixedPoint2.New(20f));
        _blood.TryAddToBloodstream(user, inject);

        _popup.PopupEntity(Loc.GetString("ninja-healing-cocktail-activate"), user, user, PopupType.Medium);
    }
}
