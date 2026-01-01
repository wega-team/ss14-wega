using System.Linq;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Prototypes;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Localizations;
using Robust.Shared.Prototypes;

namespace Content.Shared.Botany.Systems;

public abstract partial class SharedPlantAnalyzerSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    #region Loc

    public string GasesToLocalizedStrings(List<Gas> gases)
    {
        if (gases.Count == 0)
            return "";

        List<string> gasesLoc = [];
        var gasPrototypes = _prototypeManager.EnumeratePrototypes<GasPrototype>().ToArray();
        foreach (var gas in gases)
        {
            var gasProto = gasPrototypes.FirstOrDefault(g =>
                g.ID.Equals(gas.ToString(), StringComparison.OrdinalIgnoreCase));

            if (gasProto != null)
            {
                gasesLoc.Add(Loc.GetString(gasProto.Name));
            }
            else
            {
                gasesLoc.Add(Loc.GetString($"gases-{gas.ToString().ToLower()}"));
            }
        }

        return ContentLocalizationManager.FormatList(gasesLoc);
    }

    public string ChemicalsToLocalizedStrings(List<string> ids)
    {
        if (ids.Count == 0)
            return "";

        List<string> locStrings = [];
        foreach (var id in ids)
            locStrings.Add(_prototypeManager.TryIndex<ReagentPrototype>(id, out var prototype) ? prototype.LocalizedName : id);

        return ContentLocalizationManager.FormatList(locStrings);
    }

    public (string Singular, string Plural, string First) ProduceToLocalizedStrings(List<EntProtoId> ids)
    {
        if (ids.Count == 0)
            return ("", "", "");

        List<string> singularStrings = [];
        List<string> pluralStrings = [];
        foreach (var id in ids)
        {
            var singular = _prototypeManager.TryIndex(id, out var prototype) ? prototype.Name : id.Id;
            var plural = singular switch
            {
                string s when s.EndsWith("о") => s + "в",
                string s when s.EndsWith("е") => s + "й",
                string s when s.EndsWith("ь") => s.Substring(0, s.Length - 1) + "ей",
                string s when s.EndsWith("й") => s.Substring(0, s.Length - 1) + "ев",
                _ => singular
            };

            singularStrings.Add(singular);
            pluralStrings.Add(plural);
        }

        return (
            ContentLocalizationManager.FormatListToOr(singularStrings),
            ContentLocalizationManager.FormatListToOr(pluralStrings),
            singularStrings[0]
        );
    }

    #endregion
}
