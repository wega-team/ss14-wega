using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Veil.Cult.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.Stunnable;

namespace Content.Shared.Veil.Cult;

public abstract class SharedVeilCultSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    #region Deconvertation
    public void CultistDeconvertation(EntityUid cultist)
    {
        if (!TryComp<VeilCultistComponent>(cultist, out var veilCultist))
            return;

        if (TryComp<ActionsContainerComponent>(cultist, out var actionsContainer))
        {
            foreach (var actionId in actionsContainer.Container.ContainedEntities.ToArray())
            {
                if (!TryComp(actionId, out MetaDataComponent? meta))
                    continue;

                var protoId = meta.EntityPrototype?.ID;
                if (protoId == VeilCultistComponent.MidasTouch.Id)
                {
                    _action.RemoveAction(cultist, actionId);
                }
            }
        }


        if (TryComp<MindLinkComponent>(cultist, out var mindLink))
        {
            mindLink.Channels.Remove(veilCultist.CultMindChannel);
            if (mindLink.Channels.Count == 0)
                RemComp(cultist, mindLink);
        }

        _stun.TryKnockdown(cultist, TimeSpan.FromSeconds(4), true);
        _popup.PopupEntity(Loc.GetString("veil-cult-break-control", ("name", Identity.Entity(cultist, EntityManager))), cultist);

        RemComp<VeilCultistComponent>(cultist);
        RemComp<VeilCultistHandsComponent>(cultist);
        RemComp<VeilCogDisplayComponent>(cultist);
    }
    #endregion
}