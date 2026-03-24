using System.Linq;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Lock;
using Content.Shared.Modular.Suit;
using Content.Shared.Popups;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Content.Shared.Wires;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Server.Modular.Suit;

public sealed partial class ModularSuitSystem : SharedModularSuitSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPointLightSystem _light = default!;
    [Dependency] private readonly LockSystem _lock = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    private const float ModuleExtractTime = 1.5f;
    private const float CoreExtractTime = 4.0f;
    private const float PartExtractTime = 5.0f;

    public override void Initialize()
    {
        base.Initialize();

        InitializePower();
        InitializeUi();

        SubscribeLocalEvent<ModularSuitComponent, ComponentInit>(OnSuitInit);
        SubscribeLocalEvent<ModularSuitComponent, EntityTerminatingEvent>(OnTerminating);
        SubscribeLocalEvent<ModularSuitComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
        SubscribeLocalEvent<ModularSuitComponent, ModularSuitExtractDoAfterEvent>(OnDoAfterComplete);

        SubscribeLocalEvent<ModularSuitPreassembledComponent, MapInitEvent>(OnPreassembledMapInit);

        SubscribeLocalEvent<ModularSuitPartComponent, GetVerbsEvent<Verb>>(OnGetPartVerbs);
        SubscribeLocalEvent<ModularSuitPartComponent, ModularSuitPartSealDoAfterEvent>(OnPartDoAfterComplete);

        SubscribeLocalEvent<ModularSuitCoreComponent, AfterInteractEvent>(OnCoreAfterInteract);
        SubscribeLocalEvent<ModularSuitModuleComponent, AfterInteractEvent>(OnModuleAfterInteract);
        SubscribeLocalEvent<ModularSuitPartComponent, AfterInteractEvent>(OnPartAfterInteract);
    }

    private void OnSuitInit(Entity<ModularSuitComponent> ent, ref ComponentInit args)
    {
        Container.EnsureContainer<ContainerSlot>(ent, CoreContainer);
        Container.EnsureContainer<Container>(ent, PartContainer);
        Container.EnsureContainer<Container>(ent, ModuleContainer);
        Container.EnsureContainer<Container>(ent, HiddenClothingContainer);
    }

    private void OnTerminating(Entity<ModularSuitComponent> ent, ref EntityTerminatingEvent args)
    {
        var coreContainer = Container.GetContainer(ent, CoreContainer);
        if (coreContainer.ContainedEntities.Count > 0)
        {
            var core = coreContainer.ContainedEntities[0];
            Container.Remove(core, coreContainer);
        }

        var moduleContainer = Container.GetContainer(ent, ModuleContainer);
        foreach (var module in moduleContainer.ContainedEntities.ToList())
        {
            if (Container.Remove(module, moduleContainer))
            {
                if (TryComp<ModularSuitModuleComponent>(module, out var moduleComp) && moduleComp.CanBeDisabled)
                    moduleComp.IsActive = false;

                var ev = new ModularSuitRemovedEvent(ent.Owner, ent.Comp.Wearer ?? ent.Owner);
                RaiseLocalEvent(module, ref ev);
            }
        }

        if (ent.Comp.Wearer != null && TryComp<ModularSuitEquippedComponent>(ent, out var equipped))
        {
            foreach (var (slot, partUid) in equipped.EquippedParts.ToList())
            {
                if (Inventory.TryGetSlotEntity(ent.Comp.Wearer.Value, slot, out var equippedItem) && equippedItem == partUid)
                {
                    if (Inventory.TryUnequip(ent.Comp.Wearer.Value, slot, out var removedItem, force: true))
                        RemComp<AttachedModularSuitPartComponent>(removedItem.Value);
                }
            }

            equipped.EquippedParts.Clear();
        }

        var partContainer = Container.GetContainer(ent, PartContainer);
        foreach (var part in partContainer.ContainedEntities.ToList())
        {
            if (Container.Remove(part, partContainer))
            {
                if (TryComp<ItemToggleComponent>(part, out var toggle) && Toggle.IsActivated((part, toggle)))
                    Toggle.TryDeactivate(part, null, false);
            }
        }

        if (TryComp<ModularSuitHiddenClothingComponent>(ent, out var hiddenComp))
        {
            var hiddenContainer = Container.GetContainer(ent, HiddenClothingContainer);
            foreach (var (slot, itemUid) in hiddenComp.HiddenItems.ToList())
            {
                if (!hiddenContainer.Contains(itemUid))
                    continue;

                if (ent.Comp.Wearer != null && Exists(ent.Comp.Wearer.Value))
                {
                    if (Container.Remove(itemUid, hiddenContainer))
                    {
                        if (!Inventory.TryGetSlotEntity(ent.Comp.Wearer.Value, slot, out var currentItem)
                            || !HasComp<ModularSuitPartComponent>(currentItem))
                        {
                            Inventory.TryEquip(ent.Comp.Wearer.Value, itemUid, slot, force: true);
                        }
                    }
                }
                else
                {
                    Container.Remove(itemUid, hiddenContainer);
                }
            }

            hiddenComp.HiddenItems.Clear();
        }
    }

    private void OnGetVerbs(Entity<ModularSuitComponent> suit, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.CanComplexInteract)
            return;

        if (!TryComp<WiresPanelComponent>(suit, out var panel) || !panel.Open)
            return;

        if (_lock.IsLocked(suit.Owner))
            return;

        var tool = _hands.GetActiveItem(args.User);
        if (tool == null || !_tool.HasQuality(tool.Value, suit.Comp.Tool))
            return;

        var user = args.User;
        var containers = new Dictionary<string, BaseContainer>
        {
            [ModuleContainer] = Container.GetContainer(suit, ModuleContainer),
            [CoreContainer] = Container.GetContainer(suit, CoreContainer),
            [PartContainer] = Container.GetContainer(suit, PartContainer)
        };

        foreach (var module in containers[ModuleContainer].ContainedEntities)
        {
            if (!TryComp<ModularSuitModuleComponent>(module, out var moduleComp))
                continue;

            if (moduleComp.IsPermanent)
                continue;

            var extractVerb = new Verb
            {
                Priority = 2,
                Text = Name(module),
                Icon = moduleComp.VerbIcon,
                Category = VerbCategory.Eject,
                Impact = LogImpact.Low,
                DoContactInteraction = true,
                Act = () => StartExtractDoAfter(suit, module, ModuleExtractTime, user, ModularSuitPart.Module)
            };
            args.Verbs.Add(extractVerb);
        }

        foreach (var core in containers[CoreContainer].ContainedEntities)
        {
            if (!TryComp<ModularSuitCoreComponent>(core, out var coreComp))
                continue;

            var extractVerb = new Verb
            {
                Priority = 1,
                Text = Name(core),
                Icon = coreComp.VerbIcon,
                Category = VerbCategory.Eject,
                Impact = LogImpact.Medium,
                DoContactInteraction = true,
                Act = () => StartExtractDoAfter(suit, core, CoreExtractTime, user, ModularSuitPart.Core)
            };
            args.Verbs.Add(extractVerb);
        }

        foreach (var part in containers[PartContainer].ContainedEntities)
        {
            if (suit.Comp.Wearer != null || !TryComp<ModularSuitPartComponent>(part, out var partComp))
                continue;

            var extractVerb = new Verb
            {
                Priority = 0,
                Text = Name(part),
                Icon = partComp.VerbIcon,
                Category = VerbCategory.Eject,
                Impact = LogImpact.Low,
                DoContactInteraction = true,
                Act = () => StartExtractDoAfter(suit, part, PartExtractTime, user, ModularSuitPart.Part)
            };
            args.Verbs.Add(extractVerb);
        }
    }

    private void StartExtractDoAfter(EntityUid suit, EntityUid target, float delay, EntityUid user, ModularSuitPart type)
    {
        var doAfterArgs = new DoAfterArgs(EntityManager, user, delay,
            new ModularSuitExtractDoAfterEvent(type), suit, suit, target)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 0.01f
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        Popup.PopupEntity(Loc.GetString("modsuit-extract-start", ("item", target)), user, user, PopupType.Medium);
    }

    private void OnDoAfterComplete(Entity<ModularSuitComponent> suit, ref ModularSuitExtractDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Used == null)
            return;

        var container = Container.GetContainer(suit, GetContainerForType(args.Type));
        if (!container.ContainedEntities.Contains(args.Used.Value))
            return;

        switch (args.Type)
        {
            case ModularSuitPart.Module:
                if (TryComp<ModularSuitModuleComponent>(args.Used.Value, out var module))
                {
                    if (module.CanBeDisabled) module.IsActive = false;
                    var ev = new ModularSuitRemovedEvent(suit, args.User);
                    RaiseLocalEvent(args.Used.Value, ref ev);
                }
                break;
            case ModularSuitPart.Part:
                RemoveDependentModules(suit, args.Used.Value);
                if (HasComp<PointLightComponent>(args.Used.Value))
                    _light.SetEnabled(args.Used.Value, false);
                break;
        }

        if (Container.Remove(args.Used.Value, container))
        {
            Popup.PopupEntity(Loc.GetString("modsuit-extract-success", ("item", Name(args.Used.Value))), suit, args.User);
            _hands.TryPickupAnyHand(args.User, args.Used.Value);

            Dirty(suit.Owner, suit.Comp);
            CheckSuitAssembly(suit);
            args.Handled = true;

            _audio.PlayPredicted(suit.Comp.EjectSound, suit.Owner, null);

            var tool = _hands.GetActiveItem(args.User);
            if (TryComp<ToolComponent>(tool, out var toolComp))
                _tool.PlayToolSound(tool.Value, toolComp, null);

            UpdateUiState(suit);
        }
    }

    private string GetContainerForType(ModularSuitPart type)
    {
        return type switch
        {
            ModularSuitPart.Module => ModuleContainer,
            ModularSuitPart.Core => CoreContainer,
            ModularSuitPart.Part => PartContainer,
            _ => throw new ArgumentException($"Unknown type: {type}")
        };
    }

    private void OnPreassembledMapInit(Entity<ModularSuitPreassembledComponent> suit, ref MapInitEvent args)
    {
        if (!TryComp<ModularSuitComponent>(suit, out var modularSuit))
            return;

        var moduleContainer = Container.GetContainer(suit, ModuleContainer);
        foreach (var moduleProto in suit.Comp.Modules)
        {
            var moduleUid = Spawn(moduleProto, Transform(suit).Coordinates);
            if (!TryComp<ModularSuitModuleComponent>(moduleUid, out var moduleComp))
            {
                Del(moduleUid);
                continue;
            }

            if (moduleComp.Tags.Count > 0)
            {
                var hasConflict = false;
                var existingModules = GetCurrentModules((suit.Owner, modularSuit));

                foreach (var existing in existingModules)
                {
                    if (TryComp<ModularSuitModuleComponent>(existing, out var existingComp)
                        && existingComp.Tags.Intersect(moduleComp.Tags).Any())
                    {
                        hasConflict = true;
                        break;
                    }
                }

                if (hasConflict)
                {
                    Del(moduleUid);
                    continue;
                }
            }

            if (Container.Insert(moduleUid, moduleContainer))
            {
                var installEvent = new ModularSuitInstalledEvent(suit.Owner, null);
                RaiseLocalEvent(moduleUid, ref installEvent);

                Dirty(moduleUid, moduleComp);
            }
            else
            {
                Del(moduleUid);
            }
        }
    }

    private void OnGetPartVerbs(Entity<ModularSuitPartComponent> part, ref GetVerbsEvent<Verb> args)
    {
        if (!TryComp<AttachedModularSuitPartComponent>(part, out var attached))
            return;

        if (!TryComp<ModularSuitComponent>(attached.Suit, out var suit) || suit.Active)
            return;

        if (!args.CanAccess || !args.CanInteract || !args.CanComplexInteract)
            return;

        if (!TryComp<ItemToggleComponent>(part, out var itemToggle))
            return;

        var user = args.User;
        var activated = Toggle.IsActivated((part, itemToggle));
        var toggleVerb = new Verb
        {
            Priority = 2,
            Text = activated ? Loc.GetString(itemToggle.VerbToggleOff) : Loc.GetString(itemToggle.VerbToggleOn),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/Spare/poweronoff.svg.192dpi.png")),
            Impact = LogImpact.Low,
            DoContactInteraction = true,
            Act = () => StartPartToggleDoAfter(user, part, !activated)
        };

        args.Verbs.Add(toggleVerb);
    }

    private void StartPartToggleDoAfter(EntityUid user, Entity<ModularSuitPartComponent> part, bool activate)
    {
        var delay = part.Comp.ToggleDelay;
        if (TryComp<AffectedModuleSpringlockComponent>(user, out var springlock))
            delay /= springlock.SpeedMultiplier;

        var doAfterArgs = new DoAfterArgs(EntityManager, user, delay,
            new ModularSuitPartSealDoAfterEvent(activate), part, part)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 0.01f
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnPartDoAfterComplete(Entity<ModularSuitPartComponent> part, ref ModularSuitPartSealDoAfterEvent args)
    {
        if (!TryComp<AttachedModularSuitPartComponent>(part, out var attached)
            || args.Handled || args.Cancelled || attached.Suit == null)
            return;

        if (args.Activate)
        {
            args.Handled = Toggle.TryActivate(part.Owner, args.User, false);
        }
        else
        {
            args.Handled = Toggle.TryDeactivate(part.Owner, args.User, false);
        }

        CheckSuitAssembly(attached.Suit.Value);
        if (args.Handled && args.Activate)
        {
            if (!TryComp<ModularSuitEquippedComponent>(attached.Suit.Value, out var equipped))
                return;

            foreach (var (_, partUid) in equipped.EquippedParts)
            {
                if (partUid == part.Owner)
                    continue;

                if (!TryComp<ItemToggleComponent>(partUid, out var toggle))
                    continue;

                if (!Toggle.IsActivated((partUid, toggle)))
                {
                    var delay = part.Comp.ToggleDelay;
                    if (TryComp<AffectedModuleSpringlockComponent>(args.User, out var springlock))
                        delay /= springlock.SpeedMultiplier;

                    var doAfterArgs = new DoAfterArgs(EntityManager, args.User, delay,
                        new ModularSuitPartSealDoAfterEvent(true), partUid, partUid)
                    {
                        BreakOnMove = true,
                        BreakOnDamage = true,
                        MovementThreshold = 0.01f
                    };

                    _doAfter.TryStartDoAfter(doAfterArgs);
                    Popup.PopupEntity(Loc.GetString("modsuit-continue-sealing"), args.User, args.User);
                    break;
                }
            }
        }
    }

    private void OnCoreAfterInteract(Entity<ModularSuitCoreComponent> core, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        if (!TryComp<ModularSuitComponent>(args.Target, out var suit))
            return;

        if (_lock.IsLocked(args.Target.Value) || !TryComp<WiresPanelComponent>(args.Target, out var panel) || !panel.Open)
        {
            Popup.PopupEntity(Loc.GetString("modsuit-panel-closed"), args.Target.Value, args.User);
            return;
        }

        var container = Container.GetContainer(args.Target.Value, CoreContainer);
        if (container.ContainedEntities.Count > 0)
        {
            Popup.PopupEntity(Loc.GetString("modsuit-core-slot-occupied"), args.Target.Value, args.User);
            return;
        }

        if (Container.Insert(core.Owner, container))
        {
            Popup.PopupEntity(Loc.GetString("modsuit-core-installed"), args.Target.Value, args.User);

            args.Handled = true;
            CheckSuitAssembly(args.Target.Value);
            _audio.PlayPredicted(suit.InsertSound, args.Target.Value, null);

            UpdateUiState((args.Target.Value, suit));
        }
    }

    private void OnPartAfterInteract(Entity<ModularSuitPartComponent> part, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        if (!TryComp<ModularSuitComponent>(args.Target, out var suit) || suit.Wearer != null)
            return;

        if (_lock.IsLocked(args.Target.Value) || !TryComp<WiresPanelComponent>(args.Target, out var panel) || !panel.Open)
        {
            Popup.PopupEntity(Loc.GetString("modsuit-panel-closed"), args.Target.Value, args.User);
            return;
        }

        var container = Container.GetContainer(args.Target.Value, PartContainer);

        foreach (var existing in container.ContainedEntities)
        {
            if (TryComp<ModularSuitPartComponent>(existing, out var existingPart) &&
                existingPart.PartType == part.Comp.PartType)
            {
                Popup.PopupEntity(Loc.GetString("modsuit-part-already-installed"), args.Target.Value, args.User);
                return;
            }
        }

        if (Container.Insert(part.Owner, container))
        {
            Popup.PopupEntity(Loc.GetString("modsuit-part-installed", ("part", part.Comp.PartType.ToString())),
                args.Target.Value, args.User);

            args.Handled = true;
            CheckSuitAssembly(args.Target.Value);

            _audio.PlayPredicted(suit.InsertSound, args.Target.Value, null);
            Toggle.TryDeactivate(part.Owner, args.User, false);

            UpdateUiState((args.Target.Value, suit));
        }
    }

    private void OnModuleAfterInteract(Entity<ModularSuitModuleComponent> module, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        if (!TryComp<ModularSuitComponent>(args.Target, out var suit))
            return;

        if (_lock.IsLocked(args.Target.Value) || !TryComp<WiresPanelComponent>(args.Target, out var panel) || !panel.Open)
        {
            Popup.PopupEntity(Loc.GetString("modsuit-panel-closed"), args.Target.Value, args.User);
            return;
        }

        if (module.Comp.ModulePart != null)
        {
            if (!HasPartInstalled(args.Target.Value, module.Comp.ModulePart.Value))
            {
                var partName = Loc.GetString($"modsuit-part-{module.Comp.ModulePart.Value.ToString().ToLower()}");
                Popup.PopupEntity(Loc.GetString("modsuit-module-requires-part", ("part", partName)), args.Target.Value, args.User);
                return;
            }
        }

        args.Handled = TryInstallModule((args.Target.Value, suit), module, args.User);
    }

    private bool TryInstallModule(Entity<ModularSuitComponent> suit, Entity<ModularSuitModuleComponent> module, EntityUid user)
    {
        var container = Container.GetContainer(suit, ModuleContainer);
        if (module.Comp.IsPermanent && container.Contains(module.Owner))
        {
            Popup.PopupEntity(Loc.GetString("modsuit-module-permanent"), suit, user);
            return false;
        }

        if (suit.Comp.BlacklistModules != null && _whitelist.IsWhitelistPass(suit.Comp.BlacklistModules, module.Owner))
        {
            Popup.PopupEntity(Loc.GetString("modsuit-module-blacklist-conflict"), suit, user);
            return false;
        }

        if (module.Comp.Tags.Count > 0)
        {
            var moduleProto = Prototype(module);
            var existingModules = GetCurrentModules(suit);
            foreach (var existing in existingModules)
            {
                if (!TryComp<ModularSuitModuleComponent>(existing, out var existingComp))
                    continue;

                if (existingComp.Tags.Intersect(module.Comp.Tags).Any())
                {
                    Popup.PopupEntity(Loc.GetString("modsuit-module-tag-conflict"), suit, user);
                    return false;
                }

                if (moduleProto == Prototype(existing))
                {
                    Popup.PopupEntity(Loc.GetString("modsuit-module-proto-conflict"), suit, user);
                    return false;
                }
            }
        }

        if (!Container.Insert(module.Owner, container))
        {
            Popup.PopupEntity(Loc.GetString("modsuit-install-failed"), suit, user);
            return false;
        }

        Popup.PopupEntity(Loc.GetString("modsuit-module-installed"), suit, user);
        _audio.PlayPredicted(suit.Comp.InsertSound, suit.Owner, null);

        var ev = new ModularSuitInstalledEvent(suit, user);
        RaiseLocalEvent(module.Owner, ref ev);

        Dirty(suit.Owner, suit.Comp);
        UpdateUiState(suit);

        return true;
    }

    private List<EntityUid> GetCurrentModules(Entity<ModularSuitComponent> suit)
    {
        if (!Container.TryGetContainer(suit, ModuleContainer, out var container))
            return new List<EntityUid>();

        return container.ContainedEntities.ToList();
    }

    private bool HasPartInstalled(EntityUid suit, SuitPartType partType)
    {
        if (TryComp<ModularSuitEquippedComponent>(suit, out var equipped))
        {
            foreach (var (_, partUid) in equipped.EquippedParts)
            {
                if (TryComp<ModularSuitPartComponent>(partUid, out var partComp) && partComp.PartType == partType)
                    return true;
            }
        }

        var partContainer = Container.GetContainer(suit, PartContainer);
        foreach (var part in partContainer.ContainedEntities)
        {
            if (TryComp<ModularSuitPartComponent>(part, out var partComp) && partComp.PartType == partType)
                return true;
        }

        return false;
    }

    private void RemoveDependentModules(Entity<ModularSuitComponent> suit, EntityUid removedPart)
    {
        if (!TryComp<ModularSuitPartComponent>(removedPart, out var partComp))
            return;

        var moduleContainer = Container.GetContainer(suit, ModuleContainer);
        var modulesToRemove = new List<EntityUid>();

        foreach (var module in moduleContainer.ContainedEntities)
        {
            if (TryComp<ModularSuitModuleComponent>(module, out var moduleComp) && moduleComp.ModulePart == partComp.PartType)
                modulesToRemove.Add(module);
        }

        foreach (var module in modulesToRemove)
        {
            if (Container.Remove(module, moduleContainer))
            {
                var ev = new ModularSuitRemovedEvent(suit, suit.Comp.Wearer ?? suit.Owner);
                RaiseLocalEvent(module, ref ev);
            }
        }
    }

    private void CheckSuitAssembly(EntityUid uid)
    {
        if (!TryComp<ModularSuitComponent>(uid, out var suit))
            return;

        var coreContainer = Container.GetContainer(uid, CoreContainer);
        if (coreContainer.ContainedEntities.Count == 0)
        {
            suit.Assembled = false;
            UpdateUiState((uid, suit));
            return;
        }

        if (!TryComp<ModularSuitEquippedComponent>(uid, out var equipped))
        {
            suit.Assembled = false;
            UpdateUiState((uid, suit));
            return;
        }

        var allPartsSealed = true;
        foreach (var (_, partUid) in equipped.EquippedParts)
        {
            if (!TryComp<ItemToggleComponent>(partUid, out var toggle) || !Toggle.IsActivated((partUid, toggle)))
            {
                allPartsSealed = false;
                break;
            }
        }

        suit.Assembled = allPartsSealed;

        Dirty(uid, suit);
        UpdateUiState((uid, suit));
    }
}
