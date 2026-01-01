using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Blood.Cult.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Stunnable;

namespace Content.Shared.Blood.Cult;

public abstract class SharedBloodCultSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    #region Deconvertation
    public void CultistDeconvertation(EntityUid cultist)
    {
        if (!TryComp<BloodCultistComponent>(cultist, out var bloodCultist))
            return;

        if (TryComp<ActionsContainerComponent>(cultist, out var actionsContainer))
        {
            foreach (var actionId in actionsContainer.Container.ContainedEntities.ToArray())
            {
                if (!TryComp(actionId, out MetaDataComponent? meta))
                    continue;

                var protoId = meta.EntityPrototype?.ID;
                if (protoId == BloodCultistComponent.BloodMagic.Id
                    || protoId == BloodCultistComponent.RecallBloodDagger.Id)
                {
                    _action.RemoveAction(cultist, actionId);
                }
            }
        }

        _action.RemoveAction(cultist, bloodCultist.RecallSpearActionEntity);
        _action.RemoveAction(cultist, bloodCultist.SelectedSpell);

        foreach (var spell in bloodCultist.SelectedEmpoweringSpells)
            _action.RemoveAction(cultist, spell);

        _stun.TryKnockdown(cultist, TimeSpan.FromSeconds(4), true);
        _popup.PopupEntity(Loc.GetString("blood-cult-break-control", ("name", Identity.Entity(cultist, EntityManager))), cultist);

        RemComp<BloodCultistComponent>(cultist);
        RemComp<BloodCultistEyesComponent>(cultist);
        RemComp<BloodPentagramDisplayComponent>(cultist);
    }
    #endregion
}
