using Content.Server._Wega.AutoDust;
using Content.Server.Ninja.Components;
using Robust.Shared.Timing;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Shared._Wega.NinjaVisor;
using Content.Shared.Actions;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Throwing;
using Content.Shared.Humanoid;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Ninja;
using Content.Shared.Ninja.Components;
using Content.Shared.Shuttles.Components;
using static Content.Shared.Shuttles.Components.SharedShuttleConsoleComponent;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Server.Ninja.Systems;

public sealed partial class SpiderOSSystem : EntitySystem
{
    [Dependency] private InventorySystem      _inventory      = default!;
    [Dependency] private UserInterfaceSystem  _ui             = default!;
    [Dependency] private ClothingSystem       _clothing       = default!;
    [Dependency] private AutoDustSystem       _autoDust       = default!;
    [Dependency] private AppearanceSystem     _appearance     = default!;
    [Dependency] private SharedItemSystem     _item           = default!;
    [Dependency] private SharedHandsSystem    _hands          = default!;
    [Dependency] private NinjaSuitSystem      _ninjaSuit      = default!;
    [Dependency] private ShuttleConsoleSystem _consoleSystem  = default!;
    [Dependency] private SharedTransformSystem _transform     = default!;

    private float _coordTimer;

    private static readonly (string Slot, string Name)[] NinjaSlots =
    [
        ("outerClothing", "Ninja Suit"),
        ("mask",          "Ninja Mask"),
        ("head",          "Ninja Helmet"),
        ("gloves",        "Ninja Gloves"),
        ("shoes",         "Ninja Boots"),
    ];

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpiderOSComponent, BoundUIOpenedEvent>(OnOpened);
        SubscribeLocalEvent<SpiderOSComponent, BoundUIClosedEvent>(OnClosed);
        SubscribeLocalEvent<SpiderOSComponent, SpiderOSSetStyleMessage>(OnSetStyle);
        SubscribeLocalEvent<SpiderOSComponent, SpiderOSSetAbilityMessage>(OnSetAbility);
        SubscribeLocalEvent<SpiderOSComponent, SpiderOSActivateMessage>(OnActivate);
        SubscribeLocalEvent<SpiderOSComponent, SpiderOSOpenShuttleConsoleMessage>(OnOpenShuttleConsole);

