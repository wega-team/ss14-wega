#if DEBUG
using Robust.Client.Graphics;
using Robust.Shared.Console;

namespace Content.Client.Audio;

/// <summary>
/// Command to toggle the sound insulation debug overlay.
/// </summary>
public sealed partial class ShowSoundInsulationCommand : LocalizedEntityCommands
{
    [Dependency] private IOverlayManager _overlays = default!;

    public override string Command => "showsoundinsulation";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (_overlays.HasOverlay<SoundInsulationOverlay>())
        {
            _overlays.RemoveOverlay<SoundInsulationOverlay>();
            shell.WriteLine("Sound insulation overlay is Off");
            return;
        }

        _overlays.AddOverlay(new SoundInsulationOverlay());
        shell.WriteLine("Sound insulation overlay is On");
    }
}
#endif
