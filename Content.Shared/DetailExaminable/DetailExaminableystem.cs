using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Verbs;
using Content.Shared.Humanoid; // Corvax-Wega
using Robust.Shared.Utility;

namespace Content.Shared.DetailExaminable;

public sealed class DetailExaminableSystem : EntitySystem
{
    [Dependency] private readonly ExamineSystemShared _examine = default!;

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

        // Corvax-Wega-start
        var appearanceComponent = EntityManager.TryGetComponent<HumanoidAppearanceComponent>(args.Target, out var humanoid)
            ? humanoid
            : null;

        var statusText = appearanceComponent != null
            ? GetStatusText(appearanceComponent.Status)
            : string.Empty;
        // Corvax-Wega-end

        var verb = new ExamineVerb
        {
            Act = () =>
            {
                var markup = new FormattedMessage();
                markup.AddMarkupPermissive(ent.Comp.Content);
                markup.AddMarkupOrThrow(statusText); // Corvax-Wega
                _examine.SendExamineTooltip(user, ent, markup, false, false);
            },
            Text = Loc.GetString("detail-examinable-verb-text"),
            Category = VerbCategory.Examine,
            Disabled = !detailsRange,
            Message = detailsRange ? null : Loc.GetString("detail-examinable-verb-disabled"),
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/examine.svg.192dpi.png"))
        };

        args.Verbs.Add(verb);
    }

    // Corvax-Wega-start
    private string GetStatusText(Status status)
    {
        return status switch
        {
            Status.No => "\n" + ($"[color=red]{Loc.GetString("humanoid-profile-editor-status-no-text")}[/color]"),
            Status.Semi => "\n" + ($"[color=orange]{Loc.GetString("humanoid-profile-editor-status-semi-text")}[/color]"),
            Status.Full => "\n" + ($"[color=blue]{Loc.GetString("humanoid-profile-editor-status-full-text")}[/color]"),
            Status.Absolute => "\n" + ($"[color=purple]{Loc.GetString("humanoid-profile-editor-status-absolute-text")}[/color]"),
            _ => string.Empty,
        };
    }
    // Corvax-Wega-end
}
