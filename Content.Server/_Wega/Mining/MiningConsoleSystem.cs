using System.Linq;
using Content.Server.Power.Components;
using Content.Server.Research.Disk;
using Content.Server.Stack;
using Content.Shared.Audio;
using Content.Shared.Mining;
using Content.Shared.Mining.Components;
using Content.Shared.Stacks;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.Mining;

public sealed class MiningConsoleSystem : EntitySystem
{
    [Dependency] private readonly SharedAmbientSoundSystem _ambient = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly StackSystem _stack = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    private static readonly ProtoId<StackPrototype> Credit = "Credit";
    private static readonly EntProtoId Disk = "ResearchDisk";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MiningConsoleComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<MiningConsoleComponent, BoundUIOpenedEvent>(OnUIOpened);

        SubscribeLocalEvent<MiningConsoleComponent, MiningConsoleToggleActivationMessage>(OnToggleActivation);
        SubscribeLocalEvent<MiningConsoleComponent, MiningConsoleToggleServerActivationMessage>(OnToggleServerActivation);
        SubscribeLocalEvent<MiningConsoleComponent, MiningConsoleChangeServerStageMessage>(OnChangeServerStage);
        SubscribeLocalEvent<MiningConsoleComponent, MiningConsoleToggleModeMessage>(OnToggleMode);
        SubscribeLocalEvent<MiningConsoleComponent, MiningConsoleToggleUpdateMessage>(OnUpdate);
        SubscribeLocalEvent<MiningConsoleComponent, MiningConsoleWithdrawMessage>(OnWithdraw);
    }

    private void OnInit(EntityUid uid, MiningConsoleComponent comp, MapInitEvent args)
    {
        comp.LinkedServer = EnsureAccount();
    }

    private void OnUIOpened(Entity<MiningConsoleComponent> entity, ref BoundUIOpenedEvent args)
    {
        UpdateUi(entity);
    }

    private void OnToggleActivation(Entity<MiningConsoleComponent> entity, ref MiningConsoleToggleActivationMessage args)
    {
        if (entity.Comp.LinkedServer == null || !TryComp<MiningAccountComponent>(entity.Comp.LinkedServer.Value, out var account))
            return;

        SetGlobalActivation(entity.Owner, !account.GlobalActivation);
    }

    private void OnToggleServerActivation(Entity<MiningConsoleComponent> entity, ref MiningConsoleToggleServerActivationMessage args)
    {
        var serverUid = GetEntity(args.ServerUid);
        ToggleServerActivation(serverUid);
        UpdateUi(entity);
    }

    private void OnToggleMode(Entity<MiningConsoleComponent> entity, ref MiningConsoleToggleModeMessage args)
    {
        if (entity.Comp.LinkedServer == null || !TryComp<MiningAccountComponent>(entity.Comp.LinkedServer.Value, out var account))
            return;

        var newMode = account.GlobalMode == MiningMode.Credits ? MiningMode.Research : MiningMode.Credits;
        SwitchGlobalMode(entity.Owner, newMode);
        UpdateUi(entity);
    }

    private void OnChangeServerStage(Entity<MiningConsoleComponent> entity, ref MiningConsoleChangeServerStageMessage args)
    {
        var serverUid = GetEntity(args.ServerUid);
        if (TryComp<MiningServerComponent>(serverUid, out var server))
        {
            var newStage = Math.Clamp(server.MiningStage + args.Delta, 1, 3);
            SetServerStage(serverUid, newStage);
            UpdateUi(entity);
        }
    }

    private void OnUpdate(Entity<MiningConsoleComponent> entity, ref MiningConsoleToggleUpdateMessage arg)
        => UpdateUi(entity);

    private void OnWithdraw(Entity<MiningConsoleComponent> entity, ref MiningConsoleWithdrawMessage args)
    {
        if (entity.Comp.LinkedServer == null || !TryComp<MiningAccountComponent>(entity.Comp.LinkedServer.Value, out var account))
            return;

        if (account.Credits >= 1)
        {
            _stack.SpawnAtPosition((int)account.Credits, Credit, Transform(entity).Coordinates);
            account.Credits = 0;
        }

        if (account.ResearchPoints >= 1)
        {
            var disk = Spawn(Disk, Transform(entity).Coordinates);
            EnsureComp<ResearchDiskComponent>(disk).Points = (int)account.ResearchPoints;
            account.ResearchPoints = 0;
        }

        UpdateUi(entity);
    }

    public void UpdateUi(Entity<MiningConsoleComponent> entity)
    {
        if (entity.Comp.LinkedServer == null || !TryComp<MiningAccountComponent>(entity.Comp.LinkedServer.Value, out var account))
            return;

        var servers = new List<MiningServerData>();
        var query = EntityQueryEnumerator<MiningServerComponent>();
        while (query.MoveNext(out var serverUid, out var server))
        {
            servers.Add(new MiningServerData(
                GetNetEntity(serverUid),
                server.MiningStage,
                server.CurrentTemperature,
                server.IsBroken,
                server.IsActive
            ));
        }

        var state = new MiningConsoleBoundInterfaceState(
            account.Credits,
            account.ResearchPoints,
            account.GlobalMode,
            account.GlobalActivation,
            servers
        );

        _ui.SetUiState(entity.Owner, MiningConsoleUiKey.Key, state);
    }

    private EntityUid? EnsureAccount()
    {
        var query = EntityQueryEnumerator<MiningAccountComponent>();
        if (query.MoveNext(out var uid, out _))
            return uid;

        return null;
    }

    public void SwitchGlobalMode(EntityUid console, MiningMode mode)
    {
        if (!TryComp<MiningConsoleComponent>(console, out var miningConsole) || miningConsole.LinkedServer == null
            || !TryComp<MiningAccountComponent>(miningConsole.LinkedServer, out var account))
            return;

        account.GlobalMode = mode;
        var query = EntityQueryEnumerator<MiningServerComponent>();
        while (query.MoveNext(out _, out var server))
            server.Mode = mode;
    }

    public void SetServerStage(EntityUid uid, int stage, MiningServerComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        comp.MiningStage = Math.Clamp(stage, 1, 3);
        if (TryComp<PowerConsumerComponent>(uid, out var consumer))
            consumer.DrawRate = comp.ActualPowerConsumption;

        UpdateAppearance(uid, comp);
    }

    public void SetGlobalActivation(EntityUid console, bool activate)
    {
        if (!TryComp<MiningConsoleComponent>(console, out var miningConsole) || miningConsole.LinkedServer == null
            || !TryComp<MiningAccountComponent>(miningConsole.LinkedServer, out var account))
            return;

        account.GlobalActivation = activate;
        var query = EntityQueryEnumerator<MiningServerComponent, PowerConsumerComponent>();
        while (query.MoveNext(out var serverUid, out var server, out var consumer))
        {
            if (!server.IsBroken && consumer.ReceivedPower >= server.ActualPowerConsumption)
            {
                server.IsActive = activate;
                UpdateAppearance(serverUid, server);
                _ambient.SetAmbience(serverUid, activate);
            }
        }

        UpdateUi((console, miningConsole));
    }

    public void ToggleServerActivation(EntityUid serverUid)
    {
        if (!TryComp<MiningServerComponent>(serverUid, out var server) || server.IsBroken
            || !TryComp<PowerConsumerComponent>(serverUid, out var consumer) || consumer.ReceivedPower < server.ActualPowerConsumption)
            return;

        server.IsActive = !server.IsActive;
        UpdateAppearance(serverUid, server);
        _ambient.SetAmbience(serverUid, server.IsActive);
    }

    private void UpdateAppearance(EntityUid uid, MiningServerComponent? server = null)
    {
        if (!Resolve(uid, ref server))
            return;

        if (TryComp<AppearanceComponent>(uid, out var appearance))
        {
            _appearance.SetData(uid, MiningServerVisuals.MiningStage, server.MiningStage, appearance);
            _appearance.SetData(uid, MiningServerVisuals.IsActive, server.IsActive, appearance);
        }
    }
}
