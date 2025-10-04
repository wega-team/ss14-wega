using Content.Shared.Clothing.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.Genetics;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Clothing;

public sealed class TearableClothingSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private static readonly ProtoId<DamageTypePrototype> Damage = "Slash";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TearableClothingComponent, GetVerbsEvent<AlternativeVerb>>(AddTearVerb);
        SubscribeLocalEvent<TearableClothingComponent, TearClothingDoAfterEvent>(OnDoAfter);
    }

    private void AddTearVerb(Entity<TearableClothingComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {
        var user = args.User;
        if (!HasComp<ClothingComponent>(entity) || !HasComp<DamageableComponent>(entity))
            return;

        var text = Loc.GetString("tearable-clothing-verb-tear");

        AlternativeVerb verb = new()
        {
            Text = text,
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/zap.svg.192dpi.png")),
            Act = () => StartTearing(user, entity),
            Priority = 1
        };
        args.Verbs.Add(verb);
    }

    public void StartTearing(EntityUid user, Entity<TearableClothingComponent> entity)
    {
        if (!TryComp<PhysicsComponent>(user, out var physics) || physics.Mass <= 60f
            && !HasComp<StrongnessGenComponent>(user))
        {
            _popup.PopupClient(Loc.GetString("tearable-clothing-too-weakness"), user, user);
            return;
        }

        var multiplier = HasComp<StrongnessGenComponent>(user) ? 2f : 1f;
        var args = new DoAfterArgs(EntityManager, user, TimeSpan.FromSeconds(entity.Comp.Delay) / multiplier,
            new TearClothingDoAfterEvent(), entity, entity)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true
        };

        _popup.PopupCoordinates(Loc.GetString("tearable-clothing-try-tear",
            ("user", Identity.Name(user, EntityManager)), ("clothing", Name(entity))),
            Transform(user).Coordinates, type: PopupType.Medium);

        _doAfter.TryStartDoAfter(args);
    }

    private void OnDoAfter(Entity<TearableClothingComponent> entity, ref TearClothingDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target == null)
            return;

        args.Handled = true;

        _popup.PopupClient(Loc.GetString("tearable-clothing-successed", ("clothing", Name(entity))), args.User, args.User);

        var damageSpec = new DamageSpecifier { DamageDict = { { Damage, 60 } } };
        _damage.TryChangeDamage(args.Args.Target.Value, damageSpec, true);
    }
}
