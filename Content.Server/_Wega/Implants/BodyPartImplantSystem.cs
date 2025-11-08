using Content.Server.Body.Systems;
using Content.Shared._Wega.Implants.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;

namespace Content.Server.Implants
{
    public sealed class BodyPartImplantSystem : EntitySystem
    {
        [Dependency] private readonly BodySystem _body = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<BodyPartImplantComponent, MapInitEvent>(OnMapInit);

            SubscribeLocalEvent<BodyComponent, BodyPartAddedEvent>(OnPartAdded);
            SubscribeLocalEvent<BodyComponent, BodyPartRemovedEvent>(OnPartRemove);
        }

        private void OnMapInit(EntityUid uid, BodyPartImplantComponent component, ref MapInitEvent args)
        {
            if (!TryComp<BodyPartComponent>(uid, out _))
                return;

            foreach (var connection in component.Connections)
            {
                _body.TryCreatePartSlot(uid, connection.Key, connection.Value, out _);
            }
        }

        private void OnPartAdded(EntityUid uid, BodyComponent component, ref BodyPartAddedEvent args)
        {
            if (!TryComp<BodyPartImplantComponent>(args.Part.Owner, out var implant) || implant.ImplantComponents == null)
                return;

            EntityManager.AddComponents(uid, implant.ImplantComponents);

            var ev = new BodyPartImplantAddedEvent(args.Slot, args.Part.Owner);
            RaiseLocalEvent(uid, ref ev);
        }

        private void OnPartRemove(EntityUid uid, BodyComponent component, ref BodyPartRemovedEvent args)
        {
            if (!TryComp<BodyPartImplantComponent>(args.Part, out var implant) || implant.ImplantComponents == null)
                return;

            if (!HasParts(uid, component, implant.ImplantKey))
                EntityManager.RemoveComponents(uid, implant.ImplantComponents);

            var ev = new BodyPartImplantRemovedEvent(args.Slot, args.Part.Owner);
            RaiseLocalEvent(uid, ref ev);
        }

        private bool HasParts(EntityUid uid, BodyComponent component, string? key)
        {
            if (key == null)
                return false;

            var slots = _body.GetBodyContainers(uid, component);
            foreach (var slot in slots)
            {
                if (slot.ContainedEntities.Count == 0 || !TryComp<BodyPartImplantComponent>(slot.ContainedEntities[0], out var implant))
                    continue;

                if (implant.ImplantKey == key)
                    return true;
            }

            return false;
        }
    }
}
