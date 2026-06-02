using Content.Server.Chat.Systems;
using Content.Server.Stack;
using Content.Server.Station.Systems;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Stacks;
using Robust.Shared.Prototypes;

namespace Content.Server.Cargo;

public sealed partial class CargoHackerSystem : SharedCargoHackerSystem
{
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private SharedCargoSystem _cargo = default!;
    [Dependency] private StackSystem _stack = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CargoHackerComponent, CargoHackDoAfterEvent>(OnDoAfter);
    }

    private void OnDoAfter(Entity<CargoHackerComponent> ent, ref CargoHackDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target == null)
            return;

        var console = args.Target.Value;
        if (_station.GetOwningStation(console) is not {} station)
            return;

        var cargoAccount = new ProtoId<CargoAccountPrototype>("Cargo");
        var balance = _cargo.GetBalanceFromAccount((station, null), cargoAccount);
        var stolen = (int)(balance * ent.Comp.StealFraction);

        if (stolen <= 0)
            return;

        _cargo.UpdateBankAccount((station, null), -stolen, cargoAccount);

        _stack.SpawnAtPosition(stolen, new ProtoId<StackPrototype>("Credit"), Transform(args.User).Coordinates);

        _chat.DispatchGlobalAnnouncement(
            Loc.GetString("ninja-cargo-hack-announcement", ("amount", stolen)),
            playSound: true,
            colorOverride: Color.Red);

        RemComp<CargoHackerComponent>(ent);

        var ev = new CargoHackedEvent(ent, console);
        RaiseLocalEvent(args.User, ref ev);
    }
}

/// <summary>
/// Raised on the user (ninja) after successfully hacking a cargo console.
/// </summary>
[ByRefEvent]
public record struct CargoHackedEvent(EntityUid User, EntityUid Target);
