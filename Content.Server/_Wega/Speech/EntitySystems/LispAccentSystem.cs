using System.Text;
using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Content.Shared.Speech;

namespace Content.Server.Speech.EntitySystems
{
    public sealed class HissingAccentSystem : EntitySystem
    {
        public override void Initialize()
        {
            SubscribeLocalEvent<LispAccentComponent, AccentGetEvent>(OnAccent);
        }

        private static readonly Regex WordSplitRegex = new Regex(@"(\W+)", RegexOptions.Compiled);

        private static readonly IReadOnlyDictionary<string, string> Replacements = new Dictionary<string, string>()
        {
            { "с", "ш" },
            { "С", "Ш" },
            { "р", "л" },
            { "Р", "Л" },
            { "л", "в" },
            { "Л", "В" },
            { "г", "х" },
            { "Г", "Х" },
            { "д", "з" },
            { "Д", "З" },
            { "т", "с" },
            { "Т", "С" },
            { "з", "ж" },
            { "З", "Ж" },
            { "ж", "з" },
            { "Ж", "З" },
            { "ш", "ф" },
            { "Ш", "Ф" },
            { "щ", "ф" },
            { "Щ", "Ф" },
            { "ч", "ц" },
            { "Ч", "Ц" }
        };

        public string Accentuate(string message)
        {
            var words = WordSplitRegex.Split(message);
            var result = new StringBuilder();

            foreach (var word in words)
            {
                if (string.IsNullOrWhiteSpace(word))
                {
                    result.Append(word);
                    continue;
                }

                var newWord = new StringBuilder(word);
                foreach (var (key, value) in Replacements)
                {
                    newWord.Replace(key, value);
                }

                result.Append(newWord.ToString());
            }

            return result.ToString();
        }

        private void OnAccent(EntityUid uid, LispAccentComponent component, AccentGetEvent args)
        {
            args.Message = Accentuate(args.Message);
        }
    }
}
