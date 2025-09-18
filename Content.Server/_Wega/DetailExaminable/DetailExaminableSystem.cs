using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Verbs;
using Content.Shared.Humanoid;
using Robust.Shared.Utility;
using Content.Server.EUI;
using Robust.Shared.Player;
using Content.Server.Mind;
using Content.Shared.DetailExaminable;

namespace Content.Server.DetailExaminable;

public sealed partial class DetailExaminableSystem : EntitySystem
{
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly EuiManager _euiMan = default!;
    [Dependency] private readonly MindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DetailExaminableComponent, GetVerbsEvent<ExamineVerb>>(OnGetExamineVerbs);
    }

    private void OnGetExamineVerbs(Entity<DetailExaminableComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (Identity.Name(args.Target, EntityManager) != MetaData(args.Target).EntityName)
            return;

        var detailsRange = _examine.IsInDetailsRange(args.User, ent);

        var user = args.User;

        var verb = new ExamineVerb
        {
            Act = () => OpenEui(user, ent.Owner),
            Text = Loc.GetString("detail-examinable-verb-text"),
            Category = VerbCategory.Examine,
            Disabled = !detailsRange,
            Message = detailsRange ? null : Loc.GetString("detail-examinable-verb-disabled"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/examine.svg.192dpi.png"))
        };

        args.Verbs.Add(verb);
    }

    private void OpenEui(EntityUid user, EntityUid target)
    {
        if (!TryComp<DetailExaminableComponent>(target, out var detail)
            || !TryComp<HumanoidAppearanceComponent>(target, out var humanoid))
            return;

        if (_mind.TryGetMind(user, out _, out var mind)
            && mind is { UserId: not null } && _player.TryGetSessionById(mind.UserId, out var session))
        {
            var nsfw = humanoid.Status == Status.No
                || TryComp<HumanoidAppearanceComponent>(user, out var appearance) && appearance.Status == Status.No
                ? string.Empty
                : detail.NSFWContent;

            var state = new DetailExaminableEuiState(
                GetNetEntity(target),
                Identity.Name(target, EntityManager),
                humanoid.Species.Id,
                humanoid.Sex,
                humanoid.Gender,
                humanoid.Status,
                detail.Content,
                detail.OOCContent,
                detail.CharacterContent,
                detail.GreenContent,
                detail.YellowContent,
                detail.RedContent,
                detail.TagsContent,
                detail.LinksContent,
                nsfw
            );

            var window = new DetailExaminableEui(state);
            _euiMan.OpenEui(window, session);
            window.StateDirty();
        }
    }
}
