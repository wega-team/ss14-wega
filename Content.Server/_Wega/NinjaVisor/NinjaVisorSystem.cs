using Content.Shared._Wega.NinjaVisor;
using Content.Shared.Flash.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;

namespace Content.Server._Wega.NinjaVisor;

public sealed class NinjaVisorSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NinjaVisorComponent, CycleNinjaVisorEvent>(OnCycle);
        SubscribeLocalEvent<NinjaVisorComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<NinjaVisorComponent, GotUnequippedEvent>(OnUnequipped);
    }

    private void OnCycle(Entity<NinjaVisorComponent> ent, ref CycleNinjaVisorEvent args)
    {
        var next = ent.Comp.Mode switch
        {
            NinjaVisorMode.Flash => NinjaVisorMode.NightVision,
            NinjaVisorMode.NightVision => NinjaVisorMode.Thermal,
            _ => NinjaVisorMode.Flash,
        };
        SetMode(ent, args.Performer, next);
        args.Handled = true;
    }

    private void OnEquipped(Entity<NinjaVisorComponent> ent, ref GotEquippedEvent args)
    {
        var mode = ent.Comp.Mode == NinjaVisorMode.Off ? NinjaVisorMode.Flash : ent.Comp.Mode;
        SetMode(ent, args.EquipTarget, mode);
    }

    private void OnUnequipped(Entity<NinjaVisorComponent> ent, ref GotUnequippedEvent args)
    {
        RemoveFlash(ent.Owner);
    }

    private void SetMode(Entity<NinjaVisorComponent> ent, EntityUid performer, NinjaVisorMode mode)
    {
        RemoveFlash(ent.Owner);

        ent.Comp.Mode = mode;
        Dirty(ent.Owner, ent.Comp);

        if (mode == NinjaVisorMode.Flash)
            EnsureComp<FlashImmunityComponent>(ent.Owner);

        var msgKey = mode switch
        {
            NinjaVisorMode.Flash => "ninja-visor-mode-flash",
            NinjaVisorMode.NightVision => "ninja-visor-mode-nightvision",
            NinjaVisorMode.Thermal => "ninja-visor-mode-thermal",
            _ => "ninja-visor-mode-flash",
        };
        _popup.PopupEntity(Loc.GetString(msgKey), performer, performer);
    }

    private void RemoveFlash(EntityUid visor)
    {
        RemCompDeferred<FlashImmunityComponent>(visor);
    }
}
