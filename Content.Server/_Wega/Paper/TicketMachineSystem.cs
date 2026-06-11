using Content.Shared.Interaction;
using Content.Shared.Timing;
using Content.Shared.Popups;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Examine;
using Content.Shared.Paper;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Paper;

public sealed partial class TicketMachineSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private PaperSystem _paperSystem = default!;
    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private UseDelaySystem _useDelay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TicketMachineComponent, InteractHandEvent>(OnTicket);
        SubscribeLocalEvent<TicketMachineComponent, ExaminedEvent>(OnExamined);
    }

    private void OnTicket(EntityUid machine, TicketMachineComponent component, InteractHandEvent args)
    {
        var user = args.User;
        if (!TryComp<UseDelayComponent>(machine, out var useDelay) || _useDelay.IsDelayed((machine, useDelay)))
        {
            _popup.PopupEntity(Loc.GetString("paper-component-ticket-failed"), user, user, PopupType.Small);
            return;
        }

        var number = (component.Queue + 1).ToString();
        var ticket = _entityManager.SpawnEntity(component.TicketProto, Transform(machine).Coordinates);
        if (!TryComp<PaperComponent>(ticket, out var paper) || !TryAddTicket((ticket, paper), machine, number))
            return;

        component.Queue++;
        _hands.TryPickupAnyHand(user, ticket);
        _useDelay.TryResetDelay((machine, useDelay));
    }

    private bool TryAddTicket(Entity<PaperComponent> ticket, EntityUid machine, string number)
    {

        StampDisplayInfo info = new StampDisplayInfo
        {
            StampedName = number,
            StampedColor = Color.FromHex("#333333"),
        };

        if (_paperSystem.TryStamp(ticket, info, "paper_stamp-hop"))
        {
            _paperSystem.SetContent(ticket, Loc.GetString("paper-component-ticket", ("queue", number)));
            _audio.PlayPvs(new SoundPathSpecifier("/Audio/Machines/short_print_and_rip.ogg"), machine, AudioParams.Default.WithLoop(false));
        }
        return true;
    }

    private void OnExamined(Entity<TicketMachineComponent> entity, ref ExaminedEvent args)
    {
        if (args.IsInDetailsRange)
            args.AddMarkup($"{Loc.GetString("paper-component-ticket-count", ("number", (entity.Comp.Queue + 1).ToString()))}{Environment.NewLine}");
    }
}
