using System.Diagnostics.CodeAnalysis;
using Content.Shared.Clothing.Components;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Modular.Suit;
using Content.Shared.Tools.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;

namespace Content.Server.Modular.Suit;

public sealed class ModularSuitLightModuleSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
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
        RemoveLight(args.User, args.Suit, module.Comp);
    }

    private void OnModuleToggled(Entity<ModularSuitLightModuleComponent> module, ref ModularSuitModuleToggledEvent args)
    {
        if (args.Activated)
            return;

        RemoveLight(args.Wearer, args.Suit, module.Comp);
    }

    private void RemoveLight(EntityUid? user, EntityUid suit, ModularSuitLightModuleComponent component)
    {
        if (user == null)
            return;

        if (!TryGetTargetEntity(user, suit, component.TargetSlot, out var targetEntity))
            return;

        _light.SetEnabled(targetEntity.Value, false);
        if (component.GuaranteedRemoved != null)
        {
            EntityManager.RemoveComponents(targetEntity.Value, component.GuaranteedRemoved);
        }
    }

    private bool TryGetTargetEntity(EntityUid? user, EntityUid suit, string targetSlot, [NotNullWhen(true)] out EntityUid? targetEntity)
    {
        targetEntity = null;
        if (user != null && _inventory.TryGetSlotEntity(user.Value, targetSlot, out var wearerSlot))
        {
            targetEntity = wearerSlot;
            return true;
        }

        if (targetSlot == "back")
        {
            targetEntity = suit;
            return true;
        }

        var partContainer = _container.GetContainer(suit, SharedModularSuitSystem.PartContainer);
        foreach (var part in partContainer.ContainedEntities)
        {
            if (TryComp<ClothingComponent>(part, out var clothing) && clothing.Slots.HasFlag(GetSlotFlag(targetSlot)))
            {
                targetEntity = part;
                return true;
            }
        }

        return false;
    }

    private SlotFlags GetSlotFlag(string slot)
    {
        return slot switch
        {
            "head" => SlotFlags.HEAD,
            "eyes" => SlotFlags.EYES,
            "ears" => SlotFlags.EARS,
            "mask" => SlotFlags.MASK,
            "outerClothing" => SlotFlags.OUTERCLOTHING,
            "jumpsuit" => SlotFlags.INNERCLOTHING,
            "neck" => SlotFlags.NECK,
            "belt" => SlotFlags.BELT,
            "gloves" => SlotFlags.GLOVES,
            "shoes" => SlotFlags.FEET,
            _ => SlotFlags.NONE
        };
    }
}
