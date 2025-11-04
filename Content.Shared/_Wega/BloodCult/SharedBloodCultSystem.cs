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
                if (protoId == BloodCultistComponent.CultObjective.Id
                    || protoId == BloodCultistComponent.CultCommunication.Id
                    || protoId == BloodCultistComponent.BloodMagic.Id
                    || protoId == BloodCultistComponent.RecallBloodDagger.Id)
                {
                    _action.RemoveAction(cultist, actionId);
                }
            }
        }

        if (bloodCultist.RecallSpearActionEntity != null)
            _action.RemoveAction(cultist, bloodCultist.RecallSpearActionEntity);

        if (bloodCultist.SelectedSpell != null)
            _action.RemoveAction(cultist, bloodCultist.SelectedSpell.Value);

        foreach (var spell in bloodCultist.SelectedEmpoweringSpells)
        {
            if (spell != null)
            {
                _action.RemoveAction(cultist, spell.Value);
            }
        }

        var stunTime = TimeSpan.FromSeconds(4);
        var name = Identity.Entity(cultist, EntityManager);

        _stun.TryKnockdown(cultist, stunTime, true);
        _popup.PopupEntity(Loc.GetString("blood-cult-break-control", ("name", name)), cultist);

        RemComp<BloodCultistComponent>(cultist);
        if (HasComp<CultistEyesComponent>(cultist)) RemComp<CultistEyesComponent>(cultist);
        if (HasComp<PentagramDisplayComponent>(cultist)) RemComp<PentagramDisplayComponent>(cultist);
    }
    #endregion
}