using System.Linq;
using Content.Shared.Administration.Logs;
using Content.Shared.Armor;
using Content.Shared.Clothing.Upgrades.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.Clothing.Upgrades;

public sealed class ClothingUpgradeSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UpgradeableClothingComponent, InventoryRelayedEvent<CoefficientQueryEvent>>(RelayInventoryEvent,
            after: [typeof(SharedArmorSystem)]);
        SubscribeLocalEvent<UpgradeableClothingComponent, InventoryRelayedEvent<DamageModifyEvent>>(RelayInventoryEvent,
            after: [typeof(SharedArmorSystem)]);

        SubscribeLocalEvent<UpgradeableClothingComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<UpgradeableClothingComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
        SubscribeLocalEvent<UpgradeableClothingComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<UpgradeableClothingComponent, GetVerbsEvent<Verb>>(OnGetVerb);
    }

    private void RelayInventoryEvent<T>(Entity<UpgradeableClothingComponent> ent, ref InventoryRelayedEvent<T> args) where T : notnull
    {
        foreach (var upgrade in GetCurrentUpgrades(ent))
            RaiseLocalEvent(upgrade, ref args.Args);
    }

    private void OnInit(Entity<UpgradeableClothingComponent> ent, ref ComponentInit args)
    {
        _container.EnsureContainer<Container>(ent, ent.Comp.UpgradesContainerId);
    }

    private void OnAfterInteractUsing(Entity<UpgradeableClothingComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (args.Handled || !args.CanReach || !TryComp<ClothingUpgradeComponent>(args.Used, out var upgradeComponent))
            return;

        if (GetCurrentUpgrades(ent).Count >= ent.Comp.MaxUpgradeCount)
        {
            _popup.PopupPredicted(Loc.GetString("upgradeable-clothing-popup-upgrade-limit"), ent, args.User);
            return;
        }

        if (_entityWhitelist.IsWhitelistFail(ent.Comp.Whitelist, args.Used))
            return;

        if (GetCurrentUpgradeTags(ent).ToHashSet().IsSupersetOf(upgradeComponent.Tags))
        {
            _popup.PopupPredicted(Loc.GetString("upgradeable-clothing-popup-already-present"), ent, args.User);
            return;
        }

        _popup.PopupClient(Loc.GetString("clothing-upgrade-popup-insert",
            ("upgrade", args.Used), ("clothing", ent.Owner)), args.User);

        args.Handled = _container.Insert(args.Used, _container.GetContainer(ent, ent.Comp.UpgradesContainerId));

        _adminLog.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(args.User):player} inserted clothing upgrade {ToPrettyString(args.Used)} into {ToPrettyString(ent.Owner)}.");

        Dirty(ent.Owner, ent.Comp);
    }

    private void OnExamine(Entity<UpgradeableClothingComponent> ent, ref ExaminedEvent args)
    {
        var upgrades = GetCurrentUpgrades(ent);
        if (upgrades.Count == 0)
            return;

        using (args.PushGroup(nameof(UpgradeableClothingComponent)))
        {
            args.PushMarkup(Loc.GetString("clothing-upgrade-examine-header"));

            foreach (var upgrade in upgrades)
            {
                args.PushMarkup(Loc.GetString(upgrade.Comp.ExamineText));
            }
        }
    }

    private void OnGetVerb(Entity<UpgradeableClothingComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.CanComplexInteract)
            return;

        var upgrades = GetCurrentUpgrades(ent);
        if (upgrades.Count == 0)
            return;

        var user = args.User;
        foreach (var upgrade in upgrades)
        {
            var upgradeEntity = upgrade.Owner;
            var name = MetaData(upgradeEntity).EntityName;

            var v = new Verb
            {
                Priority = 1,
                Category = VerbCategory.Eject,
                Text = name,
                Impact = LogImpact.Low,
                DoContactInteraction = true,
                Act = () =>
                {
                    RemoveUpgrade(ent, upgradeEntity, user);
                }
            };

            args.Verbs.Add(v);
        }
    }

    private void RemoveUpgrade(Entity<UpgradeableClothingComponent> ent, EntityUid upgrade, EntityUid user)
    {
        if (!_container.TryGetContainer(ent, ent.Comp.UpgradesContainerId, out var container))
            return;

        if (_container.Remove(upgrade, container))
        {
            _popup.PopupPredicted(Loc.GetString("clothing-upgrade-popup-remove", ("upgrade", upgrade)), ent, user);
            _adminLog.Add(LogType.Action, LogImpact.Low,
                $"{ToPrettyString(user):player} removed clothing upgrade {ToPrettyString(upgrade)} from {ToPrettyString(ent.Owner)}.");

            _hands.TryPickupAnyHand(user, upgrade);

            Dirty(ent.Owner, ent.Comp);
        }
    }

    public HashSet<Entity<ClothingUpgradeComponent>> GetCurrentUpgrades(Entity<UpgradeableClothingComponent> ent)
    {
        if (!_container.TryGetContainer(ent, ent.Comp.UpgradesContainerId, out var container))
            return new HashSet<Entity<ClothingUpgradeComponent>>();

        var upgrades = new HashSet<Entity<ClothingUpgradeComponent>>();
        foreach (var contained in container.ContainedEntities)
        {
            if (TryComp<ClothingUpgradeComponent>(contained, out var upgradeComp))
                upgrades.Add((contained, upgradeComp));
        }

        return upgrades;
    }

    public IEnumerable<ProtoId<TagPrototype>> GetCurrentUpgradeTags(Entity<UpgradeableClothingComponent> ent)
    {
        foreach (var upgrade in GetCurrentUpgrades(ent))
        {
            foreach (var tag in upgrade.Comp.Tags)
                yield return tag;
        }
    }
}
