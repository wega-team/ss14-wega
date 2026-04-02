using System.Linq;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Components;
using Content.Shared.Modular.Suit;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.Server.Modular.Suit;

public sealed class ModularSuitGrabberModuleSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly ModularSuitSystem _modularSuit = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public const string Container = "grabber_storage";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ModularSuitGrabberToolComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ModularSuitGrabberToolComponent, AfterInteractEvent>(OnInteract);
        SubscribeLocalEvent<ModularSuitGrabberToolComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
        SubscribeLocalEvent<ModularSuitGrabberToolComponent, ModularSuitModuleItemCreatedEvent>(OnCreated);
        SubscribeLocalEvent<ModularSuitGrabberToolComponent, ModuleGrabberDoAfterEvent>(OnGrabComplete);

        SubscribeLocalEvent<ModularSuitGrabberModuleComponent, ModularSuitRemovedEvent>(OnModuleRemoved);
        SubscribeLocalEvent<ModularSuitGrabberModuleComponent, ModularSuitModuleToggledEvent>(OnModuleToggled);
    }

    private void OnMapInit(Entity<ModularSuitGrabberToolComponent> tool, ref MapInitEvent args)
    {
        _container.EnsureContainer<Container>(tool, Container);
    }

    private void OnInteract(Entity<ModularSuitGrabberToolComponent> tool, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null || args.Target == args.User)
            return;

        if (IsUnGrabable(args.Target.Value))
        {
            _popup.PopupEntity(Loc.GetString("modsuit-grabber-cannot-grab"), args.User, args.User, PopupType.Medium);
            return;
        }

        if (!TryComp<ModularSuitCarrierComponent>(args.User, out var carrier) || string.IsNullOrEmpty(carrier.CurrentSlot)
            || !_inventory.TryGetSlotEntity(args.User, carrier.CurrentSlot, out var suit))
            return;

        if (!TryComp<ModularSuitComponent>(suit.Value, out var suitComp) || suitComp.Wearer == null || !suitComp.Active)
        {
            _popup.PopupEntity(Loc.GetString("modsuit-grabber-not-active"), args.User, args.User, PopupType.Medium);
            return;
        }

        if (!_container.TryGetContainer(tool, Container, out var container))
            return;

        if (container.ContainedEntities.Count >= tool.Comp.MaxContents)
        {
            _popup.PopupEntity(Loc.GetString("modsuit-grabber-full"), args.User, args.User, PopupType.Medium);
            return;
        }

        tool.Comp.AudioStream = _audio.PlayPvs(tool.Comp.StartGrabSound, args.User)?.Entity;
        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, tool.Comp.GrabDelay, new ModuleGrabberDoAfterEvent(), tool, args.Target, tool)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 0.01f
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        _popup.PopupEntity(Loc.GetString("modsuit-grabber-start-grab", ("target", Name(args.Target.Value))), args.User, args.User, PopupType.Medium);

        args.Handled = true;
    }

    private void OnGetVerbs(Entity<ModularSuitGrabberToolComponent> tool, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!_container.TryGetContainer(tool, Container, out var container))
            return;

        if (container.ContainedEntities.Count == 0)
            return;

        var user = args.User;
        var ejectAllVerb = new Verb
        {
            Priority = 2,
            Text = Loc.GetString("modsuit-grabber-eject-all"),
            Category = VerbCategory.Eject,
            Act = () => EjectAllItems(user, tool, container)
        };

        args.Verbs.Add(ejectAllVerb);
    }

    private void OnGrabComplete(Entity<ModularSuitGrabberToolComponent> tool, ref ModuleGrabberDoAfterEvent args)
    {
        if (args.Handled || args.Args.Target == null)
            return;

        if (args.Cancelled)
        {
            tool.Comp.AudioStream = _audio.Stop(tool.Comp.AudioStream);
            return;
        }

        if (IsUnGrabable(args.Args.Target.Value))
            return;

        if (!_container.TryGetContainer(tool, Container, out var container))
            return;

        if (container.ContainedEntities.Count >= tool.Comp.MaxContents)
            return;

        if (_container.Insert(args.Args.Target.Value, container))
        {
            args.Handled = true;
            _popup.PopupEntity(Loc.GetString("modsuit-grabber-grabbed", ("target", Name(args.Args.Target.Value))), args.Args.User, args.Args.User, PopupType.Medium);

            if (tool.Comp.Module != null && TryComp<ModularSuitModuleComponent>(tool.Comp.Module.Value, out var moduleComp))
            {
                if (TryComp<ModularSuitCarrierComponent>(args.User, out var carrier) && !string.IsNullOrEmpty(carrier.CurrentSlot)
                    && _inventory.TryGetSlotEntity(args.User, carrier.CurrentSlot, out var suit))
                {
                    _modularSuit.UseCoreCharge(suit.Value, moduleComp.PowerInstanceUsage);
                }
            }
        }
    }

    private void OnCreated(Entity<ModularSuitGrabberToolComponent> tool, ref ModularSuitModuleItemCreatedEvent args)
    {
        tool.Comp.Module = args.Module;
        Dirty(tool.Owner, tool.Comp);
    }

    private void EjectAllItems(EntityUid user, Entity<ModularSuitGrabberToolComponent> tool, BaseContainer container)
    {
        var items = container.ContainedEntities.ToList();
        foreach (var item in items)
        {
            _container.Remove(item, container);
            _transform.DropNextTo(item, user);
        }

        _audio.PlayPvs(tool.Comp.EjectSound, user);
        _popup.PopupEntity(Loc.GetString("modsuit-grabber-ejected-all"), user, user, PopupType.Medium);

        if (tool.Comp.Module != null && TryComp<ModularSuitModuleComponent>(tool.Comp.Module.Value, out var moduleComp))
        {
            if (TryComp<ModularSuitCarrierComponent>(user, out var carrier) && !string.IsNullOrEmpty(carrier.CurrentSlot)
                && _inventory.TryGetSlotEntity(user, carrier.CurrentSlot, out var suit))
            {
                _modularSuit.UseCoreCharge(suit.Value, moduleComp.PowerInstanceUsage);
            }
        }
    }

    private void OnModuleToggled(Entity<ModularSuitGrabberModuleComponent> module, ref ModularSuitModuleToggledEvent args)
    {
        if (args.Activated)
            return;

        ClearContainer(module, args.Suit);
    }

    private void OnModuleRemoved(Entity<ModularSuitGrabberModuleComponent> module, ref ModularSuitRemovedEvent args)
    {
        ClearContainer(module, args.Suit);
    }

    private void ClearContainer(Entity<ModularSuitGrabberModuleComponent> module, EntityUid suit)
    {
        if (!TryComp<ModularSuitItemModuleComponent>(module, out var itemModule))
            return;

        EntityUid? grabber = null;
        if (itemModule.HandItem != null)
        {
            grabber = itemModule.HandItem.Value;
        }
        else
        {
            if (_container.TryGetContainer(module.Owner, itemModule.ContainerId, out var itemContainer)
                && itemContainer.ContainedEntities.Count > 0)
                grabber = itemContainer.ContainedEntities[0];
        }

        if (grabber == null || !_container.TryGetContainer(grabber.Value, Container, out var container))
            return;

        var coords = Transform(suit).Coordinates;
        if (TryComp<ModularSuitComponent>(suit, out var modular) && modular.Wearer != null)
            coords = Transform(modular.Wearer.Value).Coordinates;

        _container.EmptyContainer(container, true, coords);
    }

    private bool IsUnGrabable(EntityUid uid)
    {
        if (HasComp<MobStateComponent>(uid))
            return true;

        if (TryComp<PhysicsComponent>(uid, out var physics))
        {
            if (physics.BodyType == BodyType.Static)
                return true;
        }

        if (Transform(uid).Anchored)
            return true;

        return false;
    }
}
