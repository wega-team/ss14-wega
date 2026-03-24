using System.Diagnostics.CodeAnalysis;
using Content.Shared.Modular.Suit;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.Modular.Suit;

public abstract partial class ModuleActionHandler : EntitySystem
{
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] protected readonly SharedContainerSystem Container = default!;
    [Dependency] protected readonly ModularSuitSystem ModularSuit = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;

    public BaseContainer? GetModulesContainer(EntityUid suitUid)
    {
        if (!TryComp<ModularSuitComponent>(suitUid, out var suit) || !suit.Active)
            return null;

        return Container.GetContainer(suitUid, ModularSuitSystem.ModuleContainer);
    }

    public bool TryFindModuleByAction(Entity<ModularSuitActionHolderComponent> suit, EntityUid actionUid, [NotNullWhen(true)] out EntityUid? moduleEnt)
    {
        moduleEnt = null;

        EntProtoId? actionId = null;
        foreach (var kvp in suit.Comp.ModuleActions)
        {
            if (kvp.Value == actionUid)
            {
                actionId = kvp.Key;
                break;
            }
        }

        if (actionId == null)
            return false;

        var container = GetModulesContainer(suit);
        if (container == null)
            return false;

        foreach (var module in container.ContainedEntities)
        {
            if (!TryComp<ModularSuitActionModuleComponent>(module, out var moduleAction))
                continue;

            if (moduleAction.Action == actionId)
            {
                moduleEnt = module;
                return true;
            }
        }

        return false;
    }
}
