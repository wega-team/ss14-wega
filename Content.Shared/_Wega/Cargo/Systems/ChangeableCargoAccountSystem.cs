using Robust.Shared.Prototypes;
using Content.Shared.Verbs;
using Content.Shared.Access.Systems;
using Content.Shared.Popups;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Cargo.Components;

namespace Content.Shared.Cargo;

public sealed class ChangeableCargoAccountSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly AccessReaderSystem _accessReaderSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangeableCargoAccountComponent, GetVerbsEvent<Verb>>(OnGetVerb);
        SubscribeLocalEvent<ChangeableCargoAccountComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(EntityUid uid, ChangeableCargoAccountComponent component, ExaminedEvent args)
    {
        if (component.Accounts.Count < 2)
            return;

        var account = GetAccount(component);

        if (!_prototypeManager.TryIndex<CargoAccountPrototype>(account.Account, out var proto))
            return;

        args.PushMarkup(Loc.GetString("console-set-account", ("account", Loc.GetString(proto.Name))));
    }

    private ChangeableCargoAccount GetAccount(ChangeableCargoAccountComponent component)
    {
        return component.Accounts[component.CurrentAccount];
    }

    private void OnGetVerb(EntityUid uid, ChangeableCargoAccountComponent component, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.CanComplexInteract)
            return;

        if (component.Accounts.Count < 2)
            return;

        if (!_accessReaderSystem.IsAllowed(args.User, uid))
            return;

        for (var i = 0; i < component.Accounts.Count; i++)
        {
            var account = component.Accounts[i];
            var proto = _prototypeManager.Index<CargoAccountPrototype>(account.Account);
            var index = i;

            var v = new Verb
            {
                Priority = 1,
                Category = VerbCategory.CargoAccount,
                Text = Loc.GetString(proto.Code),
                Disabled = i == component.CurrentAccount,
                Impact = LogImpact.Low,
                DoContactInteraction = true,
                Act = () =>
                {
                    TrySetAccount(uid, component, index, args.User);
                }
            };

            args.Verbs.Add(v);
        }
    }

    public bool TrySetAccount(EntityUid uid, ChangeableCargoAccountComponent component, int index, EntityUid? user = null)
    {
        if (index < 0 || index >= component.Accounts.Count)
            return false;

        if (user != null && !_accessReaderSystem.IsAllowed(user.Value, uid))
            return false;

        SetAccount(uid, component, index, user);

        return true;
    }

    private void SetAccount(EntityUid uid, ChangeableCargoAccountComponent component, int index, EntityUid? user = null)
    {
        var account = component.Accounts[index];
        component.CurrentAccount = index;
        Dirty(uid, component);

        if (_prototypeManager.TryIndex<CargoAccountPrototype>(account.Account, out var prototype))
        {
            if (user != null)
                _popupSystem.PopupClient(Loc.GetString("console-set-account", ("account", Loc.GetString(prototype.Name))), uid, user.Value);
        }

        if (TryComp(uid, out CargoOrderConsoleComponent? consoleComp))
        {
            consoleComp.Account = account.Account;
            consoleComp.AnnouncementChannel = account.AnnouncementChannel;

            Dirty(uid, consoleComp);
        }
    }
}
