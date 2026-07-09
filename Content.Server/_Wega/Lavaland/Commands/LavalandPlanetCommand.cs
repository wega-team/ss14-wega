using System.Linq;
using Content.Server.Lavaland.Systems;
using Content.Shared.Administration;
using Content.Shared.Lavaland;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Server | AdminFlags.Mapping)]
public sealed partial class LavalandPlanetCommand : IConsoleCommand
{
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private IPrototypeManager _protoManager = default!;

    public string Command => "lavalandplanet";
    public string Description => Loc.GetString("cmd-lavalandplanet-desc");
    public string Help => Loc.GetString("cmd-lavalandplanet-help", ("command", Command));

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1 || args.Length > 2)
        {
            shell.WriteError(Loc.GetString("cmd-lavalandplanet-error-args"));
            return;
        }

        var prototypeId = args[0];
        if (!_protoManager.TryIndex<LavalandPlanetPrototype>(prototypeId, out var planetProto))
        {
            shell.WriteError(Loc.GetString("cmd-lavalandplanet-error-prototype", ("id", prototypeId)));
            return;
        }

        var mapSystem = _entManager.System<SharedMapSystem>();
        var lavalandSystem = _entManager.System<LavalandSystem>();

        MapId mapId;
        if (args.Length == 2)
        {
            if (!int.TryParse(args[1], out var mapInt))
            {
                shell.WriteError(Loc.GetString("cmd-lavalandplanet-error-map", ("map", args[1])));
                return;
            }

            mapId = new MapId(mapInt);
            if (mapSystem.MapExists(mapId))
            {
                shell.WriteError(Loc.GetString("cmd-lavalandplanet-error-exists", ("mapId", mapId)));
                return;
            }

            mapSystem.CreateMap(mapId);
        }
        else
        {
            mapSystem.CreateMap(out mapId);
        }

        var mapUid = mapSystem.GetMapOrInvalid(mapId);
        if (mapUid == EntityUid.Invalid)
        {
            shell.WriteError(Loc.GetString("cmd-lavalandplanet-error-create", ("mapId", mapId)));
            return;
        }

        lavalandSystem.GenerateLavalandPlanet(mapUid, mapId, planetProto, avanpostUid: null);

        if (shell.Player?.AttachedEntity is { Valid: true } _)
        {
            shell.ExecuteCommand($"tp 0 0 {mapId}");
        }

        shell.WriteLine(Loc.GetString("cmd-lavalandplanet-success",
            ("planet", planetProto.ID), ("mapId", mapId)));
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var options = _protoManager.EnumeratePrototypes<LavalandPlanetPrototype>()
                .Select(o => new CompletionOption(o.ID, $"Planet: {o.ID}"))
                .ToList();

            return CompletionResult.FromHintOptions(
                options,
                Loc.GetString("cmd-lavalandplanet-arg-prototype"));
        }

        if (args.Length == 2)
        {
            return CompletionResult.FromHint(Loc.GetString("cmd-lavalandplanet-arg-map"));
        }

        return CompletionResult.Empty;
    }
}
