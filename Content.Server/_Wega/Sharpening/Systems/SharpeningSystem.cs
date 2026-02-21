using Content.Shared.Sharpening.Components;
using Content.Shared.Sharpening.Systems;
using Content.Shared.Sharpening.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;

namespace Content.Server.Sharpening.Systems;

public sealed class SharpeningSystem : SharedSharpeningSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SharpenerComponent, SharpeningFinishedEvent>(OnSharpeningFinished);
    }

    private void OnSharpeningFinished(Entity<SharpenerComponent> ent, ref SharpeningFinishedEvent args)
    {
        _audio.PlayPvs(ent.Comp.SharpeningSound, GetEntity(args.Sharpening), AudioParams.Default.WithMaxDistance(2f));

        if (!ent.Comp.DeleteOnSharpening)
            return;

        if (ent.Comp.Prototype != string.Empty && TryComp(ent, out TransformComponent? xform))
            Spawn(ent.Comp.Prototype, xform.Coordinates);

        Del(ent);
    }
}
