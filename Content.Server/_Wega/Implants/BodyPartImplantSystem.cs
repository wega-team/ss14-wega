using Content.Shared._Wega.Implants.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;

namespace Content.Shared._Wega.Implants
{
    public sealed class BodyPartImplantSystem : EntitySystem
    {
        [Dependency] private readonly SharedBodySystem _body = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<BodyComponent, BodyPartAddedEvent>(OnPartAdded);
            SubscribeLocalEvent<BodyComponent, BodyPartRemovedEvent>(OnPartRemove);
        }

        private void OnPartAdded(EntityUid uid, BodyComponent component, ref BodyPartAddedEvent args)
        {
            if (!TryComp<BodyPartImplantComponent>(args.Part.Owner, out var implant))
                return;

            EntityManager.AddComponents(uid, implant.ImplantComponents);
        }

        private void OnPartRemove(EntityUid uid, BodyComponent component, ref BodyPartRemovedEvent args)
        {
            if (!TryComp<BodyPartImplantComponent>(args.Part, out var implant))
                return;

            if (!HasParts(uid, component, implant.ImplantKey))
                EntityManager.RemoveComponents(uid, implant.ImplantComponents);
        }

        private bool HasParts(EntityUid uid, BodyComponent component, string? key)
        {
            if (key == null)
                return false;

            var slots = _body.GetBodyContainers(uid, component);
            foreach (var slot in slots)
            {
                if (!TryComp<BodyPartImplantComponent>(slot.ContainedEntities[0], out var implant))
                    continue;

                if (implant.ImplantKey == key)
                    return true;
            }

            return false;
        }
    }
}
