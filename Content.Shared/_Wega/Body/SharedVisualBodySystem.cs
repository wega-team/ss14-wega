using Content.Shared._Wega.Implants.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Body;

public abstract partial class SharedVisualBodySystem
{
    [SubscribeLocalEvent]
    private void OnBodyPartImplantAdded(Entity<VisualBodyComponent> ent, ref BodyPartImplantAddedEvent args)
    {
        if (!TryComp<OrganComponent>(args.Part.Owner, out var organComp)
            || organComp.Category is not { } category)
            return;

        if (!_container.TryGetContainer(ent, BodyComponent.ContainerID, out var container))
            return;

        foreach (var organ in container.ContainedEntities)
        {
            if (TryComp<VisualOrganComponent>(organ, out var visualOrgan))
            {
                var profiles = new Dictionary<ProtoId<OrganCategoryPrototype>, OrganProfileData>
                {
                    { category, visualOrgan.Profile }
                };
                var evt = new ApplyOrganProfileDataEvent(null, profiles);
                RaiseLocalEvent(ent, ref evt);
                return;
            }
        }
    }
}
