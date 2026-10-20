using Content.Shared.Sharpening.Components;
using Content.Shared.Sharpening.Systems;
using Content.Shared.Sharpening.Events;
using Content.Shared.Popups;
using Content.Server.Popups;
using Content.Shared.Blood.Cult.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;

namespace Content.Server.Sharpening.Systems;

public sealed partial class SharpeningSystem : SharedSharpeningSystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private PopupSystem _popup = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SharpenerComponent, SharpeningFinishedEvent>(OnSharpeningFinished);
        SubscribeLocalEvent<SharpenerComponent, SharpeningFailedEvent>(OnSharpeningFailed);
    }

    private void OnSharpeningFinished(Entity<SharpenerComponent> ent, ref SharpeningFinishedEvent args)
    {
        _audio.PlayPvs(ent.Comp.SharpeningSound, GetEntity(args.Sharpening), AudioParams.Default.WithMaxDistance(2f));

        if (HasComp<BloodSharpenerComponent>(ent))
        {
            var user = GetEntity(args.User);

            Spawn("Ash", Transform(user).Coordinates);

            _popup.PopupEntity(Loc.GetString("blood-sharpener-success"),user, user, PopupType.Small);
        }

        if (!ent.Comp.DeleteOnSharpening)
            return;

        if (ent.Comp.Prototype != string.Empty && TryComp(ent, out TransformComponent? xform))
            Spawn(ent.Comp.Prototype, xform.Coordinates);

        Del(ent);
    }

    private void OnSharpeningFailed(Entity<SharpenerComponent> ent, ref SharpeningFailedEvent args)
    {
        if (!HasComp<BloodSharpenerComponent>(ent))
            return;

        var user = GetEntity(args.User);

        _popup.PopupEntity(Loc.GetString("blood-sharpener-failed"), user, user, PopupType.Small);
    }
}
