using System.Linq;
using System.Text;
using Content.Server.Database;
using Content.Shared.Achievements;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Commands;

/// What are you looking at here?

[AdminCommand(AdminFlags.Permissions)]
public sealed class AchievementsGrantCommand : IConsoleCommand
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IServerDbManager _db = default!;

    public string Command => "achievements_grant";
    public string Description => Loc.GetString("cmd-achievements_grant-desc");
    public string Help => Loc.GetString("cmd-achievements_grant-help", ("command", Command));

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError(Loc.GetString("cmd-achievements_grant-error-args"));
            return;
        }

        var userName = args[0];
        if (!_playerManager.TryGetSessionByUsername(userName, out var player))
        {
            shell.WriteError(Loc.GetString("cmd-achievements_grant-error-player", ("username", userName)));
            return;
        }

        if (!_prototypeManager.TryIndex<AchievementPrototype>(args[1], out var prototype))
        {
            if (!Enum.TryParse<AchievementsEnum>(args[1], true, out var achievementEnum))
            {
                shell.WriteError(Loc.GetString("cmd-achievements_grant-error-achievement", ("id", args[1])));
                return;
            }

            prototype = _prototypeManager.EnumeratePrototypes<AchievementPrototype>()
                .FirstOrDefault(p => p.Key == achievementEnum);

            if (prototype == null)
            {
                shell.WriteError(Loc.GetString("cmd-achievements_grant-error-achievement", ("id", args[1])));
                return;
            }
        }

        var result = await _db.AddAchievementAsync(player.UserId, (byte)prototype.Key);

        if (result)
        {
            shell.WriteLine(Loc.GetString("cmd-achievements_grant-success",
                ("username", userName),
                ("achievement", Loc.GetString(prototype.Name))));
        }
        else
        {
            shell.WriteLine(Loc.GetString("cmd-achievements_grant-already-has", ("username", userName)));
        }
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.SessionNames(players: _playerManager),
                Loc.GetString("cmd-achievements_grant-arg-user"));
        }

        if (args.Length == 2)
        {
            var achievements = _prototypeManager.EnumeratePrototypes<AchievementPrototype>()
                .Select(p => new CompletionOption(p.ID, Loc.GetString(p.Name)))
                .ToList();

            var enumOptions = Enum.GetNames<AchievementsEnum>()
                .Select(e => new CompletionOption(e, $"Enum: {e}"))
                .ToList();

            achievements.AddRange(enumOptions);

            return CompletionResult.FromHintOptions(
                achievements,
                Loc.GetString("cmd-achievements_grant-arg-achievement"));
        }

        return CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Permissions)]
public sealed class AchievementsRevokeCommand : IConsoleCommand
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IServerDbManager _db = default!;

    public string Command => "achievements_revoke";
    public string Description => Loc.GetString("cmd-achievements_revoke-desc");
    public string Help => Loc.GetString("cmd-achievements_revoke-help", ("command", Command));

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError(Loc.GetString("cmd-achievements_revoke-error-args"));
            return;
        }

        var userName = args[0];
        if (!_playerManager.TryGetSessionByUsername(userName, out var player))
        {
            shell.WriteError(Loc.GetString("cmd-achievements_revoke-error-player", ("username", userName)));
            return;
        }

        if (!_prototypeManager.TryIndex<AchievementPrototype>(args[1], out var prototype))
        {
            if (!Enum.TryParse<AchievementsEnum>(args[1], true, out var achievementEnum))
            {
                shell.WriteError(Loc.GetString("cmd-achievements_revoke-error-achievement", ("id", args[1])));
                return;
            }

            prototype = _prototypeManager.EnumeratePrototypes<AchievementPrototype>()
                .FirstOrDefault(p => p.Key == achievementEnum);

            if (prototype == null)
            {
                shell.WriteError(Loc.GetString("cmd-achievements_revoke-error-achievement", ("id", args[1])));
                return;
            }
        }

        var removedCount = await _db.RemoveAchievementAsync(player.UserId, (byte)prototype.Key);

        if (removedCount > 0)
        {
            shell.WriteLine(Loc.GetString("cmd-achievements_revoke-success",
                ("username", userName),
                ("achievement", Loc.GetString(prototype.Name))));
        }
        else
        {
            shell.WriteLine(Loc.GetString("cmd-achievements_revoke-not-has", ("username", userName)));
        }
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.SessionNames(players: _playerManager),
                Loc.GetString("cmd-achievements_revoke-arg-user"));
        }

        if (args.Length == 2)
        {
            if (_playerManager.TryGetSessionByUsername(args[0], out var session))
            {
                var achievements = _prototypeManager.EnumeratePrototypes<AchievementPrototype>()
                    .Select(p => new CompletionOption(p.ID, Loc.GetString(p.Name)))
                    .ToList();

                var enumOptions = Enum.GetNames<AchievementsEnum>()
                    .Select(e => new CompletionOption(e, $"Enum: {e}"))
                    .ToList();

                achievements.AddRange(enumOptions);

                return CompletionResult.FromHintOptions(
                    achievements,
                    Loc.GetString("cmd-achievements_revoke-arg-achievement"));
            }

            return CompletionResult.FromHint(Loc.GetString("cmd-achievements_revoke-arg-achievement"));
        }

        return CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Permissions)]
