using Content.Server.Chat.Managers;
using Content.Server.Silicons.Laws;
using Content.Shared.Chat;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Ninja.Components;
using Content.Shared.Ninja.Systems;
using Content.Shared.Popups;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Robust.Shared.Player;

namespace Content.Server.Ninja.Systems;

public sealed partial class AiLawHackerSystem : EntitySystem
{
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private IonLawSystem _ionLaw = default!;
    [Dependency] private NinjaGlovesSystem _gloves = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SiliconLawSystem _siliconLaw = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AiLawHackerComponent, BeforeInteractHandEvent>(OnBeforeInteractHand);
        SubscribeLocalEvent<AiLawHackerComponent, AiLawHackDoAfterEvent>(OnDoAfter);
    }

    private void OnBeforeInteractHand(EntityUid uid, AiLawHackerComponent comp, BeforeInteractHandEvent args)
    {
        if (args.Handled || !TryComp<SiliconLawUpdaterComponent>(args.Target, out var updater))
            return;

        if (!_gloves.AbilityCheck(uid, args, out var target))
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager, uid, comp.Delay, new AiLawHackDoAfterEvent(), target: target, used: uid, eventTarget: uid)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            MovementThreshold = 0.5f,
            CancelDuplicate = false
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        _popup.PopupEntity(Loc.GetString("ninja-ai-hack-start"), uid, uid);

        // Alert every AI that the console targets
        NotifyAiEntities(updater, "ninja-ai-hack-alert");

        args.Handled = true;
    }

    private void OnDoAfter(EntityUid uid, AiLawHackerComponent comp, AiLawHackDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target == null)
            return;

        if (!TryComp<SiliconLawUpdaterComponent>(args.Target.Value, out var updater))
            return;

        // Generate the new lawset from ion storm laws
        var newLawset = new SiliconLawset { ObeysTo = string.Empty };
        for (var i = 0; i < comp.IonLawCount; i++)
        {
            newLawset.Laws.Add(new SiliconLaw
            {
                LawString = _ionLaw.GetIonLaw(),
                Order = i + 1,
                LawIdentifierOverride = (i + 1).ToString()
            });
        }

        // Apply to every AI entity the console targets
        var query = EntityManager.CompRegistryQueryEnumerator(updater.Components);
        var anyHacked = false;
        while (query.MoveNext(out var ai))
        {
            _siliconLaw.SubvertWithLawset(ai, newLawset.Clone());
            anyHacked = true;
        }

        if (!anyHacked)
        {
            _popup.PopupEntity(Loc.GetString("ninja-ai-hack-no-target"), uid, uid);
            return;
        }

        _popup.PopupEntity(Loc.GetString("ninja-ai-hack-success"), uid, uid);

        RemComp<AiLawHackerComponent>(uid);

        var ev = new AiLawsHackedEvent(uid, args.Target.Value);
        RaiseLocalEvent(uid, ref ev);
    }

    private void NotifyAiEntities(SiliconLawUpdaterComponent updater, string locKey)
    {
        var query = EntityManager.CompRegistryQueryEnumerator(updater.Components);
        while (query.MoveNext(out var ai))
        {
            if (!TryComp<ActorComponent>(ai, out var actor))
                continue;

            var msg = Loc.GetString(locKey);
            var wrapped = Loc.GetString("chat-manager-server-wrap-message", ("message", msg));
            _chat.ChatMessageToOne(ChatChannel.Server, msg, wrapped, default, false, actor.PlayerSession.Channel, colorOverride: Color.Red);
        }
    }
}

/// <summary>
/// Raised on the ninja when AI laws have been successfully replaced.
/// </summary>
[ByRefEvent]
public record struct AiLawsHackedEvent(EntityUid Used, EntityUid Target);
