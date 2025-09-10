using Content.Shared.Database;
using Content.Shared.Mobs.Components;
using Content.Shared.Verbs;
using Content.Shared._Wega.Implants.Components;

using Robust.Shared.Utility;
using Content.Server.Medical;

namespace Content.Server._Wega.Implants;

public sealed class DefibImplantSystem : EntitySystem
{
    [Dependency] private readonly DefibrillatorSystem _defib = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MobStateComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerbs);
    }

    private void OnGetAltVerbs(EntityUid uid, MobStateComponent component, ref GetVerbsEvent<AlternativeVerb> args)
    {
        var user = args.User;

        if (!TryComp<DefibImplantComponent>(user, out var implant))
            return;

        if (!args.CanAccess || !args.CanInteract || !args.Using.HasValue)
            return;

        AlternativeVerb verb = new()
        {
            Text = Loc.GetString("defib-implant-verb-zap"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/zap.svg.192dpi.png")),
            Act = () => _defib.TryStartZap(user, uid, user),
            Impact = LogImpact.Medium
        };
        args.Verbs.Add(verb);
    }
}
