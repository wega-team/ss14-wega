using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Achievements;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Preferences.Loadouts.Effects;

public sealed partial class AchievementLoadoutEffect : LoadoutEffect
{
    [DataField(required: true)]
    public List<AchievementsEnum> RequiredAchievements = new();

    [DataField]
    public bool RequireAll = false;

    public override bool Validate(
        HumanoidCharacterProfile profile,
        RoleLoadout loadout,
        LoadoutPrototype proto,
        ICommonSession? session,
        IDependencyCollection collection,
        [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = null;

        if (session == null)
            return true;

        if (RequiredAchievements.Count == 0)
            return true;

        // I think you're trying to break this logic by uploading a profile, but as I understand it,
        // The client-side upload is validated, so it's unlikely to succeed.◝(ᵔᵕᵔ)◜
        var net = collection.Resolve<INetManager>();
        if (net.IsServer)
            return true;

        var achievementSystem = collection.Resolve<IEntitySystemManager>().GetEntitySystem<SharedAchievementsSystem>();
        var unlockedAchievements = achievementSystem.GetUnlockedAchievements();

        bool hasRequired = RequireAll
            ? RequiredAchievements.All(ach => unlockedAchievements.Contains(ach))
            : RequiredAchievements.Any(ach => unlockedAchievements.Contains(ach));

        if (hasRequired)
            return true;

        var protoManager = collection.Resolve<IPrototypeManager>();
        var achievementList = string.Join("\n", RequiredAchievements.Select(ach =>
        {
            var prototype = protoManager.EnumeratePrototypes<AchievementPrototype>()
                .FirstOrDefault(p => p.Key == ach);
            var name = prototype != null ? Loc.GetString(prototype.Name)
                : Loc.GetString($"achievements-{ach.ToString().ToLower()}-name"); // Trying format
            return "• " + name;
        }));

        var locKey = RequireAll ? "loadout-group-achievement-restriction-all" : "loadout-group-achievement-restriction-any";
        reason = FormattedMessage.FromUnformatted(Loc.GetString(locKey, ("achievements", achievementList)));
        return false;
    }
}
