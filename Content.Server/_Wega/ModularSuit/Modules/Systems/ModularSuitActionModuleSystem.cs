using Content.Shared.Actions;
using Content.Shared.Clothing.Components;
using Content.Shared.Modular.Suit;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.Modular.Suit;

public sealed class ModularSuitActionModuleSystem : EntitySystem
{
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ModularSuitActionModuleComponent, ModularSuitInstalledEvent>(OnModuleInstalled);
        SubscribeLocalEvent<ModularSuitActionModuleComponent, ModularSuitRemovedEvent>(OnModuleRemoved);
        SubscribeLocalEvent<ModularSuitActionModuleComponent, ModularSuitModuleToggledEvent>(OnModuleToggled);

        SubscribeLocalEvent<ModularSuitActionHolderComponent, GetItemActionsEvent>(OnGetItemActions);
        SubscribeLocalEvent<ModularSuitActionHolderComponent, ModularSuitRefreshPowerEvent>(OnPowerRefresh);
    }

    private void OnModuleInstalled(Entity<ModularSuitActionModuleComponent> module, ref ModularSuitInstalledEvent args)
    {
        if (!TryComp<ModularSuitModuleComponent>(module.Owner, out var moduleComp) || !moduleComp.IsActive)
            return;

        AddActionToSuit(args.Suit, module.Comp.Action);
    }

    private void OnModuleRemoved(Entity<ModularSuitActionModuleComponent> module, ref ModularSuitRemovedEvent args)
    {
        RemoveActionFromSuit(args.Suit, module.Comp.Action);
    }

    private void OnModuleToggled(Entity<ModularSuitActionModuleComponent> module, ref ModularSuitModuleToggledEvent args)
    {
        if (args.Activated)
        {
            AddActionToSuit(args.Suit, module.Comp.Action);
        }
        else
        {
            RemoveActionFromSuit(args.Suit, module.Comp.Action);
        }
    }

    private void AddActionToSuit(EntityUid suit, EntProtoId actionId)
    {
        var actionHolder = EnsureComp<ModularSuitActionHolderComponent>(suit);
        if (actionHolder.ModuleActions.ContainsKey(actionId))
            return;

        var actionEnt = _actionContainer.AddAction(suit, actionId);

        if (actionEnt != null)
        {
            actionHolder.ModuleActions[actionId] = actionEnt.Value;
            Dirty(suit, actionHolder);
        }

        UpdateActions(suit);
    }

    private void RemoveActionFromSuit(EntityUid suit, EntProtoId actionId)
    {
        if (!TryComp<ModularSuitActionHolderComponent>(suit, out var actionHolder))
            return;

        if (actionHolder.ModuleActions.TryGetValue(actionId, out var action))
        {
            _actionContainer.RemoveAction(action);
            actionHolder.ModuleActions.Remove(actionId);
            Dirty(suit, actionHolder);
        }

        if (actionHolder.ModuleActions.Count == 0)
        {
            RemComp<ModularSuitActionHolderComponent>(suit);
        }

        UpdateActions(suit);
    }

    private void UpdateActions(EntityUid suit)
    {
        if (!TryComp<ModularSuitComponent>(suit, out var suitComp) || suitComp.Wearer == null)
            return;

        if (TryComp<ClothingComponent>(suit, out var clothing) && clothing.InSlotFlag != null)
        {
            var ev = new GetItemActionsEvent(_actionContainer, suitComp.Wearer.Value, suit, clothing.InSlotFlag.Value);
            RaiseLocalEvent(suit, ev);
            if (ev.Actions.Count == 0)
                return;

            _actions.GrantActions(suitComp.Wearer.Value, ev.Actions, suit);
        }
    }

    private void OnGetItemActions(Entity<ModularSuitActionHolderComponent> ent, ref GetItemActionsEvent args)
    {
        if (args.InHands)
            return;

        foreach (var action in ent.Comp.ModuleActions.Values)
            args.AddAction(action);
    }

    private void OnPowerRefresh(Entity<ModularSuitActionHolderComponent> ent, ref ModularSuitRefreshPowerEvent args)
    {
        if (!TryComp<ModularSuitComponent>(ent, out var suit) || suit.Active || suit.Wearer == null)
            return;

        var moduleContainer = _container.GetContainer(ent.Owner, ModularSuitSystem.ModuleContainer);
        foreach (var module in moduleContainer.ContainedEntities)
            _ui.CloseUis(module, suit.Wearer.Value);
    }
}
