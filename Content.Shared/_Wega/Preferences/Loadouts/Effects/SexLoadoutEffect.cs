using System.Diagnostics.CodeAnalysis;
using Content.Shared.Humanoid;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Shared.Preferences.Loadouts.Effects;

public sealed partial class SexLoadoutEffect : LoadoutEffect
{
    [DataField(required: true)]
    public List<Sex> AllowedSexes = new();

    public override bool Validate(HumanoidCharacterProfile profile, RoleLoadout loadout, LoadoutPrototype proto, ICommonSession? session, IDependencyCollection collection, // Corvax-Sponsors
        [NotNullWhen(false)] out FormattedMessage? reason)
    {
        if (AllowedSexes.Contains(profile.Sex))
        {
            reason = null;
            return true;
        }

        reason = FormattedMessage.FromUnformatted(Loc.GetString("loadout-group-allowed-sex-restriction"));
        return false;
    }
}
