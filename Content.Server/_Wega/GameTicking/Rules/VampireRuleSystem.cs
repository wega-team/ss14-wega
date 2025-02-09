using Content.Server.Antag;
using Content.Server.Atmos.Components;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Roles;
using Content.Server.Temperature.Components;
using Content.Server.Actions;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Clumsy;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Damage;
using Content.Shared.GameTicking.Components;
using Content.Shared.Humanoid;
using Content.Shared.Nutrition.Components;
using Content.Shared.Temperature.Components;
using Content.Shared.Vampire.Components;

namespace Content.Server.GameTicking.Rules
{
    public sealed class VampireRuleSystem : GameRuleSystem<VampireRuleComponent>
    {
        [Dependency] private readonly AntagSelectionSystem _antag = default!;
        [Dependency] private readonly BodySystem _body = default!;
        [Dependency] private readonly MetabolizerSystem _metabolism = default!;
        [Dependency] private readonly ActionsSystem _actions = default!;
        [Dependency] private readonly DamageableSystem _damage = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<VampireRuleComponent, AfterAntagEntitySelectedEvent>(OnVampireSelected);
            SubscribeLocalEvent<VampireRoleComponent, GetBriefingEvent>(OnVampireBriefing);
        }

        private void OnVampireSelected(Entity<VampireRuleComponent> mindId, ref AfterAntagEntitySelectedEvent args)
        {
            var ent = args.EntityUid;
            MakeVampire(ent);
            _antag.SendBriefing(ent, MakeBriefing(ent), Color.Purple, null);
        }

        private void OnVampireBriefing(Entity<VampireRoleComponent> vampire, ref GetBriefingEvent args)
        {
            var ent = args.Mind.Comp.OwnedEntity;
            if (ent is null)
                return;

            args.Append(MakeBriefing(ent.Value));
        }

        private string MakeBriefing(EntityUid ent)
        {
            var isHuman = HasComp<HumanoidAppearanceComponent>(ent);
            var briefing = isHuman
                ? Loc.GetString("vampire-role-greeting-human")
                : Loc.GetString("vampire-role-greeting-animal");

            return briefing;
        }

        protected override void AppendRoundEndText(EntityUid uid,
            VampireRuleComponent component,
            GameRuleComponent gameRule,
            ref RoundEndTextAppendEvent args)
        {
            var totalBloodDrank = GetTotalBloodDrankInRound();
            args.AddLine(Loc.GetString("vampires-drank-total-blood", ("bloodAmount", totalBloodDrank)));
        }

        private float GetTotalBloodDrankInRound()
        {
            var totalBloodDrank = 0f;
            foreach (var vampireEntity in EntityManager.EntityQuery<VampireComponent>(true))
            {
                totalBloodDrank += vampireEntity.TotalBloodDrank;
            }

            return totalBloodDrank;
        }

        private void MakeVampire(EntityUid vampire)
        {
            var vampireComponent = EnsureComp<VampireComponent>(vampire);

            RemoveUnnecessaryComponents(vampire);
            HandleMetabolismAndOrgans(vampire);
            SetVampireComponents(vampire, vampireComponent);
            UpdateAppearance(vampire);
            AddVampireActions(vampire);
        }

        private void RemoveUnnecessaryComponents(EntityUid vampire)
        {
            var componentsToRemove = new[]
            {
                typeof(PacifiedComponent),
                typeof(PerishableComponent),
                typeof(BarotraumaComponent),
                typeof(TemperatureSpeedComponent),
                typeof(ThirstComponent),
                typeof(ClumsyComponent)
            };

            foreach (var compType in componentsToRemove)
            {
                if (HasComp(vampire, compType))
                    RemComp(vampire, compType);
            }
        }

        private void HandleMetabolismAndOrgans(EntityUid vampire)
        {
            if (TryComp<BodyComponent>(vampire, out var bodyComponent))
            {
                foreach (var organ in _body.GetBodyOrgans(vampire, bodyComponent))
                {
                    if (TryComp<MetabolizerComponent>(organ.Id, out var metabolizer))
                    {
                        if (TryComp<StomachComponent>(organ.Id, out _))
                            _metabolism.ClearMetabolizerTypes(metabolizer);

                        _metabolism.TryAddMetabolizerType(metabolizer, VampireComponent.MetabolizerVampire);
                    }
                }
            }
        }

        private void SetVampireComponents(EntityUid vampire, VampireComponent _)
        {
            if (TryComp<TemperatureComponent>(vampire, out var temperatureComponent))
                temperatureComponent.ColdDamageThreshold = Atmospherics.TCMB;

            EnsureComp<UnholyComponent>(vampire);
            EnsureComp<VampireComponent>(vampire);

            _damage.SetDamageModifierSetId(vampire, "Vampire");

            if (TryComp<ReactiveComponent>(vampire, out var reactive))
            {
                reactive.ReactiveGroups ??= new();

                if (!reactive.ReactiveGroups.ContainsKey("Unholy"))
                {
                    reactive.ReactiveGroups.Add("Unholy", new() { ReactionMethod.Touch });
                }
            }
        }

        private void UpdateAppearance(EntityUid vampire)
        {
            if (TryComp<HumanoidAppearanceComponent>(vampire, out var appearanceComponent))
            {
                appearanceComponent.EyeColor = Color.FromHex("#E22218FF");
                Dirty(vampire, appearanceComponent);
            }
        }

        private void AddVampireActions(EntityUid vampire)
        {
            var actionPrototypes = new[]
            {
                VampireComponent.DrinkActionPrototype,
                VampireComponent.SelectClassActionPrototype,
                VampireComponent.RejuvenateActionPrototype,
                VampireComponent.GlareActionPrototype
            };

            foreach (var actionPrototype in actionPrototypes)
            {
                _actions.AddAction(vampire, actionPrototype);
            }
        }
    }
}
