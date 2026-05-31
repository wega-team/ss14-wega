// Developed by Nox project.
// Author: KloopRe

using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;
using Content.Shared._Modifications.Disease.Components;
using Robust.Client.Player;
using Content.Shared._Modifications.Disease;

namespace Content.Client._Modifications.Disease.Systems;

public sealed partial class DiseaseSystem : SharedDiseaseSystem
{
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DiseaseComponent, GetStatusIconsEvent>(GetPatient);
        SubscribeLocalEvent<PrimaryPatientComponent, GetStatusIconsEvent>(GetPrimaryPatient);
    }

    private void GetPatient(Entity<DiseaseComponent> ent, ref GetStatusIconsEvent args)
    {
        if (_player.LocalEntity == ent)
            return;

        if (HasComp<PrimaryPatientComponent>(ent))
            return;

        if (_prototype.TryIndex(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }

    private void GetPrimaryPatient(Entity<PrimaryPatientComponent> ent, ref GetStatusIconsEvent args)
    {
        if (_player.LocalEntity == ent)
            return;

        if (_prototype.TryIndex(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }

}
