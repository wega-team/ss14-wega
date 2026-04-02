using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Modular.Suit;
using Content.Shared.PowerCell;
using Robust.Shared.Containers;

namespace Content.Server.Modular.Suit;

public sealed partial class ModularSuitSystem
{
    [Dependency] private readonly PowerCellSystem _powerCell = default!;

    public const string CellContainer = "cell_slot";

    private void InitializePower()
    {
        SubscribeLocalEvent<ModularSuitComponent, ModularSuitRefreshPowerEvent>(OnRefreshPower);
        SubscribeLocalEvent<ModularSuitComponent, ModularSuitInstalledEvent>(OnModuleInstalledRefresh);
        SubscribeLocalEvent<ModularSuitComponent, ModularSuitRemovedEvent>(OnModuleRemovedRefresh);
        SubscribeLocalEvent<ModularSuitComponent, ModularSuitModuleToggledEvent>(OnModuleToggledRefresh);
        SubscribeLocalEvent<ModularSuitComponent, EntRemovedFromContainerMessage>(OnCellRemoved);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ModularSuitComponent>();
        while (query.MoveNext(out var uid, out var suit))
        {
            if (!suit.Active)
                continue;

            if (GameTiming.CurTime < suit.NextUpdate)
                continue;

            suit.NextUpdate = GameTiming.CurTime + suit.UpdateInterval;
            UpdatePower((uid, suit));
        }
    }

    public override void SetActive(Entity<ModularSuitComponent> ent, bool active)
    {
        if (ent.Comp.Active == active)
            return;

        if (active && !ent.Comp.Assembled)
        {
            Popup.PopupEntity(Loc.GetString("modsuit-not-assembled"), ent, ent.Comp.Wearer ?? ent);
            UpdateUiState(ent);
            return;
        }

        if (active && !HasCore(ent))
        {
            Popup.PopupEntity(Loc.GetString("modsuit-no-core"), ent, ent.Comp.Wearer ?? ent);
            UpdateUiState(ent);
            return;
        }

        if (active && !ent.Comp.Active)
        {
            _audio.PlayPvs(ent.Comp.NominalSound, ent.Owner);
        }

        if (HasComp<ItemToggleComponent>(ent))
        {
            if (active)
                Toggle.TryActivate(ent.Owner, ent.Comp.Wearer, false);
            else
                Toggle.TryDeactivate(ent.Owner, ent.Comp.Wearer, false);
        }

        if (!active && TryComp<ModularSuitEquippedComponent>(ent, out var equipped))
        {
            foreach (var (_, partUid) in equipped.EquippedParts)
            {
                if (TryComp<ItemToggleComponent>(partUid, out var partToggle) && Toggle.IsActivated((partUid, partToggle)))
                    Toggle.TryDeactivate(partUid, ent.Comp.Wearer);
            }

            CheckSuitAssembly(ent.Owner);
        }

        if (active)
        {
            if (ent.Comp.SlotActiveComponents != null && ent.Comp.Wearer != null)
            {
                foreach (var (slot, components) in ent.Comp.SlotActiveComponents)
                {
                    if (Inventory.TryGetSlotEntity(ent.Comp.Wearer.Value, slot, out var targetEntity))
                        EntityManager.AddComponents(targetEntity.Value, components);
                }
            }
        }
        else
        {
            if (ent.Comp.SlotActiveComponents != null && ent.Comp.Wearer != null)
            {
                foreach (var (slot, components) in ent.Comp.SlotActiveComponents)
                {
                    if (Inventory.TryGetSlotEntity(ent.Comp.Wearer.Value, slot, out var targetEntity))
                        EntityManager.RemoveComponents(targetEntity.Value, components);
                }
            }
        }

        base.SetActive(ent, active);

        var ev = new ModularSuitRefreshPowerEvent();
        RaiseLocalEvent(ent, ref ev);

        UpdateUiState(ent);
    }

    private void OnRefreshPower(Entity<ModularSuitComponent> ent, ref ModularSuitRefreshPowerEvent args)
    {
        RefreshPowerState(ent);

        var modules = GetCurrentModules(ent);
        if (!ent.Comp.Active)
        {
            foreach (var module in modules)
            {
                if (TryComp<ModularSuitModuleComponent>(module, out var mod) && mod.IsActive && mod.CanBeDisabled)
                {
                    mod.IsActive = false;
                    Dirty(module, mod);

                    var ev = new ModularSuitModuleToggledEvent(ent, ent.Comp.Wearer, false);
                    RaiseLocalEvent(module, ref ev);
                }
            }
        }
        else
        {
            foreach (var module in modules)
            {
                if (TryComp<ModularSuitModuleComponent>(module, out var mod) && (mod.IsActive || !mod.CanBeDisabled))
                {
                    var ev = new ModularSuitModuleToggledEvent(ent, ent.Comp.Wearer, true);
                    RaiseLocalEvent(module, ref ev);
                }
            }
        }
    }

    private void OnModuleInstalledRefresh(Entity<ModularSuitComponent> ent, ref ModularSuitInstalledEvent args)
    {
        RefreshPowerState(ent);
        UpdateUiState(ent);
    }

