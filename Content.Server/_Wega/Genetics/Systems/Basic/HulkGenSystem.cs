using Content.Server.Destructible;
using Content.Server.Polymorph.Systems;
using Content.Shared.Actions;
using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Genetics;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Stunnable;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Genetics.System;

public sealed partial class HulkGenSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _action = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private DnaModifierSystem _dnaModifier = default!;
    [Dependency] private PhysicsSystem _physics = default!;
    [Dependency] private PolymorphSystem _polymorph = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedStunSystem _stun = default!;

    private static readonly ProtoId<StructuralEnzymesPrototype> HulkGen = "GeneticsHulkBasic";

    [SubscribeLocalEvent]
    private void OnInit(Entity<HulkGenComponent> ent, ref ComponentInit args)
        => ent.Comp.ActionEntity = _action.AddAction(ent, ent.Comp.ActionPrototype);

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<HulkGenComponent> ent, ref ComponentShutdown args)
        => _action.RemoveAction(ent.Comp.ActionEntity);

    [SubscribeLocalEvent]
    private void OnTransformation(Entity<HulkGenComponent> ent, ref HulkTransformationActionEvent args)
    {
        args.Handled = true;

        if (!TryComp<HumanoidProfileComponent>(ent, out var humanoid))
            return;

        _dnaModifier.TrySetEnzymeHexCode(ent.Owner, HulkGen.Id, 0x333);
        var polymorph = CheckSpeciesEntity(humanoid)
            ? ent.Comp.PolymorphAltProto
            : ent.Comp.PolymorphProto;

        _polymorph.PolymorphEntity(ent, polymorph);
    }

    private bool CheckSpeciesEntity(HumanoidProfileComponent humanoid)
    {
        var altSpecies = new[]
        {
            new ProtoId<SpeciesPrototype>("Reptilian"),
            new ProtoId<SpeciesPrototype>("Feroxi"),
            new ProtoId<SpeciesPrototype>("Resomi"),
            new ProtoId<SpeciesPrototype>("Vox")
        };
        return altSpecies.Contains(humanoid.Species);
    }

    #region Abilities
    [SubscribeLocalEvent]
    private void OnHulkInit(Entity<HulkComponent> ent, ref ComponentInit args)
    {
        foreach (var action in ent.Comp.ActionPrototypes)
            ent.Comp.ActionsEntity.Add(_action.AddAction(ent, action));
    }

    [SubscribeLocalEvent]
    private void OnHulkShutdown(Entity<HulkComponent> ent, ref ComponentShutdown args)
    {
        foreach (var action in ent.Comp.ActionsEntity)
            _action.RemoveAction(action);
    }

    [SubscribeLocalEvent]
    private void OnHulkCharge(Entity<HulkComponent> entity, ref HulkChargeActionEvent args)
    {
        var target = args.Target;
        var vampirePosition = _transform.GetWorldPosition(entity);
        var targetPosition = _transform.ToMapCoordinates(Transform(target).Coordinates, true).Position;
        var direction = (targetPosition - vampirePosition).Normalized();

        if (TryComp(entity, out PhysicsComponent? vampirePhysics))
            _physics.ApplyLinearImpulse(entity, direction * 20000f, body: vampirePhysics);

        if (TryComp(target, out DestructibleComponent? _))
        {
            var damage = new DamageSpecifier { DamageDict = { { "Structural", 300 } } };
            _damage.TryChangeDamage(target, damage, origin: entity);
        }

        if (TryComp(target, out BodyComponent? _))
        {
            var damage = new DamageSpecifier { DamageDict = { { "Blunt", 60 } } };
            _damage.TryChangeDamage(target, damage, ignoreResistances: false, origin: entity);

            if (TryComp(target, out PhysicsComponent? physics))
                _physics.ApplyLinearImpulse(target, direction * 1000f, body: physics);

            _stun.TryUpdateParalyzeDuration(target, TimeSpan.FromSeconds(10f));
        }

        _audio.PlayPvs(args.Sound, entity);
        args.Handled = true;
    }
    #endregion
}
