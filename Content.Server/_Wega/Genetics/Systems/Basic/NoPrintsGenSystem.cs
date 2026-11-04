using Content.Shared.Forensics.Components;
using Content.Shared.Genetics;

namespace Content.Server.Genetics.System;

public sealed partial class NoPrintsGenSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnInit(Entity<NoPrintsGenComponent> ent, ref ComponentInit args)
    {
        if (TryComp<FingerprintComponent>(ent, out var fingerprint))
        {
            if (fingerprint.Fingerprint is { } prints)
                ent.Comp.OldPrints = prints;

            RemComp<FingerprintComponent>(ent);
        }
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<NoPrintsGenComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        EnsureComp<FingerprintComponent>(ent).Fingerprint = ent.Comp.OldPrints;
    }
}