public sealed class AchievementsGrantAllCommand : IConsoleCommand
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IServerDbManager _db = default!;

    public string Command => "achievements_grantall";
    public string Description => Loc.GetString("cmd-achievements_grantall-desc");
    public string Help => Loc.GetString("cmd-achievements_grantall-help", ("command", Command));

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("cmd-achievements_grantall-error-args"));
            return;
        }

        var userName = args[0];
        if (!_playerManager.TryGetSessionByUsername(userName, out var player))
        {
            shell.WriteError(Loc.GetString("cmd-achievements_grantall-error-player", ("username", userName)));
            return;
        }

        var allPrototypes = _prototypeManager.EnumeratePrototypes<AchievementPrototype>().ToList();
        var grantedCount = 0;

        foreach (var prototype in allPrototypes)
        {
            var result = await _db.AddAchievementAsync(player.UserId, (byte)prototype.Key);
            if (result)
                grantedCount++;
        }

        shell.WriteLine(Loc.GetString("cmd-achievements_grantall-success",
            ("username", userName),
            ("granted", grantedCount),
            ("total", allPrototypes.Count)));
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.SessionNames(players: _playerManager),
                Loc.GetString("cmd-achievements_grantall-arg-user"));
        }

        return CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Permissions)]
public sealed class AchievementsClearCommand : IConsoleCommand
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IServerDbManager _db = default!;

    public string Command => "achievements_clear";
    public string Description => Loc.GetString("cmd-achievements_clear-desc");
    public string Help => Loc.GetString("cmd-achievements_clear-help", ("command", Command));

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("cmd-achievements_clear-error-args"));
            return;
        }

        var userName = args[0];
        if (!_playerManager.TryGetSessionByUsername(userName, out var player))
        {
            shell.WriteError(Loc.GetString("cmd-achievements_clear-error-player", ("username", userName)));
            return;
        }

        var clearedCount = await _db.ClearAllAchievementsAsync(player.UserId);

        shell.WriteLine(Loc.GetString("cmd-achievements_clear-success",
            ("username", userName),
            ("count", clearedCount)));
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.SessionNames(players: _playerManager),
                Loc.GetString("cmd-achievements_clear-arg-user"));
        }

        return CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Admin)]
public sealed class AchievementsListCommand : IConsoleCommand
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IServerDbManager _db = default!;

    public string Command => "achievements_list";
    public string Description => Loc.GetString("cmd-achievements_list-desc");
    public string Help => Loc.GetString("cmd-achievements_list-help", ("command", Command));

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("cmd-achievements_list-error-args"));
            return;
        }

        var userName = args[0];
        if (!_playerManager.TryGetSessionByUsername(userName, out var player))
        {
            shell.WriteError(Loc.GetString("cmd-achievements_list-error-player", ("username", userName)));
            return;
        }

        var achievements = await _db.GetAchievementsAsync(player.UserId);

        if (achievements.Count == 0)
        {
            shell.WriteLine(Loc.GetString("cmd-achievements_list-no"));
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine(Loc.GetString("cmd-achievements_list-header", ("username", userName)));

        foreach (var achievementEnum in achievements)
        {
            var prototype = _prototypeManager.EnumeratePrototypes<AchievementPrototype>()
                .FirstOrDefault(p => (byte)p.Key == achievementEnum.AchievementKey);

            if (prototype != null)
            {
                sb.AppendLine(Loc.GetString("cmd-achievements_list-entry",
                    ("name", Loc.GetString(prototype.Name)),
                    ("id", prototype.ID)));
            }
            else
            {
                sb.AppendLine(Loc.GetString("cmd-achievements_list-entry-no-prototype",
                    ("enum", achievementEnum)));
            }
        }

        shell.WriteLine(sb.ToString());
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.SessionNames(players: _playerManager),
                Loc.GetString("cmd-achievements_list-arg-user"));
        }

        return CompletionResult.Empty;
    }
}
