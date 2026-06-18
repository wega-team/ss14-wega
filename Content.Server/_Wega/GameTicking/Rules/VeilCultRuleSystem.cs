using System.Linq;
using Content.Server.Actions;
using Content.Server.Administration.Logs;
using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Server.Objectives.Systems;
using Content.Server.Roles;
using Content.Server.RoundEnd;
using Content.Server.Audio;
using Content.Server.Chat.Systems;
using Content.Shared.Achievements;
using Content.Shared.Veil.Cult.Components;
using Content.Shared.Clumsy;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Database;
using Content.Shared.GameTicking.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared.Popups;
using Content.Shared.Pinpointer;
using Content.Shared.Zombies;
using Content.Shared.Station;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Server.Player;
using Content.Shared.Veil.Cult;

namespace Content.Server.GameTicking.Rules
{
    public sealed partial class VeilCultRuleSystem : GameRuleSystem<VeilCultRuleComponent>
    {
        [Dependency] private SharedAchievementsSystem _achievement = default!;
        [Dependency] private ActionsSystem _action = default!;
        [Dependency] private AntagSelectionSystem _antag = default!;
        [Dependency] private ISharedPlayerManager _player = default!;
        [Dependency] private IPlayerManager _playerManager = default!;
        [Dependency] private IAdminLogManager _adminLogManager = default!;
        [Dependency] private MindSystem _mind = default!;
        [Dependency] private NpcFactionSystem _npcFaction = default!;
        [Dependency] private RoleSystem _role = default!;
        [Dependency] private RoundEndSystem _roundEndSystem = default!;
        [Dependency] private IRobustRandom _random = default!;
        [Dependency] private SharedAudioSystem _audio = default!;
        [Dependency] private SharedPopupSystem _popup = default!;
        [Dependency] private ObjectivesSystem _objectives = default!;
        [Dependency] private TargetObjectiveSystem _target = default!;
        [Dependency] private MetaDataSystem _meta = default!;
        [Dependency] private SharedStationSystem _stationSystem = default!;
        [Dependency] private EntityLookupSystem _entityLookup = default!;
        [Dependency] private ServerGlobalSoundSystem _sound = default!;
        [Dependency] private ChatSystem _chat = default!;

        public readonly ProtoId<NpcFactionPrototype> VeilCultNpcFaction = "VeilCult";

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<VeilCultRuleComponent, AfterAntagEntitySelectedEvent>(OnCultistSelected);

            SubscribeLocalEvent<VeilCultistComponent, ComponentStartup>((_, _, _) => CheckStage());
            SubscribeLocalEvent<VeilCultConstructComponent, ComponentStartup>((_, _, _) => CheckStage());

            SubscribeLocalEvent<VeilGodCalledEvent>(OnGodCalled);
            SubscribeLocalEvent<VeilRitualConductedEvent>(OnRitualConducted);

            SubscribeLocalEvent<AutoVeilCultistComponent, ComponentStartup>(OnAutoCultistAdded);
            SubscribeLocalEvent<VeilCultistComponent, ComponentRemove>(OnComponentRemove);
            SubscribeLocalEvent<VeilCultistComponent, MobStateChangedEvent>(OnMobStateChanged);
            SubscribeLocalEvent<VeilCultistComponent, EntityZombifiedEvent>(OnCultistZombified);
        }

        private void OnCultistSelected(Entity<VeilCultRuleComponent> mindId, ref AfterAntagEntitySelectedEvent args)
        {
            var ent = args.EntityUid;

            if (mindId.Comp.SelectedTargets.Count == 0)
                SelectRandomTargets(mindId.Comp);

            MakeCultist(ent);
            _antag.SendBriefing(ent, MakeBriefing(ent), Color.Orange, null);
        }

