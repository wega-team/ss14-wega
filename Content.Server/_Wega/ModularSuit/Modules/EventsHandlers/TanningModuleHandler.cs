using System.Linq;
using Content.Shared.Body;
using Content.Shared.Modular.Suit;

namespace Content.Server.Modular.Suit;

public sealed class TanningModuleHandler : ModuleActionHandler
{
    [Dependency] private readonly SharedVisualBodySystem _visualBody = default!;

    private const float MinColorValue = 0.3f;
    private const float DarkenFactor = 0.85f;

    public override void Initialize()
    {
        SubscribeLocalEvent<ModularSuitActionHolderComponent, ActivateTanningModuleEvent>(OnTan);
    }

    private void OnTan(Entity<ModularSuitActionHolderComponent> ent, ref ActivateTanningModuleEvent args)
    {
        if (args.Handled)
            return;

        if (!TryFindModuleByAction(ent, args.Action, out var moduleEnt))
            return;

        if (!TryComp<ModularSuitModuleComponent>(moduleEnt, out var moduleComp) || !moduleComp.IsActive)
            return;

        var user = args.Performer;
        if (!_visualBody.TryGatherMarkingsData(user, null, out var profiles, out _, out _))
            return;

        Color currentColor = Color.White;
        foreach (var profile in profiles.Values)
        {
            currentColor = profile.SkinColor;
            break;
        }

        if (currentColor.R <= MinColorValue
            && currentColor.G <= MinColorValue
            && currentColor.B <= MinColorValue)
        {
            Popup.PopupEntity(Loc.GetString("modsuit-tanning-max"), user, user);
            return;
        }

        var newColor = new Color(
            Math.Max(MinColorValue, currentColor.R * DarkenFactor),
            Math.Max(MinColorValue, currentColor.G * DarkenFactor),
            Math.Max(MinColorValue, currentColor.B * DarkenFactor)
        );

        var updatedProfiles = profiles.ToDictionary(
            pair => pair.Key,
            pair => pair.Value with { SkinColor = newColor });

        _visualBody.ApplyProfiles(user, updatedProfiles);

        ModularSuit.UseCoreCharge(ent.Owner, moduleComp.PowerInstanceUsage);
        Popup.PopupEntity(Loc.GetString("modsuit-tanning-used"), user, user);

        args.Handled = true;
    }
}
