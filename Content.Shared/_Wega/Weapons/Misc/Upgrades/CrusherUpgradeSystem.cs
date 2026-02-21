using System.Linq;
using Content.Shared.Administration.Logs;
using Content.Shared.Armor;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Weapons.Marker;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Misc.Upgrades.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.Weapons.Misc.Upgrades;

public sealed class CrusherUpgradeSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<UpgradeableCrusherComponent, MeleeHitEvent>(RelayEvent);
        SubscribeLocalEvent<UpgradeableCrusherComponent, MarkerAttackAttemptEvent>(RelayEvent);
        SubscribeLocalEvent<UpgradeableCrusherComponent, AfterMarkerAttackedEvent>(RelayEvent);
        SubscribeLocalEvent<UpgradeableCrusherComponent, GunRefreshModifiersEvent>(RelayEvent);
        SubscribeLocalEvent<UpgradeableCrusherComponent, GunShotEvent>(RelayEvent);

        SubscribeLocalEvent<UpgradeableCrusherComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<UpgradeableCrusherComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<UpgradeableCrusherComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
        SubscribeLocalEvent<UpgradeableCrusherComponent, ExaminedEvent>(OnExamine);
    }

    private void RelayEvent<T>(Entity<UpgradeableCrusherComponent> ent, ref T args) where T : notnull
    {
        foreach (var upgrade in GetCurrentUpgrades(ent))
            RaiseLocalEvent(upgrade, ref args);
    }

    private void OnInit(Entity<UpgradeableCrusherComponent> ent, ref ComponentInit args)
        => _container.EnsureContainer<Container>(ent, ent.Comp.UpgradesContainerId);

    private void OnInteractUsing(Entity<UpgradeableCrusherComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !_tool.HasQuality(args.Used, ent.Comp.Tool))
            return;

        if (!_container.TryGetContainer(ent, ent.Comp.UpgradesContainerId, out var container) ||
            container.ContainedEntities.Count == 0)
        {
            _popup.PopupPredicted(Loc.GetString("upgradeable-crusher-popup-no-upgrades"), ent, args.User);
            return;
        }

        var upgrade = container.ContainedEntities.Last();
        if (_container.Remove(upgrade, container))
        {
            _popup.PopupPredicted(Loc.GetString("crusher-upgrade-popup-remove", ("upgrade", upgrade)), ent, args.User);
            if (TryComp<ToolComponent>(args.Used, out var tool))
                _tool.PlayToolSound(args.Used, tool, args.User);

            args.Handled = true;
        }
    }

    private void OnAfterInteractUsing(Entity<UpgradeableCrusherComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (args.Handled || !args.CanReach || !TryComp<CrusherUpgradeComponent>(args.Used, out var upgradeComponent))
            return;

        if (GetCurrentUpgrades(ent).Count >= ent.Comp.MaxUpgradeCount)
        {
            _popup.PopupPredicted(Loc.GetString("upgradeable-crusher-popup-upgrade-limit"), ent, args.User);
            return;
        }

        if (_entityWhitelist.IsWhitelistFail(ent.Comp.Whitelist, args.Used))
            return;

        if (GetCurrentUpgradeTags(ent).ToHashSet().IsSupersetOf(upgradeComponent.Tags))
        {
            _popup.PopupPredicted(Loc.GetString("upgradeable-crusher-popup-already-present"), ent, args.User);
            return;
        }

        _audio.PlayPredicted(ent.Comp.InsertSound, ent, args.User);
        _popup.PopupClient(Loc.GetString("crusher-upgrade-popup-insert", ("upgrade", args.Used), ("crusher", ent.Owner)), args.User);
        args.Handled = _container.Insert(args.Used, _container.GetContainer(ent, ent.Comp.UpgradesContainerId));

        _adminLog.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(args.User):player} inserted crusher upgrade {ToPrettyString(args.Used)} into {ToPrettyString(ent.Owner)}.");
    }

    private void OnExamine(Entity<UpgradeableCrusherComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(UpgradeableCrusherComponent)))
        {
            foreach (var upgrade in GetCurrentUpgrades(ent))
            {
                args.PushMarkup(Loc.GetString(upgrade.Comp.ExamineText));
            }
        }
    }

    public HashSet<Entity<CrusherUpgradeComponent>> GetCurrentUpgrades(Entity<UpgradeableCrusherComponent> ent)
    {
        if (!_container.TryGetContainer(ent, ent.Comp.UpgradesContainerId, out var container))
            return new HashSet<Entity<CrusherUpgradeComponent>>();

        var upgrades = new HashSet<Entity<CrusherUpgradeComponent>>();
        foreach (var contained in container.ContainedEntities)
        {
            if (TryComp<CrusherUpgradeComponent>(contained, out var upgradeComp))
                upgrades.Add((contained, upgradeComp));
        }

        return upgrades;
    }

    public IEnumerable<ProtoId<TagPrototype>> GetCurrentUpgradeTags(Entity<UpgradeableCrusherComponent> ent)
    {
        foreach (var upgrade in GetCurrentUpgrades(ent))
        {
            foreach (var tag in upgrade.Comp.Tags)
                yield return tag;
        }
    }
}