        private void MakeCultist(EntityUid ent)
        {
            var actionPrototypes = new[]
            {
                VeilCultistComponent.MidasTouch
            };

            foreach (var actionPrototype in actionPrototypes)
                _action.AddAction(ent, actionPrototype);

            var componentsToRemove = new[]
            {
                typeof(PacifiedComponent),
                typeof(ClumsyComponent)
            };

            foreach (var compType in componentsToRemove)
                RemComp(ent, compType);

            CreateObjectivesForCultist(ent);
        }

        private string MakeBriefing(EntityUid ent)
        {
            var isHuman = HasComp<HumanoidProfileComponent>(ent);
            var briefing = isHuman
                ? Loc.GetString("veil-cult-role-greeting-human")
                : Loc.GetString("veil-cult-role-greeting-animal");

            return briefing;
        }

        private void SelectRandomTargets(VeilCultRuleComponent cult)
        {
            cult.SelectedTargets.Clear();

            var stations = _stationSystem.GetStations();
            if (stations.Count == 0)
                return;

            var station = stations[0];
            var mainGrid = _stationSystem.GetLargestGrid(station);
            cult.Station = mainGrid;
            var placeCandidates = new List<EntityUid>();
            var enumerator = EntityQueryEnumerator<NavMapBeaconComponent, TransformComponent>(); // WarpPoint doesnt usually work here cuz were using navigation beacons now.

            while (enumerator.MoveNext(out var uid, out var beacon, out var xform))
            {
                if (xform.GridUid != mainGrid)
                    continue;

                if (beacon.DefaultText == null)
                    continue;

                placeCandidates.Add(uid);
            }

            if (placeCandidates.Count >= 6)
            {
                var selectedIndices = new HashSet<int>();
                while (selectedIndices.Count < 6)
                {
                    var index = _random.Next(0, placeCandidates.Count);
                    selectedIndices.Add(index);
                }

                foreach (var index in selectedIndices)
                {
                    var target = placeCandidates[index];
                    cult.SelectedTargets.Add(target);
                }

                return;
            }

            foreach (var target in placeCandidates)
                cult.SelectedTargets.Add(target);
        }

        private void CreateObjectivesForCultist(EntityUid cultist)
        {
            var cult = GetActiveRule();
            if (cult == null || cult.SelectedTargets.Count == 0)
                return;

            if (!_mind.TryGetMind(cultist, out var mindId, out var mind))
                return;

            foreach (var target in cult.SelectedTargets)
            {
                if (!Exists(target))
                    continue;

                var objective = _objectives.TryCreateObjective(mindId, mind, cult.ObjectivePrototype);
                if (objective == null)
                    continue;

                if (TryComp<NavMapBeaconComponent>(target, out var beacon) && beacon.DefaultText != null)
                {
                    _target.SetTarget(objective.Value, target);
                    _meta.SetEntityName(objective.Value, Loc.GetString("objective-condition-veil-ritual-beacon-title",
                        ("targetName", Loc.GetString(beacon.DefaultText))));
                    _mind.AddObjective(mindId, mind, objective.Value);
                }
            }
        }

        private void OnAutoCultistAdded(EntityUid uid, AutoVeilCultistComponent comp, ComponentStartup args)
        {
            if (!_mind.TryGetMind(uid, out var mindId, out var mind) || HasComp<VeilCultistComponent>(uid))
            {
                RemComp<AutoVeilCultistComponent>(uid);
                return;
            }

            _npcFaction.AddFaction(uid, VeilCultNpcFaction);
            var culsistComp = EnsureComp<VeilCultistComponent>(uid);
            _adminLogManager.Add(LogType.Mind, LogImpact.Medium, $"{ToPrettyString(uid)} converted into a Veil Cult");
            if (mindId == default || !_role.MindHasRole<VeilCultistComponent>(mindId))
                _role.MindAddRole(mindId, "MindRoleVeilCultist");
            if (mind is { UserId: not null } && _player.TryGetSessionById(mind.UserId, out var session))
                _antag.SendBriefing(session, MakeBriefing(uid), Color.Orange, new SoundPathSpecifier("/Audio/_Wega/Ambience/Antag/veilcult_start.ogg"));
            RemComp<AutoVeilCultistComponent>(uid);

            var mindLink = EnsureComp<MindLinkComponent>(uid);
            mindLink.Channels.Add(culsistComp.CultMindChannel);

            var ritualObjective = _objectives.TryCreateObjective(mindId, mind, "VeilCultRitualObjective");
            if (ritualObjective != null) _mind.AddObjective(mindId, mind, ritualObjective.Value);

            MakeCultist(uid);
        }

