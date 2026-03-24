using Content.Server.Temperature.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Components;
using Content.Shared.Modular.Suit;
using Content.Shared.Popups;
using Content.Shared.Temperature.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Modular.Suit;

public sealed class ModularSuitMicrowaveSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly ModularSuitSystem _modularSuit = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TemperatureSystem _temperature = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ModularSuitMicrowaveModuleComponent, ModularSuitModuleItemCreatedEvent>(OnCreated);

        SubscribeLocalEvent<ModularSuitMicrowaveModuleComponent, AfterInteractEvent>(OnInteract);
        SubscribeLocalEvent<ModularSuitMicrowaveModuleComponent, ModuleMicrowaveDoAfterEvent>(OnCookComplete);
    }

    private void OnCreated(Entity<ModularSuitMicrowaveModuleComponent> module, ref ModularSuitModuleItemCreatedEvent args)
    {
        module.Comp.Module = args.Module;
        Dirty(module.Owner, module.Comp);
    }

    private void OnInteract(Entity<ModularSuitMicrowaveModuleComponent> module, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target == null || args.Target == args.User)
            return;

        if (!TryComp<ModularSuitCarrierComponent>(args.User, out var carrier) || string.IsNullOrEmpty(carrier.CurrentSlot)
            || !_inventory.TryGetSlotEntity(args.User, carrier.CurrentSlot, out var suit))
            return;

        if (!TryComp<ModularSuitComponent>(suit.Value, out var suitComp) || suitComp.Wearer == null || !suitComp.Active)
        {
            _popup.PopupEntity(Loc.GetString("modsuit-microwave-not-active"), args.User, args.User);
            return;
        }

        if (!CanHeat(args.Target.Value))
        {
            _popup.PopupEntity(Loc.GetString("modsuit-microwave-cannot-cook"), args.User, args.User);
            return;
        }

        if (module.Comp.AudioStream != null)
        {
            _popup.PopupEntity(Loc.GetString("modsuit-microwave-already-cooking"), args.User, args.User);
            return;
        }

        _audio.PlayPvs(module.Comp.StartSound, args.User);

        module.Comp.AudioStream = _audio.PlayPvs(module.Comp.LoopingSound, args.User, AudioParams.Default.WithLoop(true))?.Entity;
        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, module.Comp.CookDelay, new ModuleMicrowaveDoAfterEvent(), module, args.Target, module)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 0.01f
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        _popup.PopupEntity(Loc.GetString("modsuit-microwave-start-cook", ("target", Name(args.Target.Value))), args.User, args.User);

        args.Handled = true;
    }

    private void OnCookComplete(Entity<ModularSuitMicrowaveModuleComponent> module, ref ModuleMicrowaveDoAfterEvent args)
    {
        if (module.Comp.AudioStream != null)
        {
            _audio.Stop(module.Comp.AudioStream.Value);
            module.Comp.AudioStream = null;
        }

        if (args.Handled || args.Args.Target == null)
            return;

        if (args.Cancelled)
        {
            _popup.PopupEntity(Loc.GetString("modsuit-microwave-cancelled"), args.Args.User, args.Args.User);
            return;
        }

        if (!CanHeat(args.Args.Target.Value))
            return;

        _temperature.ChangeHeat(args.Args.Target.Value, module.Comp.HeatAmount);

        _audio.PlayPvs(module.Comp.CompleteSound, args.Args.User);
        _popup.PopupEntity(Loc.GetString("modsuit-microwave-cooked", ("target", Name(args.Args.Target.Value))), args.Args.User, args.Args.User);

        if (module.Comp.Module != null && TryComp<ModularSuitModuleComponent>(module.Comp.Module.Value, out var moduleComp))
        {
            if (TryComp<ModularSuitCarrierComponent>(args.User, out var carrier) && !string.IsNullOrEmpty(carrier.CurrentSlot)
                && _inventory.TryGetSlotEntity(args.User, carrier.CurrentSlot, out var suit))
            {
                _modularSuit.UseCoreCharge(suit.Value, moduleComp.PowerInstanceUsage);
            }
        }

        args.Handled = true;
    }

    private bool CanHeat(EntityUid uid)
    {
        if (HasComp<MobStateComponent>(uid))
            return false;

        if (!HasComp<TemperatureComponent>(uid))
            return false;

        return true;
    }
}
