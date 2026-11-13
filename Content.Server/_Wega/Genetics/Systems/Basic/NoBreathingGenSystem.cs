using Content.Server.Body.Components;
using Content.Shared.Genetics;

namespace Content.Server.Genetics.System;

public sealed partial class NoBreathingGenSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnInit(Entity<NoBreathingGenComponent> ent, ref ComponentInit args)
    {
        if (HasComp<RespiratorComponent>(ent))
            RemComp<RespiratorComponent>(ent);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<NoBreathingGenComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        EnsureComp<RespiratorComponent>(ent);
    }
}
