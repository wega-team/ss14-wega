using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Content.Server.Emp;
using Content.Server.Flash;
using Content.Shared.Veil.Cult;
using Content.Shared.Veil.Cult.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Stunnable;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Silicons.Laws.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Map;

namespace Content.Server.Veil.Cult;

public sealed partial class VeilCultSystem
{
    [Dependency] private BloodstreamSystem _blood = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private EmpSystem _emp = default!;
    [Dependency] private EntityLookupSystem _entityLookup = default!;
    [Dependency] private FlashSystem _flash = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedStackSystem _stack = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private VisibilitySystem _visibility = default!;
    [Dependency] private SharedStationAiSystem _stationAi = default!;

    private static readonly SoundPathSpecifier CultSpell = new SoundPathSpecifier("/Audio/_Wega/Effects/cult_spell.ogg");
    private static readonly int EnergyPerOne = 100; // TODO: МБ сделать значение в компоненте рула, а не хардкодом | Не, похуй, но я бы вывел в константу :P

    private void InitializeVeilAbilities()
    {
        SubscribeLocalEvent<VeilCultistComponent, VeilCultMidasTouchGetHandEvent>(OnMidasTouch);
        SubscribeLocalEvent<MidasHandComponent, AfterInteractEvent>(OnInteractHand);
        SubscribeLocalEvent<StrangeShardComponent, AfterInteractEvent>(OnInteractShard);
        SubscribeLocalEvent<MidasHandComponent, MidasTouchDoAfterEvent>(DoAfterInteractHand);
    }

    public void OnMidasTouch(EntityUid cultist, VeilCultistComponent component, VeilCultMidasTouchGetHandEvent args)
    {
        if (TryComp<HandsComponent>(cultist, out var hands))
        {
            var spell = Spawn("VeilCultMidasTouch", Transform(cultist).Coordinates);
            var activeHand = _hands.GetActiveHand((cultist, hands));
            if (_hands.TryPickupAnyHand(cultist, spell))
                args.Handled = true;
            else if (activeHand != null && _hands.TryForcePickup((cultist, hands), spell, activeHand))
                args.Handled = true;
            else
                QueueDel(spell);
        }
    }

    private void OnInteractHand(EntityUid uid, MidasHandComponent component, AfterInteractEvent args)
    {
        if (HasComp<StackComponent>(args.Target) || HasComp<StationAiCoreComponent>(args.Target) || HasComp<SiliconLawProviderComponent>(args.Target))
        {
            var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, TimeSpan.FromSeconds(2),
            new MidasTouchDoAfterEvent(), uid, args.Target)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
                MovementThreshold = 0.01f,
                NeedHand = false
            };

            _doAfterSystem.TryStartDoAfter(doAfterEventArgs);
        }
    }

    private void DoAfterInteractHand(EntityUid uid, MidasHandComponent component, MidasTouchDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (args.Target != null)
        {
            if (TryComp<StackComponent>(args.Target, out var stack))
            {
                TransformMaterial(args.User, args.Target.Value, stack);
                QueueDel(uid);
                return;
            }

            if (TryComp<SiliconLawProviderComponent>(args.Target, out var laws))
            {
                ChangeBorgLaws(args.Target.Value, laws);
                QueueDel(uid);
                return;
            }

            if (TryComp<StationAiCoreComponent>(args.Target, out var core))
            {
                ChangeAiLaws(args.Target.Value, core);
                QueueDel(uid);
                return;
            }
        }
    }

    private void TransformMaterial(EntityUid user, EntityUid material, StackComponent stack)
    {
        if (!_prototypeManager.TryIndex(stack.StackTypeId, out var stackPrototype))
            return;

        if (stackPrototype.ID is not ("Steel" or "Plasteel" or "Brass"))
            return;

        var coords = Transform(material).Coordinates;

        if (stackPrototype.ID == "Steel")
        {
            TransformSteelToBrass(material, coords, stack.Count);
        }
        else if (stackPrototype.ID == "Plasteel")
        {
            TransformToChargedBrass(material, coords, stack.Count);
        }
        else if (stackPrototype.ID == "Brass")
        {
            TransformToChargedBrass(material, coords, stack.Count);
        }

        _audio.PlayPvs(CultSpell, user);
    }

    private void TransformSteelToBrass(EntityUid metalStack, EntityCoordinates coords, int count)
    {
        var brass = Spawn("SheetBrass1", coords);
        QueueDel(metalStack);

        if (TryComp<StackComponent>(brass, out var newStack))
            _stack.SetCount((brass, newStack), count);
    }

    private void TransformToChargedBrass(EntityUid metalStack, EntityCoordinates coords, int count)
    {
        var cult = _veilCult.GetActiveRule();
        if (cult == null)
            return;

        if (_veilCult.TryUseEnergy(count * EnergyPerOne))
        {
            var chargedBrass = Spawn("SheetChargedBrass1", coords);
            QueueDel(metalStack);

            if (TryComp<StackComponent>(chargedBrass, out var newStack))
                _stack.SetCount((chargedBrass, newStack), count);
        }
        else
            _popup.PopupEntity(Loc.GetString("veil-cult-not-enough-energy"), metalStack, PopupType.Medium);
    }

    private void ChangeBorgLaws(EntityUid uid, SiliconLawProviderComponent comp)
    {
        var ev = new SiliconVeilCultHackedEvent();
        RaiseLocalEvent(uid, ref ev);
    }

    private void ChangeAiLaws(EntityUid uid, StationAiCoreComponent core)
    {
        if (_stationAi.TryGetHeld((uid, core), out var mind))
        {
            if (mind != null)
            {
                if (HasComp<SiliconLawProviderComponent>(mind.Value))
                {
                    var ev = new SiliconVeilCultHackedEvent();
                    RaiseLocalEvent(mind.Value, ref ev);
                }
            }
        }
    }

    private void OnInteractShard(EntityUid uid, StrangeShardComponent component, AfterInteractEvent args)
    {
        if (HasComp<VeilCultistComponent>(args.Target))
        {
            var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, TimeSpan.FromSeconds(3),
            new StrangeShardDoAfterEvent(), args.Target, uid)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
                MovementThreshold = 0.01f,
                NeedHand = false
            };

            _doAfterSystem.TryStartDoAfter(doAfterEventArgs);
        }

        if (HasComp<VeilCultAltarComponent>(args.Target))
        {
            if (!_veilCult.CheckObjectives())
            {
                _popup.PopupEntity(Loc.GetString("veil-cult-objectives-not-complete"), args.User, args.User, PopupType.LargeCaution);
                return;
            }

            var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, TimeSpan.FromSeconds(6),
            new StrangeShardDoAfterEvent(), args.Target, uid)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
                MovementThreshold = 0.01f,
                NeedHand = false
            };

            _doAfterSystem.TryStartDoAfter(doAfterEventArgs);
        }
    }
}