        SubscribeLocalEvent<SpiderOSSessionComponent, IsUnequippingTargetAttemptEvent>(OnUnequipAttempt);
        SubscribeLocalEvent<SpiderOSSessionComponent, DropAttemptEvent>(OnDropAttempt);
        SubscribeLocalEvent<SpiderOSSessionComponent, ThrowAttemptEvent>(OnThrowAttempt);
    }

    private void OnUnequipAttempt(EntityUid uid, SpiderOSSessionComponent _, IsUnequippingTargetAttemptEvent args)
    {
        args.Cancel();
    }

    // While SpiderOS is open, the ninja can't drop the katana out of their hands.
    private void OnDropAttempt(EntityUid uid, SpiderOSSessionComponent _, DropAttemptEvent args)
    {
        if (HasHeldKatana(uid))
            args.Cancel();
    }

    // While SpiderOS is open, the ninja can't throw the katana out of their hands.
    private void OnThrowAttempt(EntityUid uid, SpiderOSSessionComponent _, ThrowAttemptEvent args)
    {
        if (HasComp<EnergyKatanaComponent>(args.ItemUid))
            args.Cancel();
    }

    private bool HasHeldKatana(EntityUid user)
    {
        foreach (var held in _hands.EnumerateHeld(user))
        {
            if (HasComp<EnergyKatanaComponent>(held))
                return true;
        }

        return false;
    }

    private void OnOpened(EntityUid uid, SpiderOSComponent comp, BoundUIOpenedEvent args)
    {
        var user = args.Actor;

        if (TryComp<ClothingComponent>(uid, out _))
        {
            if (!_inventory.TryGetSlotEntity(user, "outerClothing", out var worn) || worn != uid)
            {
                _ui.CloseUi(uid, SpiderOSUiKey.Key, user);
                return;
            }

            // Suit is worn — dust the impostor
            if (!HasComp<SpaceNinjaComponent>(user))
            {
                _ui.CloseUi(uid, SpiderOSUiKey.Key, user);
                _autoDust.DustEntity(user);
                return;
            }
        }

        foreach (var (slot, _) in NinjaSlots)
        {
            if (!_inventory.TryGetSlotEntity(user, slot, out _))
            {
                _ui.CloseUi(uid, SpiderOSUiKey.Key, user);
                return;
            }
        }

        if (!HasKatana(user))
        {
            _ui.CloseUi(uid, SpiderOSUiKey.Key, user);
            return;
        }

        if (!comp.IsActivated)
        {
            var isFemale = TryComp<HumanoidProfileComponent>(user, out var profile)
                           && profile.Sex == Sex.Female;
            comp.SuitGender = isFemale ? 1 : 0;
        }

        LinkShuttle(comp, user);
        ApplyStyles(user, comp);
        SendState(uid, comp, user: user);

        EnsureComp<SpiderOSSessionComponent>(user);
    }

    private void OnClosed(EntityUid uid, SpiderOSComponent comp, BoundUIClosedEvent args)
    {
        if (comp.IsInitializing)
        {
            _ui.OpenUi(uid, SpiderOSUiKey.Key, args.Actor);
            return;
        }

        RemComp<SpiderOSSessionComponent>(args.Actor);
    }

    private bool HasKatana(EntityUid user)
    {
        if (_inventory.TryGetSlotEntity(user, "belt", out var belt) && HasComp<EnergyKatanaComponent>(belt.Value))
            return true;

        foreach (var held in _hands.EnumerateHeld(user))
        {
            if (HasComp<EnergyKatanaComponent>(held))
                return true;
        }

        return false;
    }

    private void OnSetAbility(EntityUid uid, SpiderOSComponent comp, SpiderOSSetAbilityMessage msg)
    {
        if (comp.IsActivated) return;
        if (msg.Row < 0 || msg.Row >= comp.AbilityChoices.Length) return;
        comp.AbilityChoices[msg.Row] = Math.Clamp(msg.Choice, 0, 2);
        Dirty(uid, comp);
        SendState(uid, comp, user: msg.Actor);
    }

    private void OnSetStyle(EntityUid uid, SpiderOSComponent comp, SpiderOSSetStyleMessage msg)
    {
        switch (msg.Slot)
        {
            case 0: comp.SuitGender       = Math.Clamp(msg.Index, 0, SpiderOSComponent.SuitGenders.Length - 1);       break;
            case 1: comp.SuitStyleVariant = Math.Clamp(msg.Index, 0, SpiderOSComponent.SuitStyleVariants.Length - 1); break;
            case 2: comp.SuitColor        = Math.Clamp(msg.Index, 0, SpiderOSComponent.SuitColors.Length - 1);        break;
        }

        ApplyStyles(msg.Actor, comp);
        Dirty(uid, comp);
        SendState(uid, comp, user: msg.Actor);
    }

    // Delay shown AFTER each message before the next phase starts.
    // Total ≈ 60 seconds across 14 phases.
    private static readonly int[] InitPhaseDelays =
    [
        4000,  // 0  Инициализация...
        7000,  // 1  Установка связи с нейронами...
        6000,  // 2  Расширение нейронной связи...
        6000,  // 3  Установка наблюдения...
        5000,  // 4  Активация механизма блокировки...
        3000,  // 5  Блокировка костюма...   (lock gear)
        2000,  // 6  Успех.
        4000,  // 7  Персонализация костюма... (apply styles)
        2000,  // 8  Успех.
        8000,  // 9  Инициализация модулей... (grant abilities)
        2000,  // 10 Успех.
        4000,  // 11 Статус основных систем... ONLINE
        4000,  // 12 Статус резервных систем... ONLINE
        3000,  // 13 Все системы в норме.
    ];

    private static readonly string[] InitMessages =
    [
        "Инициализация...",
        "Установка связи с нейронами пользователя...\tУспех.",
        "Расширение нейронной связи...\tУспех.",
        "Установка наблюдения за мозговой активностью...\tУспех.",
        "Активация механизма внешней блокировки костюма...\tУспех.",
        "Блокировка костюма...",
        "Успех.",
        "Персонализация костюма...",
        "Успех.",
        "Инициализация модулей...",
        "Успех.",
        "Статус основных систем...\tONLINE",
        "Статус резервных систем...\tONLINE",
        "Все системы в норме. Добро пожаловать в SpiderOS.",
    ];

    private void OnActivate(EntityUid uid, SpiderOSComponent comp, SpiderOSActivateMessage msg)
    {
        var user = msg.Actor;
        if (!Exists(user))
            return;

        if (comp.IsActivated || comp.IsInitializing)
            return;

        if (!HasComp<SpaceNinjaComponent>(user))
        {
            _autoDust.DustEntity(user);
            return;
        }

        var errors = new List<string>();
        foreach (var (slot, name) in NinjaSlots)
        {
            if (!_inventory.TryGetSlotEntity(user, slot, out _))
                errors.Add($"[ERROR] {name} not found in inventory");
        }

        if (errors.Count > 0)
        {
            SendState(uid, comp, errors.ToArray(), user: user);
            return;
        }

        comp.IsInitializing    = true;
        comp.TerminalFinished  = false;
        comp.CurrentTerminalLine = null;
        SendState(uid, comp, user: user);

        RunInitPhase(uid, comp, user, 0);
    }

    private void RunInitPhase(EntityUid suitUid, SpiderOSComponent comp, EntityUid user, int phase)
    {
        if (TerminatingOrDeleted(suitUid) || TerminatingOrDeleted(user))
            return;

        comp.CurrentTerminalLine = InitMessages[phase];

        switch (phase)
        {
            case 5:
                foreach (var (slot, _) in NinjaSlots)
                {
                    if (_inventory.TryGetSlotEntity(user, slot, out var item))
                        EnsureComp<UnremoveableComponent>(item.Value);
                }
                break;
            case 7:
                ApplyStyles(user, comp);
                break;
            case 9:
                _ninjaSuit.GrantChosenAbilities(suitUid, user, comp.AbilityChoices);
                comp.IsActivated = true;
                Dirty(suitUid, comp);
                break;
        }

        SendState(suitUid, comp, user: user);

        var nextPhase = phase + 1;
        var delay = phase < InitPhaseDelays.Length ? InitPhaseDelays[phase] : 2000;
        if (nextPhase < InitMessages.Length)
        {
            Timer.Spawn(delay, () => RunInitPhase(suitUid, comp, user, nextPhase));
        }
        else
        {
            Timer.Spawn(delay, () =>
            {
                if (TerminatingOrDeleted(suitUid) || TerminatingOrDeleted(user))
                    return;
                comp.IsInitializing  = false;
                comp.TerminalFinished = true;
                SendState(suitUid, comp, user: user);
            });
        }
    }

    public void ApplyStyles(EntityUid user, SpiderOSComponent comp)
    {
        if (_inventory.TryGetSlotEntity(user, "outerClothing", out var suit))
        {
            _clothing.SetEquippedState(suit.Value,
                SpiderOSComponent.GetSuitEquippedState(comp.SuitGender, comp.SuitStyleVariant, comp.SuitColor));
            _appearance.SetData(suit.Value, NinjaColorVisuals.SuitVariant,
                SpiderOSComponent.GetSuitVariant(comp.SuitGender, comp.SuitStyleVariant, comp.SuitColor));
        }

        if (_inventory.TryGetSlotEntity(user, "mask", out var mask))
        {
            _clothing.SetEquippedState(mask.Value,
                SpiderOSComponent.GetMaskEquippedState(comp.SuitStyleVariant, comp.SuitColor));
            _appearance.SetData(mask.Value, NinjaColorVisuals.MaskVariant,
                SpiderOSComponent.GetMaskVariant(comp.SuitStyleVariant, comp.SuitColor));
        }

        if (_inventory.TryGetSlotEntity(user, "gloves", out var gloves))
        {
            _clothing.SetEquippedState(gloves.Value,
                SpiderOSComponent.GetGlovesEquippedState(comp.SuitStyleVariant, comp.SuitColor));
            _appearance.SetData(gloves.Value, NinjaColorVisuals.GlovesVariant,
                SpiderOSComponent.GetGlovesVariant(comp.SuitStyleVariant, comp.SuitColor));
        }

        if (_inventory.TryGetSlotEntity(user, "head", out var helmet))
        {
            _clothing.SetEquippedState(helmet.Value,
                SpiderOSComponent.GetHelmetEquippedState(comp.SuitStyleVariant));
            _appearance.SetData(helmet.Value, NinjaColorVisuals.HelmetVariant,
                SpiderOSComponent.GetHelmetVariant(comp.SuitStyleVariant));
        }

        var colorName = comp.SuitColor switch { 0 => "red", 1 => "blue", _ => "green" };
        EntityUid? katanaEnt = null;
        foreach (var slot in new[] { "belt", "back" })
        {
            if (_inventory.TryGetSlotEntity(user, slot, out var k) && HasComp<EnergyKatanaComponent>(k.Value))
            {
                katanaEnt = k.Value;
                break;
            }
        }
        if (katanaEnt == null)
        {
            foreach (var held in _hands.EnumerateHeld(user))
            {
                if (HasComp<EnergyKatanaComponent>(held))
                {
                    katanaEnt = held;
                    break;
                }
            }
        }
        if (katanaEnt != null)
        {
            _clothing.SetEquippedState(katanaEnt.Value, SpiderOSComponent.GetKatanaEquippedBeltState(comp.SuitColor));
            _appearance.SetData(katanaEnt.Value, NinjaColorVisuals.KatanaColor, comp.SuitColor);
            _item.SetHeldPrefix(katanaEnt.Value, colorName);
        }

        if (_inventory.TryGetSlotEntity(user, "ears", out var headset)
            && HasComp<NinjaHeadsetComponent>(headset.Value))
        {
            _appearance.SetData(headset.Value, NinjaColorVisuals.HeadsetVariant,
                SpiderOSComponent.GetHeadsetVariant(comp.SuitStyleVariant, comp.SuitColor));
        }

        if (_inventory.TryGetSlotEntity(user, "eyes", out var visor)
            && TryComp<NinjaVisorComponent>(visor.Value, out var visorComp))
        {
            visorComp.NightVisionColor = comp.SuitColor switch
            {
                0 => Color.FromHex("#FF2200"),
                1 => Color.FromHex("#0066FF"),
                _ => Color.FromHex("#00FF33"),
            };
            Dirty(visor.Value, visorComp);
        }
    }

    /// <summary>
    /// Links the shuttle and facility on the same map as the user.
    /// Only searches by map when not yet linked — preserves the link after the shuttle flies away.
    /// </summary>
    private void LinkShuttle(SpiderOSComponent comp, EntityUid user)
    {
        // Keep existing valid link so the shuttle can be found after it leaves the planet.
        if (comp.LinkedShuttle != null && !TerminatingOrDeleted(comp.LinkedShuttle.Value))
            return;

        var userMap = Transform(user).MapID;

        var shutQ = EntityQueryEnumerator<NinjaShuttleComponent>();
        while (shutQ.MoveNext(out var uid, out _))
        {
            if (Transform(uid).MapID != userMap)
                continue;
            comp.LinkedShuttle = uid;
            break;
        }

        var facQ = EntityQueryEnumerator<NinjaFacilityComponent>();
        while (facQ.MoveNext(out var uid, out _))
        {
            if (Transform(uid).MapID != userMap)
                continue;
            comp.LinkedFacility = uid;
            break;
        }
    }

    private void OnOpenShuttleConsole(EntityUid uid, SpiderOSComponent comp, SpiderOSOpenShuttleConsoleMessage msg)
    {
        if (comp.LinkedShuttle == null || TerminatingOrDeleted(comp.LinkedShuttle.Value))
            return;

        var shuttleUid = comp.LinkedShuttle.Value;
        var consoleQ   = EntityQueryEnumerator<ShuttleConsoleComponent>();
        while (consoleQ.MoveNext(out var consoleUid, out var consoleComp))
        {
            if (Transform(consoleUid).GridUid != shuttleUid)
                continue;

            var session = EnsureComp<NinjaShuttleConsoleSessionComponent>(msg.Actor);
            session.ConsoleUid = consoleUid;

            // requireInputValidation=false and interactionRange=-1 are set in the ninja planet map YAML,
            // so TryOpenUi bypasses all validation and the update-loop map-distance check.
            if (_ui.TryOpenUi(consoleUid, ShuttleConsoleUiKey.Key, msg.Actor))
            {
                EnsureComp<PilotComponent>(msg.Actor);
                _consoleSystem.AddPilot(consoleUid, msg.Actor, consoleComp);
            }
            break;
        }
    }

    public override void Update(float frameTime)
    {
        var consoleQuery = EntityQueryEnumerator<NinjaShuttleConsoleSessionComponent>();
        while (consoleQuery.MoveNext(out var actorUid, out var session))
        {
            if (_ui.IsUiOpen(session.ConsoleUid, ShuttleConsoleUiKey.Key, actorUid))
                continue;

            RemComp<NinjaShuttleConsoleSessionComponent>(actorUid);
            _consoleSystem.RemovePilot(actorUid);
        }

        _coordTimer += frameTime;
        if (_coordTimer < 2f)
            return;
        _coordTimer = 0f;

        var sessQuery = EntityQueryEnumerator<SpiderOSSessionComponent>();
        while (sessQuery.MoveNext(out var userUid, out _))
        {
            if (!_inventory.TryGetSlotEntity(userUid, "outerClothing", out var suitEnt))
                continue;
            if (!TryComp<SpiderOSComponent>(suitEnt.Value, out var spiderComp))
                continue;
            SendState(suitEnt.Value, spiderComp, user: userUid);
        }
    }

    private void SendState(EntityUid uid, SpiderOSComponent comp, string[]? errors = null, EntityUid? user = null)
    {
        int coordX = 0, coordY = 0;
        var inNullspace = true;
        if (user.HasValue)
        {
            var pos = _transform.GetMapCoordinates(user.Value);
            if (pos.MapId != MapId.Nullspace)
            {
                coordX      = (int)pos.Position.X;
                coordY      = (int)pos.Position.Y;
                inNullspace = false;
            }
        }

        _ui.SetUiState(uid, SpiderOSUiKey.Key,
            new SpiderOSBoundUserInterfaceState(
                comp.SuitGender, comp.SuitStyleVariant, comp.SuitColor,
                comp.IsActivated, comp.AbilityChoices, errors,
                shuttleLinked: comp.LinkedShuttle != null && !TerminatingOrDeleted(comp.LinkedShuttle.Value),
                coordX: coordX, coordY: coordY, inNullspace: inNullspace,
                isLoading: comp.IsInitializing, terminalLine: comp.CurrentTerminalLine, terminalFinished: comp.TerminalFinished));
    }
}
