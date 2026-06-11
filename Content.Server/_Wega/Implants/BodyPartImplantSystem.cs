using Content.Server.Popups;
using Content.Server.Tools;
using Content.Shared._Wega.Implants.Components;
using Content.Shared.Body;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Tools.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Server.Implants
{
    public sealed partial class BodyPartImplantSystem : EntitySystem
    {
        [Dependency] private ToolSystem _tool = default!;
        [Dependency] private PopupSystem _popup = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<BodyPartImplantComponent, ComponentStartup>(OnStartup);
            SubscribeLocalEvent<BodyPartImplantComponent, ExaminedEvent>(OnExamine);
            SubscribeLocalEvent<BodyPartImplantComponent, InteractUsingEvent>(OnToolUseAttempt);

            SubscribeLocalEvent<BodyComponent, OrganInsertedIntoEvent>(OnOrganInserted);
            SubscribeLocalEvent<BodyComponent, OrganRemovedFromEvent>(OnOrganRemoved);
        }

        private void OnStartup(Entity<BodyPartImplantComponent> ent, ref ComponentStartup args)
        {
            UpdateConfig(ent.Owner, null, ent.Comp);

            // For implants that are organs themselves
            if (TryComp<OrganComponent>(ent, out var organComp) && organComp.Body != null)
            {
                var insertedEvent = new OrganInsertedIntoEvent(ent);
                RaiseLocalEvent(organComp.Body.Value, ref insertedEvent);
            }
        }

        private void OnExamine(EntityUid uid, BodyPartImplantComponent component, ExaminedEvent args)
        {
            if (component.Configurations.Count == 0)
                return;

            var config = component.Configurations.ElementAt(component.CurrentConfig);
            args.PushMarkup(Loc.GetString("body-part-implant-config-" + config.Key));
        }

        private void OnToolUseAttempt(EntityUid uid, BodyPartImplantComponent component, InteractUsingEvent args)
        {
            if (component.Configurations.Count == 0 || !_tool.HasQuality(args.Used, component.ConfigurationTool))
                return;

            component.CurrentConfig++;
            if (component.CurrentConfig >= component.Configurations.Count)
                component.CurrentConfig = 0;

            _tool.PlayToolSound(args.Used, Comp<ToolComponent>(args.Used), null);
            UpdateConfig(uid, args.User, component);
        }

        private void UpdateConfig(EntityUid uid, EntityUid? user, BodyPartImplantComponent component)
        {
            if (component.Configurations.Count == 0)
                return;

            var config = component.Configurations.ElementAt(component.CurrentConfig);

            EntityManager.AddComponents(uid, config.Value);

            if (user != null)
                _popup.PopupEntity(Loc.GetString("body-part-implant-config-" + config.Key), uid, user.Value);
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