        private void CheckStage()
        {
            var cult = GetActiveRule();
            if (cult == null)
                return;

            var totalCultEntities = GetCultEntities();
            var playerCount = GetPlayerCount();

            // Second
            if (playerCount >= 100 && totalCultEntities >= playerCount * 0.1f || playerCount < 100 && totalCultEntities >= playerCount * 0.2f)
            {
                foreach (var cultist in GetAllCultists())
                {
                    if (!HasComp<VeilCultistHandsComponent>(cultist))
                    {
                        AddComp<VeilCultistHandsComponent>(cultist);
                    }
                }

                if (!cult.FirstTriggered)
                {
                    var actorFilter = Filter.Empty();
                    var actorQuery = EntityQueryEnumerator<ActorComponent, VeilCultistComponent>();
                    while (actorQuery.MoveNext(out var actorUid, out var actor, out _))
                    {
                        actorFilter.AddPlayer(actor.PlayerSession);
                        _popup.PopupEntity(Loc.GetString("veil-cult-first-warning"), actorUid, actorUid, PopupType.SmallCaution);
                    }

                    _audio.PlayGlobal(new SoundPathSpecifier("/Audio/_Wega/Ambience/Antag/bloodcult_eyes.ogg"), actorFilter, true);
                    cult.FirstTriggered = true;
                }
            }

            // Third
            if (playerCount >= 100 && totalCultEntities >= playerCount * 0.2f || playerCount < 100 && totalCultEntities >= playerCount * 0.3f)
            {
                foreach (var cultist in GetAllCultists())
                {
                    EnsureComp<VeilCogDisplayComponent>(cultist);
                }

                if (!cult.SecondTriggered)
                {
                    var actorFilter = Filter.Empty();
                    var actorQuery = EntityQueryEnumerator<ActorComponent, VeilCultistComponent>();
                    while (actorQuery.MoveNext(out var actorUid, out var actor, out _))
                    {
                        actorFilter.AddPlayer(actor.PlayerSession);
                        _popup.PopupEntity(Loc.GetString("veil-cult-second-warning"), actorUid, actorUid, PopupType.SmallCaution);
                    }

                    _sound.PlayAdminGlobal(Filter.Empty().AddAllPlayers(_playerManager), _audio.ResolveSound(new SoundPathSpecifier("/Audio/_Wega/Ambience/Antag/veilcult_start.ogg")));
                    cult.SecondTriggered = true;
                    _chat.DispatchGlobalAnnouncement(Loc.GetString("veil-cult-second-phase"), playSound: false, colorOverride: Color.Orange);
                }
            }
        }

        // sub-methods region

        private int GetCultEntities()
        {
            var totalCultists = GetAllCultists().Count;
            var totalConstructs = GetAllConstructs().Count;
            return totalCultists + totalConstructs;
        }

        private int GetPlayerCount()
        {
            int count = 0;
            var players = AllEntityQuery<HumanoidProfileComponent, ActorComponent, TransformComponent>();
            while (players.MoveNext(out _, out _, out _, out _))
                count++;

            return count;
        }

        private List<EntityUid> GetAllCultists()
        {
            var cultists = new List<EntityUid>();
            var enumerator = EntityQueryEnumerator<VeilCultistComponent>();
            while (enumerator.MoveNext(out var uid, out _))
                cultists.Add(uid);

            return cultists;
        }

