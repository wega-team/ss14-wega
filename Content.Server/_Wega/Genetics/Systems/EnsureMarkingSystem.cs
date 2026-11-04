using System.Linq;
using Content.Shared.Body;
using Content.Shared.Genetics;
using Content.Shared.Genetics.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;

namespace Content.Server.Genetics.System;

public sealed partial class EnsureMarkingSystem : EntitySystem
{
    [Dependency] private SharedVisualBodySystem _visualBody = default!;

    private static readonly ProtoId<MarkingPrototype> DefaultHorns = "LizardHornsDemonic";

    [SubscribeLocalEvent]
    private void OnHornsInit(Entity<EnsureHornsGenComponent> ent, ref ComponentInit args)
    {
        if (HasComp<VisualBodyComponent>(ent))
            ApplyHornsMarking(ent);
    }

    [SubscribeLocalEvent]
    private void OnHornsShutdown(Entity<EnsureHornsGenComponent> ent, ref ComponentShutdown args)
    {
        if (HasComp<VisualBodyComponent>(ent))
            RemoveHornsMarking(ent);
    }

    private void ApplyHornsMarking(EntityUid ent)
    {
        if (!_visualBody.TryGatherMarkingsData(ent, null, out _, out _, out var applied))
            return;

        foreach (var organMarkings in applied.Values)
        {
            if (!organMarkings.TryGetValue(HumanoidVisualLayers.HeadTop, out var markings))
            {
                markings = new List<Marking>();
                organMarkings[HumanoidVisualLayers.HeadTop] = markings;
            }

            if (!markings.Any(m => m.MarkingId == DefaultHorns))
                markings.Add(new Marking(DefaultHorns, new List<Color> { Color.Black }));
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
                if (markings.Count == 0)
                    organMarkings.Remove(HumanoidVisualLayers.HeadTop);
            }
        }

        _visualBody.ApplyMarkings(ent, applied);
    }

    public void UpdateMarkingCategory(
        EntityUid ent,
        HumanoidVisualLayers layer,
        uint[] colors,
        string style,
        string species)
    {
        if (!_visualBody.TryGatherMarkingsData(ent, null, out _, out _, out var applied))
            return;

        foreach (var organMarkings in applied.Values)
            organMarkings.Remove(layer);

        if (string.IsNullOrEmpty(style))
        {
            _visualBody.ApplyMarkings(ent, applied);
            return;
        }

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

        if (!ProtoMan.TryIndex<MarkingPrototype>(style, out var markingPrototype))
            return;

        if (markingPrototype.GroupWhitelist != null && markingPrototype.GroupWhitelist.Count > 0)
        {
            bool match = markingPrototype.GroupWhitelist.Any(g => g.ToString() == species);
            if (!match)
                return;
        }

        var spriteCount = markingPrototype.Sprites.Count;
        var colorList = new List<Color>(spriteCount);

        for (var i = 0; i < spriteCount; i++)
        {
            uint raw;
            if (colors.Length == 0)
                raw = 0xFFFFFF;
            else if (i < colors.Length)
                raw = colors[i];
            else
                raw = colors[^1];

            colorList.Add(SharedDnaModifierSystem.ToColor(raw));
        }

        foreach (var organMarkings in applied.Values)
        {
            if (!organMarkings.TryGetValue(layer, out var markings))
            {
                markings = new List<Marking>();
                organMarkings[layer] = markings;
            }
            markings.Add(new Marking(markingPrototype.ID, colorList));
        }

        _visualBody.ApplyMarkings(ent, applied);
    }
}
