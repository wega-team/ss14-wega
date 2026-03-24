using Content.Shared.Modular.Suit;
using Content.Shared.Inventory;
using Content.Shared.Interaction;
using Content.Shared.Tools.Systems;
using Robust.Server.GameObjects;

namespace Content.Server.Modular.Suit;

public sealed class ModularSuitLightModuleSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedPointLightSystem _light = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ModularSuitLightModuleComponent, AfterInteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<ModularSuitLightModuleComponent, BoundUIOpenedEvent>(OnUIOpened);
        SubscribeLocalEvent<ModularSuitLightModuleComponent, UpdateLightModuleMessage>(OnUpdateLightModule);

        SubscribeLocalEvent<ModularSuitLightModuleComponent, ModularSuitRemovedEvent>(OnModuleRemoved);
        SubscribeLocalEvent<ModularSuitLightModuleComponent, ModularSuitModuleToggledEvent>(OnModuleToggled);
    }

    private void OnInteractUsing(EntityUid uid, ModularSuitLightModuleComponent comp, AfterInteractUsingEvent args)
    {
        var used = args.Used;
        if (!_tool.HasQuality(used, comp.Tool))
            return;

        OpenUi(args.User, uid);
    }

    private void OpenUi(EntityUid user, EntityUid module)
    {
        if (_ui.IsUiOpen(module, LightModuleUiKey.Key))
            return;

        _ui.OpenUi(module, LightModuleUiKey.Key, user);
    }

    private void OnUIOpened(Entity<ModularSuitLightModuleComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUiState(ent);
    }

    private void OnUpdateLightModule(Entity<ModularSuitLightModuleComponent> ent, ref UpdateLightModuleMessage args)
    {
        ent.Comp.LightColor = args.LightColor;
        ent.Comp.Multicoloured = args.Multicoloured;
        Dirty(ent.Owner, ent.Comp);

        UpdateUiState(ent);
    }

    private void UpdateUiState(Entity<ModularSuitLightModuleComponent> ent)
    {
        if (!_ui.HasUi(ent.Owner, LightModuleUiKey.Key))
            return;

        var state = new LightModuleBoundUserInterfaceState(ent.Comp.LightColor, ent.Comp.Multicoloured);
        _ui.SetUiState(ent.Owner, LightModuleUiKey.Key, state);
    }

    private void OnModuleRemoved(Entity<ModularSuitLightModuleComponent> module, ref ModularSuitRemovedEvent args)
    {
        RemoveLight(args.User, module);
    }

    private void OnModuleToggled(Entity<ModularSuitLightModuleComponent> module, ref ModularSuitModuleToggledEvent args)
    {
        if (args.Activated)
            return;

        RemoveLight(args.Wearer, module);
    }

    private void RemoveLight(EntityUid? user, Entity<ModularSuitLightModuleComponent> module)
    {
        if (user == null)
            return;

        if (!_inventory.TryGetSlotEntity(user.Value, module.Comp.TargetSlot, out var targetEntity))
            return;

        _light.SetEnabled(targetEntity.Value, false);
        if (module.Comp.GuaranteedRemoved != null)
        {
            EntityManager.RemoveComponents(targetEntity.Value, module.Comp.GuaranteedRemoved);
        }
    }
}
