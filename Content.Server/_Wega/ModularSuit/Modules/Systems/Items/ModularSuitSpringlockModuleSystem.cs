using Content.Shared.Clothing;
using Content.Shared.Inventory.Events;
using Content.Shared.Modular.Suit;
using Content.Shared.Popups;

namespace Content.Server.Modular.Suit;

public sealed class ModularSuitSpringlockModuleSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ModularSuitSpringlockModuleComponent, ModularSuitInstalledEvent>(OnModuleInstalled);
        SubscribeLocalEvent<ModularSuitSpringlockModuleComponent, ModularSuitRemovedEvent>(OnModuleRemoved);

        SubscribeLocalEvent<ModularSuitSpringlockInstalledComponent, BeingEquippedAttemptEvent>(OnEquippedAttempt);
        SubscribeLocalEvent<ModularSuitSpringlockInstalledComponent, BeingUnequippedAttemptEvent>(OnUnequippedAttempt);
        SubscribeLocalEvent<ModularSuitSpringlockInstalledComponent, ClothingGotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<ModularSuitSpringlockInstalledComponent, GotUnequippedEvent>(OnUnequipped);
    }

    private void OnModuleInstalled(Entity<ModularSuitSpringlockModuleComponent> module, ref ModularSuitInstalledEvent args)
    {
        if (!TryComp<ModularSuitComponent>(args.Suit, out var modular))
            return;

        EnsureComp<ModularSuitSpringlockInstalledComponent>(args.Suit).Module = module.Owner;
        if (modular.Wearer != null)
        {
            EnsureComp<AffectedModuleSpringlockComponent>(modular.Wearer.Value);
        }
    }

    private void OnModuleRemoved(Entity<ModularSuitSpringlockModuleComponent> module, ref ModularSuitRemovedEvent args)
    {
        if (!TryComp<ModularSuitComponent>(args.Suit, out var modular))
            return;

        RemComp<ModularSuitSpringlockInstalledComponent>(args.Suit);
        if (modular.Wearer != null)
        {
            RemComp<AffectedModuleSpringlockComponent>(modular.Wearer.Value);
        }
    }

    private void OnEquippedAttempt(Entity<ModularSuitSpringlockInstalledComponent> ent, ref BeingEquippedAttemptEvent args)
    {
        if (ent.Comp.Module == null || !TryComp<ModularSuitModuleComponent>(ent.Comp.Module.Value, out var module))
            return;

        if (!module.IsPermanent)
            return;

        args.Cancel();
    }

    private void OnUnequippedAttempt(Entity<ModularSuitSpringlockInstalledComponent> ent, ref BeingUnequippedAttemptEvent args)
    {
        if (ent.Comp.Module == null || !TryComp<ModularSuitModuleComponent>(ent.Comp.Module.Value, out var module))
            return;

        if (!module.IsPermanent)
            return;

        _popup.PopupCursor(Loc.GetString("modsuit-springlock-unequipped-failed"), args.UnEquipTarget);
        args.Cancel();
    }

    private void OnEquipped(Entity<ModularSuitSpringlockInstalledComponent> ent, ref ClothingGotEquippedEvent args)
    {
        EnsureComp<AffectedModuleSpringlockComponent>(args.Wearer);
    }

    private void OnUnequipped(Entity<ModularSuitSpringlockInstalledComponent> ent, ref GotUnequippedEvent args)
    {
        RemComp<AffectedModuleSpringlockComponent>(args.EquipTarget);
    }
}
