using System.Text.RegularExpressions;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Content.Shared.Paper;
using Content.Server.Administration;
using Content.Shared.Popups;

namespace Content.Server.Paper;

public sealed class ChameleonStampSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly QuickDialogSystem _quickDialog = default!;

    private static readonly Regex HexColorRegex = new Regex("^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{8})$", RegexOptions.Compiled);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChameleonStampComponent, GetVerbsEvent<AlternativeVerb>>(AddSignVerb);
    }

    private void AddSignVerb(Entity<ChameleonStampComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.Using.HasValue
            || !HasComp<StampComponent>(entity))
            return;

        var user = args.User;
        AlternativeVerb verb = new()
        {
            Text = Loc.GetString("paper-component-verb-chameleon-stamp"),
            Act = () =>
            {
                ChangeStamp(user, entity);
            },
        };
        args.Verbs.Add(verb);
    }

    public void ChangeStamp(EntityUid user, Entity<ChameleonStampComponent> entity)
    {
        if (!TryComp<ActorComponent>(user, out var playerActor) || !TryComp<StampComponent>(entity, out var stamp) || stamp == null)
            return;

        var playerSession = playerActor.PlayerSession;
        _quickDialog.OpenDialog(playerSession, Loc.GetString("paper-component-chameleon-stamp"),
            Loc.GetString("paper-component-chameleon-stamp-name"), Loc.GetString("paper-component-chameleon-stamp-color"),
            (string name, string color) =>
            {
                var finalName = string.IsNullOrWhiteSpace(name)
                    ? Loc.GetString("chameleon-stamp-default-name")
                    : name;

                var finalColor = string.IsNullOrWhiteSpace(color)
                    ? Loc.GetString("chameleon-stamp-default-color")
                    : color;

                if (IsValidHexColor(finalColor))
                {
                    if (Color.TryParse(finalColor, out var parsedColor))
                    {
                        stamp.StampedName = finalName;
                        stamp.StampedColor = parsedColor;
                        _popup.PopupEntity(Loc.GetString("chameleon-stamp-succes"), user, user, PopupType.Small);
                    }
                }
                else
                {
                    _popup.PopupEntity(Loc.GetString("chameleon-stamp-failed"), user, user, PopupType.Small);
                }
            });
    }

    private bool IsValidHexColor(string color)
    {
        return HexColorRegex.IsMatch(color);
    }
}
