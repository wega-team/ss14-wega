using Content.Shared.Actions;
using Content.Shared.Genetics;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;

namespace Content.Server.Genetics.System;

public sealed partial class CloakOfDarknessGenSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _action = default!;
    [Dependency] private SharedStealthSystem _stealth = default!;

    [SubscribeLocalEvent]
    private void OnInit(Entity<CloakOfDarknessGenComponent> ent, ref ComponentInit args)
    {
        ent.Comp.CloakOfDarknessActionEntity = _action.AddAction(ent, ent.Comp.CloakOfDarknessAction);

        if (HasComp<ChameleonGenComponent>(ent))
        {
            RemComp<StealthOnMoveComponent>(ent);
            if (TryComp<StealthComponent>(ent, out var oldStealth))
            {
                _stealth.SetVisibility(ent, 0.3f, oldStealth);
                _stealth.SetEnabled(ent, false, oldStealth);
                return;
            }
        }

        if (!HasComp<StealthComponent>(ent))
        {
            var stealth = EnsureComp<StealthComponent>(ent);
            _stealth.SetVisibility(ent, 0.3f, stealth);
            _stealth.SetEnabled(ent, false, stealth);
        }
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<CloakOfDarknessGenComponent> ent, ref ComponentShutdown args)
    {
        if (HasComp<StealthComponent>(ent))
            RemComp<StealthComponent>(ent);
        _action.RemoveAction(ent.Comp.CloakOfDarknessActionEntity);
    }

    [SubscribeLocalEvent]
    private void OnCloakOfDarkness(Entity<CloakOfDarknessGenComponent> ent, ref CloakOfDarknessActionEvent args)
    {
        if (TryComp(ent, out StealthComponent? stealth))
        {
            if (stealth.Enabled)
            {
                _stealth.SetEnabled(ent, false, stealth);
            }
            else
            {
                _stealth.SetEnabled(ent, true, stealth);
            }
        }

        args.Handled = true;
    }
}

