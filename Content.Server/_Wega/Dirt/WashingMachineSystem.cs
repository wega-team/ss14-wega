using System.Linq;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Audio;
using Content.Shared.Clothing.Components;
using Content.Shared.DirtVisuals;
using Content.Shared.Jittering;
using Content.Shared.Lock;
using Content.Shared.Power;
using Content.Shared.Storage.Components;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Server.WashingMachine
{
    public sealed class WashingMachineSystem : EntitySystem
    {
        [Dependency] private readonly SharedAmbientSoundSystem _ambient = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedContainerSystem _container = default!;
        [Dependency] private readonly SharedDirtSystem _dirt = default!;
        [Dependency] private readonly SharedJitteringSystem _jittering = default!;
        [Dependency] private readonly LockSystem _lock = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<WashingMachineComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<WashingMachineComponent, GetVerbsEvent<AlternativeVerb>>(AddWashVerb);
            SubscribeLocalEvent<WashingMachineComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<WashingMachineComponent, ContainerIsInsertingAttemptEvent>(OnInsertAttempt);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<WashingMachineComponent, ApcPowerReceiverComponent>();
            while (query.MoveNext(out var uid, out var wash, out var power))
            {
                if (!wash.IsWashing || !power.Powered)
                    continue;

                wash.RemainingTime -= frameTime;
                if (wash.RemainingTime <= 0)
                    FinishWashing(uid, wash);
            }
        }

        private void OnInit(EntityUid uid, WashingMachineComponent component, ComponentInit args)
            => _container.EnsureContainer<Container>(uid, "entity_storage");

        private void AddWashVerb(EntityUid uid, WashingMachineComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract || !this.IsPowered(uid, EntityManager) || component.IsWashing)
                return;

            if (!TryComp<EntityStorageComponent>(uid, out var storage) || storage.Open)
                return;

            var container = _container.GetContainer(uid, "entity_storage");
            if (container.ContainedEntities.Count == 0)
                return;

            AlternativeVerb verb = new()
            {
                Act = () => StartWashing(uid, component),
                Text = Loc.GetString("washing-machine-verb-start"),
                Priority = 2
            };

            args.Verbs.Add(verb);
        }

        private void OnPowerChanged(EntityUid uid, WashingMachineComponent component, ref PowerChangedEvent args)
        {
            if (!args.Powered && component.IsWashing)
                StopWashing(uid, component);
        }

        private void OnInsertAttempt(EntityUid uid, WashingMachineComponent component, ContainerIsInsertingAttemptEvent args)
        {
            if (component.IsWashing)
                args.Cancel();
        }

        public void StartWashing(EntityUid uid, WashingMachineComponent component)
        {
            if (component.IsWashing)
                return;

            if (!TryComp<EntityStorageComponent>(uid, out var storage) || storage.Open)
                return;

            var container = _container.GetContainer(uid, "entity_storage");
            if (container.ContainedEntities.Count == 0)
                return;

            component.IsWashing = true;
            component.RemainingTime = component.WashTime;
            _lock.Lock(uid, null);

            _jittering.AddJitter(uid, -10, 100);
            _ambient.SetAmbience(uid, true);

            _appearance.SetData(uid, WashingMachineVisuals.IsWashing, true);
        }

        private void StopWashing(EntityUid uid, WashingMachineComponent component)
        {
            component.IsWashing = false;

            _lock.Unlock(uid, null);
            _ambient.SetAmbience(uid, false);

            _appearance.SetData(uid, WashingMachineVisuals.IsWashing, false);

            RemCompDeferred<JitteringComponent>(uid);
        }

        private void FinishWashing(EntityUid uid, WashingMachineComponent component)
        {
            var container = _container.GetContainer(uid, "entity_storage");
            foreach (var entity in container.ContainedEntities.ToList())
            {
                if (HasComp<DirtableComponent>(entity))
                    _dirt.CleanDirt(entity, 100f);

                // This is to clean the switchable clothes
                var attachedClothing = new List<EntityUid>();
                var attachedClothingQuery = EntityQueryEnumerator<AttachedClothingComponent>();
                while (attachedClothingQuery.MoveNext(out var clothingUid, out var ac))
                {
                    if (ac.AttachedUid == entity)
                        attachedClothing.Add(clothingUid);
                }

                foreach (var clothing in attachedClothing)
                {
                    if (HasComp<DirtableComponent>(clothing))
                        _dirt.CleanDirt(clothing, 100f);
                }
            }

            _audio.PlayPvs(component.FinishSound, uid);
            StopWashing(uid, component);
        }
    }
}
