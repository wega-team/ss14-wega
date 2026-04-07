using System.Linq;
using Content.Shared.Body;
using Content.Shared.Genetics;
using Content.Shared.Genetics.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;

namespace Content.Server.Genetics.System;

public sealed class EnsureMarkingSystem : EntitySystem
{
    [Dependency] private readonly SharedVisualBodySystem _visualBody = default!;

    public static readonly ProtoId<MarkingPrototype> DefaultHorns = "LizardHornsDemonic";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EnsureHornsGenComponent, ComponentInit>(OnHornsInit);
        SubscribeLocalEvent<EnsureHornsGenComponent, ComponentShutdown>(OnHornsShutdown);
    }

    private void OnHornsInit(Entity<EnsureHornsGenComponent> ent, ref ComponentInit args)
    {
        if (HasComp<VisualBodyComponent>(ent))
            ApplyHornsMarking(ent);
    }

    private void OnHornsShutdown(Entity<EnsureHornsGenComponent> ent, ref ComponentShutdown args)
    {
        if (HasComp<VisualBodyComponent>(ent))
            RemoveHornsMarking(ent);
    }

    private void ApplyHornsMarking(EntityUid ent)
    {
        if (!_visualBody.TryGatherMarkingsData(ent, null, out _, out _, out var applied))
            return;

        // Add horns to head organ
        foreach (var (organCategory, organMarkings) in applied)
        {
            if (!organMarkings.TryGetValue(HumanoidVisualLayers.HeadTop, out var markings))
            {
                markings = new List<Marking>();
                organMarkings[HumanoidVisualLayers.HeadTop] = markings;
            }

            // Check if horns already exist
            if (!markings.Any(m => m.MarkingId == DefaultHorns))
            {
                markings.Add(new Marking(DefaultHorns, new List<Color> { Color.Black }));
            }
        }

        _visualBody.ApplyMarkings(ent, applied);
    }

    private void RemoveHornsMarking(EntityUid ent)
    {
        if (!_visualBody.TryGatherMarkingsData(ent, null, out _, out _, out var applied))
            return;

        foreach (var organMarkings in applied.Values)
        {
            if (organMarkings.TryGetValue(HumanoidVisualLayers.HeadTop, out var markings))
            {
                markings.RemoveAll(m => m.MarkingId == DefaultHorns);
                if (markings.Count == 0) organMarkings.Remove(HumanoidVisualLayers.HeadTop);
            }
        }

        _visualBody.ApplyMarkings(ent, applied);
    }

    public void UpdateMarkingCategory(
        EntityUid ent,
        HumanoidVisualLayers layer,
        string[] colorR, string[] colorG, string[] colorB,
        string[] style, string species,
        List<MarkingPrototypeInfo> markingPrototypes,
        string[]? secondaryColorR = null,
        string[]? secondaryColorG = null,
        string[]? secondaryColorB = null)
    {
        // Remove existing markings on this layer
        if (!_visualBody.TryGatherMarkingsData(ent, null, out _, out _, out var applied))
            return;

        foreach (var organMarkings in applied.Values)
        {
            if (organMarkings.ContainsKey(layer))
                organMarkings.Remove(layer);
        }

        // Check if we should skip (all zeros)
        if (style.All(c => c == "0"))
        {
            _visualBody.ApplyMarkings(ent, applied);
            return;
        }

        // Handle horns special case
        if (layer == HumanoidVisualLayers.HeadTop && HasComp<EnsureHornsGenComponent>(ent))
        {
            foreach (var organMarkings in applied.Values)
            {
                if (!organMarkings.TryGetValue(layer, out var markings))
                {
                    markings = new List<Marking>();
                    organMarkings[layer] = markings;
                }
                markings.Add(new Marking(DefaultHorns, new List<Color> { Color.Black }));
            }
            _visualBody.ApplyMarkings(ent, applied);
            return;
        }

        // Find best matching marking
        var bestMatch = FindBestMatchingMarking(style, species, markingPrototypes);
        if (bestMatch == null)
            return;

        // Build colors
        string redHex = colorR[0] + colorR[1];
        string greenHex = colorG[0] + colorG[1];
        string blueHex = colorB[0] + colorB[1];

        int red = Convert.ToInt32(redHex, 16);
        int green = Convert.ToInt32(greenHex, 16);
        int blue = Convert.ToInt32(blueHex, 16);

        var mainColor = new Color(red / 255f, green / 255f, blue / 255f);
        var colors = new List<Color> { mainColor };

        // Add secondary color for hair
        if (layer == HumanoidVisualLayers.Hair &&
            secondaryColorR != null && secondaryColorG != null && secondaryColorB != null)
        {
            string secondaryRedHex = secondaryColorR[0] + secondaryColorR[1];
            string secondaryGreenHex = secondaryColorG[0] + secondaryColorG[1];
            string secondaryBlueHex = secondaryColorB[0] + secondaryColorB[1];

            int secondaryRed = Convert.ToInt32(secondaryRedHex, 16);
            int secondaryGreen = Convert.ToInt32(secondaryGreenHex, 16);
            int secondaryBlue = Convert.ToInt32(secondaryBlueHex, 16);

            var secondaryColor = new Color(secondaryRed / 255f, secondaryGreen / 255f, secondaryBlue / 255f);
            colors.Add(secondaryColor);
        }

        // Apply marking to all organs that support this layer
        foreach (var organMarkings in applied.Values)
        {
            if (!organMarkings.TryGetValue(layer, out var markings))
            {
                markings = new List<Marking>();
                organMarkings[layer] = markings;
            }
            markings.Add(new Marking(bestMatch.MarkingPrototypeId, colors));
        }

        _visualBody.ApplyMarkings(ent, applied);
    }

    private MarkingPrototypeInfo? FindBestMatchingMarking(string[] style, string species, List<MarkingPrototypeInfo> markingPrototypes)
    {
        MarkingPrototypeInfo? bestMatch = null;
        int bestScore = int.MaxValue;

        foreach (var marking in markingPrototypes)
        {
            // Check species compatibility
            if (!string.IsNullOrEmpty(marking.Groups) && !marking.Groups.Contains(species))
                continue;

            int score = CalculateStyleMatchScore(marking.HexValue, style);
            if (score < bestScore)
            {
                bestScore = score;
                bestMatch = marking;
            }
        }

        return bestMatch;
    }

    private int CalculateStyleMatchScore(string[] markingStyle, string[] targetStyle)
    {
        int score = 0;
        for (int i = 0; i < markingStyle.Length; i++)
        {
            if (i >= targetStyle.Length)
                break;

            int markingValue = Convert.ToInt32(markingStyle[i], 16);
            int targetValue = Convert.ToInt32(targetStyle[i], 16);
            score += Math.Abs(markingValue - targetValue);
        }
        return score;
    }
}
