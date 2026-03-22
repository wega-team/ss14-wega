using System.Linq;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Ghost;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Interaction;

public sealed class InteractionActionSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedChatSystem _chat = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InteractionActionsComponent, GetVerbsEvent<Verb>>(OnGetVerb);
        SubscribeLocalEvent<InteractionActionsComponent, InteractionDoAfterEvent>(OnDoAfter);
    }

    private void OnGetVerb(Entity<InteractionActionsComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        // I think it might be cool... But no.
        if (HasComp<GhostComponent>(args.User))
            return;

        var availableActions = GetAvailableActions(args.User, ent);

        foreach (var actionProto in availableActions)
        {
            var verb = CreateVerbFromAction(actionProto, args.User, ent);
            args.Verbs.Add(verb);
        }
    }

    private void OnDoAfter(Entity<InteractionActionsComponent> ent, ref InteractionDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!_prototype.TryIndex(args.ActionId, out var actionProto))
            return;

        args.Handled = true;
        if (!CheckConditions(actionProto, args.User, ent))
            return;

        ExecuteAction(actionProto, args.User, ent);
    }

    private List<InteractionActionPrototype> GetAvailableActions(EntityUid user, Entity<InteractionActionsComponent> target)
    {
        var available = new List<InteractionActionPrototype>();
        var cooldowns = target.Comp.Cooldowns;

        var actions = _prototype.EnumeratePrototypes<InteractionActionPrototype>().Where(a => !a.Abstract);
        foreach (var actionProto in actions)
        {
            if (actionProto.OnlyInteractSelf && user != target.Owner)
                continue;

            if (!actionProto.CanInteractSelf && user == target.Owner)
                continue;

            if (cooldowns.TryGetValue(actionProto, out var cooldownUntil) && cooldownUntil > _timing.CurTime)
                continue;

            if (!CheckConditions(actionProto, user, target))
                continue;

            available.Add(actionProto);
        }

        // Sort by priority
        available.Sort((a, b) => b.Priority.CompareTo(a.Priority));

        return available;
    }

    private bool CheckConditions(InteractionActionPrototype action, EntityUid user, EntityUid target)
    {
        if (action.Conditions.Count == 0)
            return true;

        foreach (var condition in action.Conditions)
        {
            if (!condition.CheckWithInvert(user, target, EntityManager))
                return false;
        }

        return true;
    }

    private void TryExecuteAction(InteractionActionPrototype action, EntityUid user, EntityUid target)
    {
        if (!_net.IsServer)
            return;

        if (action.Delay > 0)
        {
            if (!string.IsNullOrWhiteSpace(action.DelayStartUserMessage))
            {
                var message = Loc.GetString(action.DelayStartUserMessage, ("user", Identity.Name(user, EntityManager, user)),
                    ("target", Identity.Name(target, EntityManager, user)));

                var popupType = action.DelayStartColorMessage == Color.Red ? PopupType.SmallCaution : PopupType.Small;
                _popup.PopupEntity(message, user, user, popupType);
                _chat.SendMessageToOne(user, message, action.DelayStartColorMessage);
            }

            if (!string.IsNullOrWhiteSpace(action.DelayStartTargetMessage))
            {
                var message = Loc.GetString(action.DelayStartTargetMessage, ("user", Identity.Name(user, EntityManager, target)),
                    ("target", Identity.Name(target, EntityManager, target)));

                var popupType = action.DelayStartColorMessage == Color.Red ? PopupType.SmallCaution : PopupType.Small;
                _popup.PopupEntity(message, target, target, popupType);
                _chat.SendMessageToOne(target, message, action.DelayStartColorMessage);
            }

            if (!string.IsNullOrWhiteSpace(action.DelayStartOtherMessage))
            {
                var filter = Filter.Local().AddAllPlayers().RemoveWhereAttachedEntity(uid => uid == user)
                    .RemoveWhereAttachedEntity(uid => uid == target);

                var popupType = action.DelayStartColorMessage == Color.Red ? PopupType.SmallCaution : PopupType.Small;
                _popup.PopupEntity(Loc.GetString(action.DelayStartOtherMessage, ("user", Identity.Name(user, EntityManager)),
                    ("target", Identity.Name(target, EntityManager))), target, filter, false, popupType);
            }

            var doAfterArgs = new DoAfterArgs(EntityManager, user, TimeSpan.FromSeconds(action.Delay),
                new InteractionDoAfterEvent(action), target, target)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
                BreakOnHandChange = true
            };

            _doAfter.TryStartDoAfter(doAfterArgs);
            return;
        }

        ExecuteAction(action, user, target);
    }

    private void ExecuteAction(InteractionActionPrototype action, EntityUid user, EntityUid target)
    {
        if (!_net.IsServer)
            return;

        foreach (var effect in action.Effects)
        {
            effect.Apply(user, target, EntityManager);
        }

        if (action.Cooldown > 0)
        {
            if (TryComp<InteractionActionsComponent>(target, out var interactionComp))
            {
                interactionComp.Cooldowns[action.ID] = _timing.CurTime + TimeSpan.FromSeconds(action.Cooldown);
                Dirty(target, interactionComp);
            }
        }
    }

    private Verb CreateVerbFromAction(InteractionActionPrototype action, EntityUid user, EntityUid target)
    {
        return new Verb
        {
            Priority = action.Priority,
            Category = VerbCategory.Interaction,
            Text = Loc.GetString(action.Name),
            Icon = action.Icon,
            Impact = LogImpact.Medium,
            DoContactInteraction = true,
            Act = () => TryExecuteAction(action, user, target)
        };
    }
}
