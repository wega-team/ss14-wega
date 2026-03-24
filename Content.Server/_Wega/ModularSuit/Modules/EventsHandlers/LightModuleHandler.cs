using Content.Shared.Actions;
using Content.Shared.Inventory;
using Content.Shared.Light.Components;
using Content.Shared.Modular.Suit;
using Robust.Server.GameObjects;

namespace Content.Server.Modular.Suit;

public sealed class LightModuleHandler : ModuleActionHandler
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedPointLightSystem _light = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ModularSuitActionHolderComponent, ToggleLightModuleEvent>(OnToggleLight);
    }

    private void OnToggleLight(Entity<ModularSuitActionHolderComponent> ent, ref ToggleLightModuleEvent args)
    {
        if (args.Handled)
            return;

        if (!TryFindModuleByAction(ent, args.Action, out var moduleEnt))
            return;

        if (!TryComp<ModularSuitModuleComponent>(moduleEnt, out var moduleComp) || !moduleComp.IsActive)
            return;

        if (!TryComp<ModularSuitLightModuleComponent>(moduleEnt, out var lightModule))
            return;

        var user = args.Performer;
        if (!_inventory.TryGetSlotEntity(user, lightModule.TargetSlot, out var targetEntity))
            return;

        if (!TryComp<PointLightComponent>(targetEntity.Value, out var lightComp))
            return;

        if (lightComp.Enabled)
        {
            _light.SetEnabled(targetEntity.Value, false);
            Audio.PlayPvs(args.TurnOffSound, ent.Owner);
            Popup.PopupEntity(Loc.GetString("modsuit-light-off"), user, user);
            _actions.SetToggled(args.Action.Owner, false);
            if (lightModule.Multicoloured)
                RemComp<RgbLightControllerComponent>(targetEntity.Value);

            args.Handled = true;
            return;
        }

        if (lightModule.Multicoloured)
            EnsureComp<RgbLightControllerComponent>(targetEntity.Value);
        else if (lightComp.Color != lightModule.LightColor)
            _light.SetColor(targetEntity.Value, lightModule.LightColor);

        if (!MathHelper.CloseToPercent(lightComp.Energy, lightModule.LightEnergy))
            _light.SetEnergy(targetEntity.Value, lightModule.LightEnergy);

        if (!MathHelper.CloseToPercent(lightComp.Radius, lightModule.LightRadius))
            _light.SetRadius(targetEntity.Value, lightModule.LightRadius);

        _light.SetEnabled(targetEntity.Value, true);
        Audio.PlayPvs(args.TurnOnSound, ent.Owner);

        Popup.PopupEntity(Loc.GetString("modsuit-light-on"), user, user);
        _actions.SetToggled(args.Action.Owner, true);
        args.Handled = true;
    }
}
