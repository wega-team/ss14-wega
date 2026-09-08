using Content.Server.Wega.MetaData.Components;
using Content.Server.Administration;
using Content.Server.Charges;
using Content.Server.Popups;
using Content.Shared.Verbs;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Server.Wega.MetaData.Systems;

public sealed partial class RedescribeableSystem : EntitySystem
{
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private QuickDialogSystem _quickDialog = default!;
    [Dependency] private IPlayerManager _playMan = default!;
    [Dependency] private PopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RedescribeOnInteractComponent, GetVerbsEvent<InteractionVerb>>(OnGetVerbs);
    }

    public bool TryRedescribe(Entity<RedescribeOnInteractComponent?, MetaDataComponent?> entity, string newName, bool raiseEvents = true)
    {
        if (!Resolve(entity, ref entity.Comp1, ref entity.Comp2, false))
            return false;

        var name = FormatNewName(newName);

        if (!IsNewNameValid(name, entity.Comp1))
            return false;

        if (entity.Comp1.IsDescIn)
		{
			var newDesc = Loc.GetString(entity.Comp1.DescIn, ("desc", name));
			_metaData.SetEntityDescription(entity, newDesc, entity.Comp2);
		}
		else
			_metaData.SetEntityDescription(entity, name, entity.Comp2);
		
        if (entity.Comp1.Namenaid && entity.Comp1.User != null)
		{
			var val = Loc.GetString(entity.Comp1.NameIn, ("owner", entity.Comp1.User));
			_metaData.SetEntityName(entity, val);
		}


		entity.Comp1.Used = true;
		
        return true;
    }

    public bool IsNewNameValid(string str, RedescribeOnInteractComponent comp)
    {
        if (str.Length > comp.MaxLength)
            return false;

        if (string.IsNullOrWhiteSpace(str))
            return false;

        return true;
    }

    // TODO: тоже заполнить компонент уточняющими свойствами. деспэйр
    public string FormatNewName(string str)
    {
        return str;
    }

    public bool TryOpenDialog(ICommonSession session, Entity<RedescribeOnInteractComponent?> item)
    {
        if (session.AttachedEntity == null)
            return false;

        if (!Resolve(item, ref item.Comp, false))
            return false;


        var titleLoc = Loc.GetString(item.Comp.RedescribeActionLocString);
        var promptLoc = Loc.GetString(item.Comp.NameTitleLocString);

        _quickDialog.OpenDialog(session, titleLoc, promptLoc, (string newName) =>
        {
            if (!IsNewNameValid(newName, item.Comp))
            {
                _popup.PopupCursor(Loc.GetString(item.Comp.NewNameConditions, ("count", item.Comp.MaxLength)), session, Shared.Popups.PopupType.Medium);
                return;
            }

            TryRedescribe(item, newName, true);
        }, null);

        return true;
    }

    private void OnGetVerbs(EntityUid item, RedescribeOnInteractComponent comp, GetVerbsEvent<InteractionVerb> ev)
    {
        if (!ev.CanAccess || !ev.CanInteract || !ev.CanComplexInteract)
            return;

        if (!comp.UseVerbs)
            return;

        if (comp.Used)
            return;

		comp.User = ev.User;

        var verb = new InteractionVerb()
        {
            Act = () =>
            {
                var user = ev.User;
                if (!_playMan.TryGetSessionByEntity(user, out var session))
                    return;

                TryOpenDialog(session, item);
            },
            Impact = Shared.Database.LogImpact.Low,
            Text = Loc.GetString(comp.RedescribeActionLocString),
            Icon = new SpriteSpecifier.Texture(comp.VerbTexturePath),
            Priority = 10,
        };

        ev.Verbs.Add(verb);
    }
}