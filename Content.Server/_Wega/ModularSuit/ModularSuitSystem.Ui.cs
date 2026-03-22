using System.Linq;
using Content.Shared.Modular.Suit;
using Robust.Server.GameObjects;

namespace Content.Server.Modular.Suit;

public sealed partial class ModularSuitSystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    private void InitializeUi()
    {
        SubscribeLocalEvent<ModularSuitComponent, BoundUIOpenedEvent>(OnUIOpened);

        SubscribeLocalEvent<ModularSuitComponent, ToggleSuitActiveMessage>(OnToggleActiveMessage);
        SubscribeLocalEvent<ModularSuitComponent, ToggleModuleMessage>(OnToggleModuleMessage);
        SubscribeLocalEvent<ModularSuitComponent, ModularSuitChargeChangedEvent>(OnChargeChanged);
        SubscribeLocalEvent<ModularSuitComponent, ModularSuitActiveChangedEvent>(OnActiveChanged);
    }

    private void OnUIOpened(Entity<ModularSuitComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUiState(ent);
    }

    private void OnToggleActiveMessage(Entity<ModularSuitComponent> ent, ref ToggleSuitActiveMessage args)
    {
        SetActive(ent, args.Active);
        UpdateUiState(ent);
    }

    private void OnToggleModuleMessage(Entity<ModularSuitComponent> ent, ref ToggleModuleMessage args)
    {
        if (!ent.Comp.Active)
        {
            Popup.PopupPredicted(Loc.GetString("modsuit-not-active"), ent, null);
            UpdateUiState(ent);
            return;
        }

        var moduleEnt = GetEntity(args.ModuleUid);
        if (!TryComp<ModularSuitModuleComponent>(moduleEnt, out var module))
            return;

        if (!module.CanBeDisabled)
            return;

        if (args.Active)
        {
            var attemptEvent = new ModularSuitModuleAttemptEvent(ent.Owner);
            RaiseLocalEvent(moduleEnt, ref attemptEvent);

            if (attemptEvent.Cancelled)
                return;
        }

        module.IsActive = args.Active;
        Dirty(moduleEnt, module);

        var ev = new ModularSuitModuleToggledEvent(ent, ent.Comp.Wearer, args.Active);
        RaiseLocalEvent(moduleEnt, ref ev);

        UpdateUiState(ent);
    }

    private void OnChargeChanged(Entity<ModularSuitComponent> ent, ref ModularSuitChargeChangedEvent args)
    {
        UpdateUiState(ent);
    }

    private void OnActiveChanged(Entity<ModularSuitComponent> ent, ref ModularSuitActiveChangedEvent args)
    {
        UpdateUiState(ent);
    }

    private void UpdateUiState(Entity<ModularSuitComponent> ent)
    {
        if (!_ui.HasUi(ent.Owner, ModularSuitUiKey.Key))
            return;

        float coreCharge = 0;
        float maxCoreCharge = 100;
        bool hasCore = false;

        var coreContainer = Container.GetContainer(ent, CoreContainer);
        if (coreContainer.ContainedEntities.Count > 0)
        {
            if (TryComp<ModularSuitCoreComponent>(coreContainer.ContainedEntities[0], out var core))
            {
                coreCharge = core.Charge;
                maxCoreCharge = core.MaxCharge;
                hasCore = true;
            }
        }

        bool hasBattery = _powerCell.HasBattery(ent.Owner);

        var modules = new List<SuitModuleEntry>();
        var moduleContainer = Container.GetContainer(ent, ModuleContainer);
        foreach (var moduleUid in moduleContainer.ContainedEntities)
        {
            if (!TryComp<ModularSuitModuleComponent>(moduleUid, out var module))
                continue;

            modules.Add(new SuitModuleEntry(
                GetNetEntity(moduleUid),
                Name(moduleUid),
                module.ModuleId,
                module.IsActive,
                module.IsPermanent,
                module.PowerUsage,
                module.PowerInstanceUsage,
                module.CanBeDisabled,
                module.Tags.Select(t => t.ToString()).ToList()
            ));
        }

        var parts = new List<SuitPartEntry>();
        var partContainer = Container.GetContainer(ent, PartContainer);

        if (partContainer.ContainedEntities.Count > 0)
        {
            foreach (var partUid in partContainer.ContainedEntities)
            {
                if (!TryComp<ModularSuitPartComponent>(partUid, out var part))
                    continue;

                parts.Add(new SuitPartEntry(
                    GetNetEntity(partUid),
                    Name(partUid),
                    part.PartType
                ));
            }
        }

        if (TryComp<ModularSuitEquippedComponent>(ent, out var equipped)
            && equipped.EquippedParts.Count > 0)
        {
            foreach (var (_, partUid) in equipped.EquippedParts)
            {
                if (!TryComp<ModularSuitPartComponent>(partUid, out var part))
                    continue;

                parts.Add(new SuitPartEntry(
                    GetNetEntity(partUid),
                    Name(partUid),
                    part.PartType
                ));
            }
        }

        string? wearerName = null;
        if (ent.Comp.Wearer != null)
            wearerName = Name(ent.Comp.Wearer.Value);

        var state = new ModularSuitBoundUserInterfaceState(
            ent.Comp.Active,
            coreCharge,
            maxCoreCharge,
            hasCore,
            hasBattery,
            modules,
            parts,
            wearerName
        );

        _ui.SetUiState(ent.Owner, ModularSuitUiKey.Key, state);
    }
}
