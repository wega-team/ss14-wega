using Content.Server.Chat.Systems;
using Content.Shared.Administration;
using Content.Shared.Mind;
using Robust.Shared.Console;
using Robust.Shared.Enums;

namespace Content.Server.Chat.Commands
{
    [AnyCommand]
    internal sealed class MindSayCommand : LocalizedEntityCommands
    {
        [Dependency] private readonly ChatSystem _chatSystem = default!;

        public override string Command => "mindsay";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (shell.Player is not { } player)
            {
                shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
                return;
            }

            if (player.Status != SessionStatus.InGame)
                return;

            if (player.AttachedEntity is not { } playerEntity)
            {
                shell.WriteError(Loc.GetString($"shell-must-be-attached-to-entity"));
                return;
            }

            if (args.Length < 1)
                return;

            var message = string.Join(" ", args).Trim();
            if (string.IsNullOrEmpty(message))
                return;

            // Process the mind message
            if (_chatSystem.TryProcessMindMessage(playerEntity, message, out var modifiedMessage, out var channel))
            {
                if (channel != null)
                {
                    // Check if entity has access to the channel
                    if (EntityManager.TryGetComponent<MindLinkComponent>(playerEntity, out var mindLink) &&
                        mindLink.Channels.Contains(channel.ID))
                    {
                        _chatSystem.SendMindMessage(playerEntity, modifiedMessage, channel);
                    }
                    else
                    {
                        shell.WriteError(Loc.GetString("chat-manager-no-access-mind-channel"));
                    }
                }
                else
                {
                    shell.WriteError(Loc.GetString("chat-manager-no-mind-channel"));
                }
            }
        }
    }
}
