using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Veil.Cult.Components;
using Content.Shared.Veil.Cult.UI;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.Movement.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Player;
using Robust.Shared.Spawners;

namespace Content.Shared.Veil.Cult;

public abstract partial class SharedVeilCultSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _action = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private MovementSpeedModifierSystem _speed = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VeilCultistComponent, VeilCultMidasTouchActionEvent>(OnMidasTouch);
        SubscribeLocalEvent<EnchantableComponent, EnchantSelectedMessage>(OnEnchantSelected);
        SubscribeLocalEvent<CogscarabComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<ConfusionComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ConfusionComponent, ComponentRemove>(OnShutdown);
        SubscribeLocalEvent<ConfusionComponent, RefreshMovementSpeedModifiersEvent>(Invert);
    }

    #region Deconvertation
    public void CultistDeconvertation(EntityUid cultist)
    {
        if (!TryComp<VeilCultistComponent>(cultist, out var veilCultist))
            return;

        if (TryComp<ActionsContainerComponent>(cultist, out var actionsContainer))
        {
            foreach (var actionId in actionsContainer.Container.ContainedEntities.ToArray())
            {
                if (!TryComp(actionId, out MetaDataComponent? meta))
                    continue;

                var protoId = meta.EntityPrototype?.ID;
                if (protoId == VeilCultistComponent.MidasTouch.Id)
                    _action.RemoveAction(cultist, actionId);
            }
        }

        if (TryComp<MindLinkComponent>(cultist, out var mindLink))
        {
            mindLink.Channels.Remove(veilCultist.CultMindChannel);
            if (mindLink.Channels.Count == 0)
                RemComp(cultist, mindLink);
        }

        _stun.TryKnockdown(cultist, TimeSpan.FromSeconds(4), true);
        _popup.PopupEntity(Loc.GetString("veil-cult-break-control", ("name", Identity.Entity(cultist, EntityManager))), cultist);

        RemComp<VeilCultistComponent>(cultist);
        RemComp<VeilCultistHandsComponent>(cultist);
        RemComp<VeilCogDisplayComponent>(cultist);
    }
    #endregion

    private void OnMidasTouch(EntityUid cultist, VeilCultistComponent component, VeilCultMidasTouchActionEvent args)
    {
        if (_hands.TryGetActiveItem(cultist, out var hand) && hand != null)
        {
            var uid = hand.Value;
            if (TryComp<EnchantableComponent>(uid, out var enchant) && !HasComp<EnchantedComponent>(uid))
            {
                TryEnchant(uid, enchant, args);
            }
            else
            {
                var ev = new VeilCultMidasTouchGetHandEvent();
                RaiseLocalEvent(cultist, ev);
            }
        }
        else
        {
            var ev = new VeilCultMidasTouchGetHandEvent();
            RaiseLocalEvent(cultist, ev);
        }

        args.Handled = true;
    }

    private void TryEnchant(EntityUid uid, EnchantableComponent component, VeilCultMidasTouchActionEvent args)
    {
        if (args.Handled)
            return;

        OpenEnchantSelectionUI(uid, component, args.Performer);
        args.Handled = true;
    }

    private void OnEnchantSelected(EntityUid uid, EnchantableComponent component, EnchantSelectedMessage args)
    {
        var user = args.Actor;
        var doAfterDelay = component.Delay;
        var doAfterEventArgs = new DoAfterArgs(EntityManager, user, doAfterDelay,
            new EnchantingDoAfterEvent(args.EnchantId), uid, user)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 0.01f,
            NeedHand = false
        };

        _doAfterSystem.TryStartDoAfter(doAfterEventArgs);
    }

    private void OpenEnchantSelectionUI(EntityUid card, EnchantableComponent component, EntityUid user)
    {
        var availableEnchants = new List<EntProtoId>();

        foreach (var enchantId in component.Enchants)
        {
            if (_prototype.TryIndex(enchantId, out var enchant))
                availableEnchants.Add(enchant);
        }

        if (availableEnchants.Count > 0)
        {
            var state = new EnchantSelectionState(availableEnchants);

            _ui.OpenUi(card, EnchantUiKey.Key, user);
            _ui.SetUiState(card, EnchantUiKey.Key, state);
        }
    }

    private void OnPlayerAttached(EntityUid uid, CogscarabComponent component, PlayerAttachedEvent args)
    {
        EnsureComp<TimedDespawnComponent>(uid).Lifetime = 15f;
        RemComp<CogscarabComponent>(uid);
    }

    private void OnInit(EntityUid uid, ConfusionComponent component, ComponentInit args)
    {
        _speed.RefreshMovementSpeedModifiers(uid);
    }

    private void OnShutdown(EntityUid uid, ConfusionComponent component, ComponentRemove args)
    {
        _speed.RefreshMovementSpeedModifiers(uid);
    }

    private void Invert(EntityUid uid, ConfusionComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(-1f, -1f);
    }
}
