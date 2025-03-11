using System.Linq;
using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.RoundEnd;
using Content.Shared.Veil.Cult;
using Content.Shared.Veil.Cult.Components;
using Content.Shared.Clumsy;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mobs;
using Content.Shared.NPC.Prototypes;
using Content.Shared.Zombies;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules
{
    public sealed class VeilCultRuleSystem : GameRuleSystem<VeilCultRuleComponent>
    {
        [Dependency] private readonly AntagSelectionSystem _antag = default!;
        [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;

        public readonly ProtoId<NpcFactionPrototype> VeilCultNpcFaction = "VeilCult";

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<VeilCultRuleComponent, AfterAntagEntitySelectedEvent>(OnCultistSelected);

            SubscribeLocalEvent<GodCalledEvent>(OnGodCalled);
            SubscribeLocalEvent<RitualConductedEvent>(OnRitualConducted);

            SubscribeLocalEvent<VeilCultistComponent, ComponentRemove>(OnComponentRemove);
            SubscribeLocalEvent<VeilCultistComponent, MobStateChangedEvent>(OnMobStateChanged);
            SubscribeLocalEvent<VeilCultistComponent, EntityZombifiedEvent>(OnOperativeZombified);
        }

        private void OnCultistSelected(Entity<VeilCultRuleComponent> mindId, ref AfterAntagEntitySelectedEvent args)
        {
            var ent = args.EntityUid;
            MakeCultist(ent);
        }

        private void MakeCultist(EntityUid ent)
        {
            /*
            var actionPrototypes = new[]
            {
                VeilCultistComponent.CultObjective,
                VeilCultistComponent.CultCommunication,
                VeilCultistComponent.VeilMagic,
            };

            foreach (var actionPrototype in actionPrototypes)
            {
                _action.AddAction(ent, actionPrototype);
            }
            */

            var componentsToRemove = new[]
            {
                typeof(PacifiedComponent),
                typeof(ClumsyComponent)
            };

            foreach (var compType in componentsToRemove)
            {
                if (HasComp(ent, compType))
                    RemComp(ent, compType);
            }

        }

        protected override void AppendRoundEndText(EntityUid uid,
            VeilCultRuleComponent component,
            GameRuleComponent gameRule,
            ref RoundEndTextAppendEvent args)
        {
            var winText = Loc.GetString($"veil-cult-{component.WinType.ToString().ToLower()}");
            args.AddLine(winText);

            foreach (var cond in component.VeilCultWinCondition)
            {
                var text = Loc.GetString($"veil-cult-cond-{cond.ToString().ToLower()}");
                args.AddLine(text);
            }

            args.AddLine(Loc.GetString("veil-cultist-list-start"));

            var antags = _antag.GetAntagIdentifiers(uid);
            foreach (var (_, sessionData, name) in antags)
            {
                args.AddLine(Loc.GetString("veil-cultist-list-name-user", ("name", name), ("user", sessionData.UserName)));
            }
        }

        private void OnGodCalled(GodCalledEvent ev)
        {
            var query = QueryActiveRules();
            while (query.MoveNext(out _, out _, out var cult, out _))
            {
                if (cult.VeilCultWinCondition.Contains(VeilCultWinType.RitualConducted))
                    cult.VeilCultWinCondition.Remove(VeilCultWinType.RitualConducted);

                cult.WinType = VeilCultWinType.GodCalled;

                if (!cult.VeilCultWinCondition.Contains(VeilCultWinType.GodCalled))
                {
                    cult.VeilCultWinCondition.Add(VeilCultWinType.GodCalled);
                    _roundEndSystem.DoRoundEndBehavior(RoundEndBehavior.ShuttleCall, TimeSpan.FromMinutes(1f));
                }
            }
        }

        private void OnRitualConducted(RitualConductedEvent ev)
        {
            var query = QueryActiveRules();
            while (query.MoveNext(out _, out _, out var cult, out _))
            {
                cult.WinType = VeilCultWinType.RitualConducted;

                if (!cult.VeilCultWinCondition.Contains(VeilCultWinType.RitualConducted))
                    cult.VeilCultWinCondition.Add(VeilCultWinType.RitualConducted);
            }
        }

        private void OnMobStateChanged(EntityUid uid, VeilCultistComponent component, MobStateChangedEvent ev)
        {
            if (ev.NewMobState == MobState.Dead)
            {
                var query = QueryActiveRules();
                while (query.MoveNext(out var ruleUid, out _, out var cult, out _))
                {
                    CheckCultLose(ruleUid, cult);
                }
            }
        }

        private void OnComponentRemove(EntityUid uid, VeilCultistComponent component, ComponentRemove args)
        {
            var query = QueryActiveRules();
            while (query.MoveNext(out var ruleUid, out _, out var cult, out _))
            {
                CheckCultLose(ruleUid, cult);
            }
        }

        private void OnOperativeZombified(EntityUid uid, VeilCultistComponent component, EntityZombifiedEvent args)
        {
            var query = QueryActiveRules();
            while (query.MoveNext(out var ruleUid, out _, out var cult, out _))
            {
                CheckCultLose(ruleUid, cult);
            }
        }

        private void CheckCultLose(EntityUid uid, VeilCultRuleComponent cult)
        {
            var hasLivingCultists = EntityManager.EntityQuery<VeilCultistComponent>().Any();
            if (!hasLivingCultists && !cult.VeilCultWinCondition.Contains(VeilCultWinType.GodCalled)
                && !cult.VeilCultWinCondition.Contains(VeilCultWinType.RitualConducted))
            {
                cult.VeilCultWinCondition.Add(VeilCultWinType.CultLose);
                cult.WinType = VeilCultWinType.CultLose;
            }
        }
    }
}