        private List<EntityUid> GetAllConstructs()
        {
            var constructs = new List<EntityUid>();
            var enumerator = EntityQueryEnumerator<VeilCultConstructComponent>();
            while (enumerator.MoveNext(out var uid, out _))
                constructs.Add(uid);

            return constructs;
        }

        public VeilCultRuleComponent? GetActiveRule()
        {
            var query = QueryActiveRules();
            while (query.MoveNext(out _, out _, out var cult, out _))
            {
                return cult;
            }
            return null;
        }

        public bool TryUseEnergy(float amount)
        {
            var comp = GetActiveRule();
            if (comp == null)
                return true;

            if (comp.EnergyCount < amount)
                return false;

            comp.EnergyCount -= amount;
            return true;
        }

        public bool CheckObjectives()
        {
            var cult = GetActiveRule();
            if (cult == null)
                return false;

            foreach (var target in cult.SelectedTargets)
            {
                var beacons = _entityLookup.GetEntitiesInRange<VeilCultBeaconComponent>(Transform(target).Coordinates, 10f);
                if (beacons.Count < 1)
                    return false;
            }
            return true;
        }
        // endround

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

        // round stages

        private void OnGodCalled(VeilGodCalledEvent ev)
        {
            var cult = GetActiveRule();
            if (cult == null)
                return;

            if (cult.VeilCultWinCondition.Contains(VeilCultWinType.RitualConducted))
                cult.VeilCultWinCondition.Remove(VeilCultWinType.RitualConducted);

            cult.WinType = VeilCultWinType.GodCalled;

            if (!cult.VeilCultWinCondition.Contains(VeilCultWinType.GodCalled))
            {
                cult.VeilCultWinCondition.Add(VeilCultWinType.GodCalled);
                _roundEndSystem.DoRoundEndBehavior(RoundEndBehavior.ShuttleCall, TimeSpan.FromMinutes(1f));
            }

            var cultists = GetAllCultists();
            cultists.AddRange(GetAllConstructs());
            foreach (var cultist in cultists)
            {
                // Yes, you have reached this stage, and it was achieved with your help.
                _achievement.QueueAchievement(cultist, AchievementsEnum.VeilCult);
            }
        }

        private void OnRitualConducted(VeilRitualConductedEvent ev)
        {
            var cult = GetActiveRule();
            if (cult == null)
                return;

            cult.RitualStage = true;
            cult.WinType = VeilCultWinType.RitualConducted;

            CheckStage();

            if (!cult.VeilCultWinCondition.Contains(VeilCultWinType.RitualConducted))
                cult.VeilCultWinCondition.Add(VeilCultWinType.RitualConducted);
        }

        private void OnMobStateChanged(EntityUid uid, VeilCultistComponent component, MobStateChangedEvent ev)
        {
            if (ev.NewMobState == MobState.Dead)
                CheckCultLose(GetActiveRule());
        }

        private void OnComponentRemove(EntityUid uid, VeilCultistComponent component, ComponentRemove args)
        {
            CheckCultLose(GetActiveRule());
        }

        private void OnCultistZombified(EntityUid uid, VeilCultistComponent component, EntityZombifiedEvent args)
        {
            CheckCultLose(GetActiveRule());
        }

        private void CheckCultLose(VeilCultRuleComponent? cult)
        {
            if (cult == null)
                return;

            var hasLivingCultists = EntityQuery<VeilCultistComponent>().Any();
            if (!hasLivingCultists && !cult.VeilCultWinCondition.Contains(VeilCultWinType.GodCalled)
                && !cult.VeilCultWinCondition.Contains(VeilCultWinType.RitualConducted))
            {
                cult.VeilCultWinCondition.Add(VeilCultWinType.CultLose);
                cult.WinType = VeilCultWinType.CultLose;
            }
        }
    }
}
