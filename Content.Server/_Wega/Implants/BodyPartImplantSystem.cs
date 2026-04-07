using Content.Shared._Wega.Implants.Components;
using Content.Shared.Body;
using Robust.Shared.Containers;

namespace Content.Server.Implants
{
    public sealed class BodyPartImplantSystem : EntitySystem
    {
        [Dependency] private readonly SharedContainerSystem _container = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<BodyPartImplantComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<BodyPartImplantComponent, ComponentStartup>(OnStartup);

            SubscribeLocalEvent<BodyComponent, OrganInsertedIntoEvent>(OnOrganInserted);
            SubscribeLocalEvent<BodyComponent, OrganRemovedFromEvent>(OnOrganRemoved);
        }

        private void OnStartup(Entity<BodyPartImplantComponent> ent, ref ComponentStartup args)
        {
            // For implants that are organs themselves
            if (TryComp<OrganComponent>(ent, out var organComp) && organComp.Body != null)
            {
                var insertedEvent = new OrganInsertedIntoEvent(ent);
                RaiseLocalEvent(organComp.Body.Value, ref insertedEvent);
            }
        }

        private void OnMapInit(Entity<BodyPartImplantComponent> ent, ref MapInitEvent args)
        {
            if (!HasComp<OrganComponent>(ent))
                return;

            // This implant is an organ with slots for other organs
            foreach (var connection in ent.Comp.Connections)
            {
                // Create container for child organs
                var containerId = $"{ent.Owner}-{connection.Key}";
                if (!_container.TryGetContainer(ent, containerId, out _))
                {
                    _container.EnsureContainer<Container>(ent, containerId);
                }
            }

            // Spawn child organs
            foreach (var (slot, organProto) in ent.Comp.Parts)
            {
                var childOrgan = Spawn(organProto, Transform(ent).Coordinates);
                var containerId = $"{ent.Owner}-{slot}";

                if (_container.TryGetContainer(ent, containerId, out var container))
                {
                    _container.Insert(childOrgan, container);
                }
                else
                {
                    container = _container.EnsureContainer<Container>(ent, containerId);
                    _container.Insert(childOrgan, container);
                }
            }
        }

        private void OnOrganInserted(Entity<BodyComponent> body, ref OrganInsertedIntoEvent args)
        {
            if (!TryComp<BodyPartImplantComponent>(args.Organ, out var implant))
                return;

            // Apply components to the body when implant is inserted
            if (implant.ImplantComponents != null)
            {
                EntityManager.AddComponents(body.Owner, implant.ImplantComponents);
            }

            var ev = new BodyPartImplantAddedEvent((args.Organ, implant));
            RaiseLocalEvent(body, ref ev);
        }

        private void OnOrganRemoved(Entity<BodyComponent> body, ref OrganRemovedFromEvent args)
        {
            if (!TryComp<BodyPartImplantComponent>(args.Organ, out var implant))
                return;

            // Remove components from the body if no other implants with same key exist
            if (implant.ImplantComponents != null && !HasImplantWithKey(body.Owner, implant.ImplantKey))
            {
                EntityManager.RemoveComponents(body.Owner, implant.ImplantComponents);
            }

            var ev = new BodyPartImplantRemovedEvent((args.Organ, implant));
            RaiseLocalEvent(body, ref ev);
        }

        private bool HasImplantWithKey(EntityUid bodyUid, string? key)
        {
            if (key == null)
                return false;

            if (!TryComp<BodyComponent>(bodyUid, out var body) || body.Organs == null)
                return false;

            // Check all organs in the body
            foreach (var organ in body.Organs.ContainedEntities)
            {
                if (organ == bodyUid) // Skip self
                    continue;

                if (TryComp<BodyPartImplantComponent>(organ, out var implant) && implant.ImplantKey == key)
                    return true;
            }

            return false;
        }
    }
}
