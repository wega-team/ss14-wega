using System.Text.RegularExpressions;
using Content.Server._Wega.Speech.Components;
using Content.Shared.Speech;
using Robust.Shared.Random;

namespace Content.Server._Wega.Speech.EntitySystems;

public sealed class ResomiAccentSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    private static readonly Regex RegexSh = new("ш+", RegexOptions.Compiled);
    private static readonly Regex RegexShUpper = new("Ш+", RegexOptions.Compiled);
    private static readonly Regex RegexCh = new("ч+", RegexOptions.Compiled);
    private static readonly Regex RegexChUpper = new("Ч+", RegexOptions.Compiled);
    private static readonly Regex RegexR = new("р+", RegexOptions.Compiled);
    private static readonly Regex RegexRUpper = new("Р+", RegexOptions.Compiled);

    private static readonly List<string> ShReplacements = new() { "шш", "шшш" };
    private static readonly List<string> ShUpperReplacements = new() { "ШШ", "ШШШ" };
    private static readonly List<string> ChReplacements = new() { "щщ", "щщщ" };
    private static readonly List<string> ChUpperReplacements = new() { "ЩЩ", "ЩЩЩ" };
    private static readonly List<string> RReplacements = new() { "рр", "ррр" };
    private static readonly List<string> RUpperReplacements = new() { "РР", "РРР" };

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ResomiAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, ResomiAccentComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        message = RegexSh.Replace(message, _random.Pick(ShReplacements));
        message = RegexShUpper.Replace(message, _random.Pick(ShUpperReplacements));
        message = RegexCh.Replace(message, _random.Pick(ChReplacements));
        message = RegexChUpper.Replace(message, _random.Pick(ChUpperReplacements));
        message = RegexR.Replace(message, _random.Pick(RReplacements));
        message = RegexRUpper.Replace(message, _random.Pick(RUpperReplacements));

        args.Message = message;
    }
}