    private void OnModuleRemovedRefresh(Entity<ModularSuitComponent> ent, ref ModularSuitRemovedEvent args)
    {
        RefreshPowerState(ent);
        UpdateUiState(ent);
    }

    private void OnModuleToggledRefresh(Entity<ModularSuitComponent> ent, ref ModularSuitModuleToggledEvent args)
    {
        RefreshPowerState(ent);
        UpdateUiState(ent);
    }

    private void OnCellRemoved(Entity<ModularSuitComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != CellContainer)
            return;

        RefreshPowerState(ent);
        UpdateUiState(ent);
    }

    private bool HasCore(Entity<ModularSuitComponent> ent)
    {
        var coreContainer = Container.GetContainer(ent, CoreContainer);
        return coreContainer.ContainedEntities.Count > 0;
    }

    private void RefreshPowerState(Entity<ModularSuitComponent> ent)
    {
        ent.Comp.NextUpdate = GameTiming.CurTime + ent.Comp.UpdateInterval;
    }

    private void UpdatePower(Entity<ModularSuitComponent> suit)
    {
        var coreContainer = Container.GetContainer(suit.Owner, CoreContainer);
        if (coreContainer.ContainedEntities.Count == 0)
        {
            SetActive(suit, false);
            return;
        }

        var coreEnt = coreContainer.ContainedEntities[0];
        if (!TryComp<ModularSuitCoreComponent>(coreEnt, out var core))
        {
            SetActive(suit, false);
            return;
        }

        if (core.Infinite)
            return;

        float totalDraw = suit.Comp.BasePowerDraw;
        foreach (var module in GetCurrentModules(suit))
        {
            if (TryComp<ModularSuitModuleComponent>(module, out var mod) && mod.IsActive)
                totalDraw += mod.PowerUsage;
        }

        totalDraw *= core.DrawMultiplier;
        if (totalDraw <= 0)
            return;

        var chargeToUse = totalDraw * (float)suit.Comp.UpdateInterval.TotalSeconds;
        var newCharge = Math.Max(0, core.Charge - chargeToUse);
        var used = core.Charge - newCharge;
        core.Charge = newCharge;
        Dirty(coreEnt, core);

        if (used > 0)
        {
            var ev = new ModularSuitChargeChangedEvent(core.Charge, core.MaxCharge);
            RaiseLocalEvent(coreEnt, ref ev);
        }

        if (core.Charge <= 0)
        {
            SetActive(suit, false);
            UpdateUiState(suit);

            _audio.PlayPvs(suit.Comp.CriticalDestroySound, suit.Owner);
        }

        if (core.Charge < core.MaxCharge)
        {
            TryChargeFromBattery(suit.Owner, core, suit);
            UpdateUiState(suit);
        }

        if (!core.Infinite)
        {
            if (core.Charge <= core.MaxCharge * 0.4f && GameTiming.CurTime >= suit.Comp.NextLowPowerSound)
            {
                suit.Comp.NextLowPowerSound = GameTiming.CurTime + suit.Comp.LowPowerCooldown;
                _audio.PlayPvs(suit.Comp.LowPowerSound, suit.Owner);
            }
        }
    }

    private void TryChargeFromBattery(EntityUid uid, ModularSuitCoreComponent core, ModularSuitComponent suit)
    {
        if (!_powerCell.TryGetBatteryFromSlot(uid, out _))
            return;

        var needed = core.MaxCharge - core.Charge;
        var maxTransfer = core.ChargeRate * (float)suit.UpdateInterval.TotalSeconds;
        var transfer = Math.Min(needed, maxTransfer);

        if (transfer <= 0)
            return;

        if (_powerCell.TryUseCharge(uid, transfer, predicted: false))
        {
            core.Charge += transfer;
            Dirty(uid, core);
            var ev = new ModularSuitChargeChangedEvent(core.Charge, core.MaxCharge);
            RaiseLocalEvent(uid, ref ev);
        }
    }

    public void UseCoreCharge(Entity<ModularSuitComponent?> suit, float amount)
    {
        if (!Resolve(suit, ref suit.Comp) || amount <= 0)
            return;

        var coreContainer = Container.GetContainer(suit, CoreContainer);
        if (coreContainer.ContainedEntities.Count == 0)
            return;

        var coreEnt = coreContainer.ContainedEntities[0];
        if (!TryComp<ModularSuitCoreComponent>(coreEnt, out var core))
            return;

        if (core.Infinite)
            return;

        var actualUse = Math.Min(core.Charge, amount);
        core.Charge -= actualUse;
        Dirty(coreEnt, core);

        if (actualUse > 0)
        {
            var ev = new ModularSuitChargeChangedEvent(core.Charge, core.MaxCharge);
            RaiseLocalEvent(coreEnt, ref ev);
        }

        if (core.Charge <= 0 && suit.Comp.Active)
        {
            SetActive((suit.Owner, suit.Comp), false);
            UpdateUiState((suit.Owner, suit.Comp));
        }
    }
}
