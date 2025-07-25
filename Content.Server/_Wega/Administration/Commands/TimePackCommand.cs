using System.Linq;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Server.Player;
using Content.Server.Players.PlayTimeTracking;
using Content.Server.Database;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Permissions)]
public sealed class TimePackCommand : IConsoleCommand
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IPlayerLocator _locator = default!;

    public string Command => "timepack";
    public string Description => "Executes a set of commands based on the selected pack(s)";
    public string Help => "Usage: timepack <username> <pack1> [<pack2> ...]";

    private readonly Dictionary<int, List<(string Tracker, int Minutes)>> _packs = new()
    {
        { 1, new List<(string, int)>
            {
                ("Overall", 8400),
                ("JobCaptain", 1200),
                ("JobStationEngineer", 1200),
                ("JobMedicalDoctor", 1200),
                ("JobSecurityOfficer", 1200),
                ("JobWarden", 600),
                ("JobAtmosphericTechnician", 1200),
                ("JobChemist", 600),
                ("JobScientist", 900),
                ("JobSalvageSpecialist", 600),
                ("JobServiceWorker", 60)
            }
        },
        { 2, new List<(string, int)>
            {
                ("JobSecurityOfficer", 1200),
                ("JobWarden", 600),
                ("Overall", 600)
            }
        },
        { 3, new List<(string, int)>
            {
                ("JobAtmosphericTechnician", 600),
                ("JobStationEngineer", 1200)
            }
        },
        { 4, new List<(string, int)>
            {
                ("JobChemist", 600),
                ("JobMedicalDoctor", 1200)
            }
        },
        { 5, new List<(string, int)>
            {
                ("JobScientist", 900)
            }
        },
        { 6, new List<(string, int)>
            {
                ("JobSalvageSpecialist", 600),
                ("Overall", 2400)
            }
        },
        { 7, new List<(string, int)>
            {
                ("JobBorg", 900)
            }
        }
    };

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 2)
        {
            shell.WriteError("Invalid arguments. Usage: timepack <username> <pack1> [<pack2> ...]");
            return;
        }

        var username = args[0];
        var packIds = args.Skip(1).Select(arg =>
        {
            if (int.TryParse(arg, out var id))
                return id;
            return (int?)null;
        }).Where(id => id.HasValue).Select(id => id!.Value).ToList();

        if (packIds.Count == 0)
        {
            shell.WriteError("No valid packs provided.");
            return;
        }

        var playerData = await _locator.LookupIdByNameOrIdAsync(username);
        if (playerData == null)
        {
            shell.WriteError($"Player '{username}' not found in database.");
            return;
        }

        var userId = playerData.UserId;
        var isOnline = _playerManager.TryGetSessionByUsername(username, out var playerSession);

        var timeUpdates = new Dictionary<string, TimeSpan>();
        foreach (var packId in packIds)
        {
            if (!_packs.TryGetValue(packId, out var packData))
            {
                shell.WriteError($"Pack {packId} does not exist.");
                continue;
            }

            foreach (var (tracker, minutes) in packData)
            {
                var timeToAdd = TimeSpan.FromMinutes(minutes);
                if (timeUpdates.TryGetValue(tracker, out var existingTime))
                {
                    timeUpdates[tracker] = existingTime + timeToAdd;
                }
                else
                {
                    timeUpdates[tracker] = timeToAdd;
                }
            }
        }

        foreach (var (tracker, time) in timeUpdates)
        {
            await _db.AddPlayTimeAsync(userId, tracker, time);
            shell.WriteLine($"Added {time.TotalMinutes} minutes to {tracker} for {username}");
        }

        if (isOnline && playerSession != null)
        {
            var playTimeTracking = IoCManager.Resolve<PlayTimeTrackingManager>();
            foreach (var (tracker, time) in timeUpdates)
            {
                playTimeTracking.AddTimeToTracker(playerSession, tracker, time);
            }
            playTimeTracking.QueueSendTimers(playerSession);
        }

        shell.WriteLine($"Successfully applied timepack to {username} with packs: {string.Join(", ", packIds)}");
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
            return CompletionResult.FromHint("Enter username");

        if (args.Length > 1)
        {
            var hintOptions = _packs.Keys.Select(id => id.ToString()).ToList();
            return CompletionResult.FromHintOptions(hintOptions, "Enter pack number(s)");
        }

        return CompletionResult.Empty;
    }
}
