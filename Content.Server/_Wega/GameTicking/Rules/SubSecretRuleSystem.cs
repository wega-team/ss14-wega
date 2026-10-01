using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.GameTicking.Rules.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Random;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Prototypes;
using Content.Shared.GameTicking.Rules;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Configuration;
using Robust.Shared.Utility;

namespace Content.Server.GameTicking.Rules;

public sealed partial class SubSecretRuleSystem : GameRuleSystem<SubSecretRuleComponent>
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IAdminLogManager _adminLogger = default!;

    protected override void Added(Entity<SubSecretRuleComponent, GameRuleComponent> ent, ref GameRuleAddedEvent args)
    {
        base.Added(ent, ref args);
        var weights = ent.Comp1.Secret;

        if (!TryPickPreset(weights, out var preset))
        {
            Log.Error($"{ToPrettyString(ent.Owner)} failed to pick any preset. Removing rule.");
            Del(ent.Owner);
            return;
        }

        Log.Info($"Selected {preset.ID} as the secret preset.");
        _adminLogger.Add(LogType.EventStarted, $"Selected {preset.ID} as the secret preset.");

        foreach (var rule in preset.Rules)
        {
            if (GameTicker.IsIgnored(rule))
                continue;

            Entity<GameRuleComponent>? ruleEnt;

            // if we're pre-round (i.e. will only be added)
            // then just add rules. if we're added in the middle of the round (or at any other point really)
            // then we want to start them as well
            if (GameTicker.RunLevel <= GameRunLevel.InRound)
                ruleEnt = GameTicker.AddGameRule(rule);
            else
                GameTicker.StartGameRule(rule, out ruleEnt);

            if (ruleEnt == null)
                continue;

            ent.Comp1.AdditionalGameRules.Add(ruleEnt.Value);
        }
    }

    // TODO: We PROBABLY SHOULD NOT BE DOING THIS as the only time Secret ends naturally is end of round which already cleans up these rules.
    // TODO: IN ADDITION We should end secret once it spawns its rules so we don't tick it :V or pause it?
    protected override void Ended(Entity<SubSecretRuleComponent> rule, ref GameRuleEndedEvent args)
    {
        base.Ended(rule, ref args);

        foreach (var gameRule in rule.Comp.AdditionalGameRules)
        {
            GameTicker.EndGameRule(gameRule);
        }
    }

    private bool TryPickPreset(ProtoId<WeightedRandomPrototype> weights, [NotNullWhen(true)] out GamePresetPrototype? preset)
    {
        var options = ProtoMan.Index(weights).Weights.ShallowClone();
        var players = GameTicker.ReadyPlayerCount();

        GamePresetPrototype? selectedPreset = null;
        var sum = options.Values.Sum();
        while (options.Count > 0)
        {
            var accumulated = 0f;
            var rand = _random.NextFloat(sum);
            foreach (var (key, weight) in options)
            {
                accumulated += weight;
                if (accumulated < rand)
                    continue;

                if (!ProtoMan.TryIndex(key, out selectedPreset))
                {
                    Log.Error($"Invalid preset {key} in secret rule weights: {weights}");
                    continue;
                }

                options.Remove(key);
                sum -= weight;
                break;
            }

            if (CanPick(selectedPreset, players))
            {
                preset = selectedPreset;
                return true;
            }

            if (selectedPreset != null)
                Log.Info($"Excluding {selectedPreset.ID} from secret preset selection.");
        }

        preset = null;
        return false;
    }

    /// <summary>
    /// Can any of the given presets be picked, taking into account the currently available player count?
    /// </summary>
    private bool CanPickAny(ProtoId<WeightedRandomPrototype> weightedPresets)
    {
        var ids = ProtoMan.Index(weightedPresets).Weights.Keys
            .Select(x => new ProtoId<GamePresetPrototype>(x));

        return CanPickAny(ids);
    }

    /// <summary>
    /// Can any of the given presets be picked, taking into account the currently available player count?
    /// </summary>
    private bool CanPickAny(IEnumerable<ProtoId<GamePresetPrototype>> protos)
    {
        var players = GameTicker.ReadyPlayerCount();
        foreach (var id in protos)
        {
            if (!ProtoMan.TryIndex(id, out var selectedPreset))
            {
                Log.Error($"Invalid preset {id} in secret rule weights: {id}");
                continue;
            }

            if (CanPick(selectedPreset, players))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Can the given preset be picked, taking into account the currently available player count?
    /// </summary>
    private bool CanPick([NotNullWhen(true)] GamePresetPrototype? selected, int players)
    {
        if (selected == null)
            return false;

        return players >= GameTicker.GetMinimumPlayerCount(selected);
    }
}
