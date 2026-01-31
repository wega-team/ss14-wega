using System.Linq;
using Content.Client._Wega.Achievements;
using Content.Shared.Achievements;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.Achievements;

public sealed class AchievementsSystem : SharedAchievementsSystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public IReadOnlyList<AchievementsEnum> UnlockedAchievements => _unlockedAchievements;
    private List<AchievementsEnum> _unlockedAchievements = new();

    public event Action? OnAchievementsUpdated;
    public event Action<AchievementsEnum>? OnAchievementUnlocked;

    private Queue<(AchievementsEnum achievement, TimeSpan showTime)> _notificationQueue = new();
    private AchievementNotification? _currentNotification;
    private TimeSpan _notificationStartTime;
    private const float NOTIFICATION_DURATION = 5f;
    private const float NOTIFICATION_SLIDE_TIME = 0.5f;
    private bool _achievementsRequested = false;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<ResponseAchievementsEvent>(OnAchievementsResponse);
        SubscribeNetworkEvent<AchievementUnlockedEvent>(OnAchievementUnlockedEvent);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (!_achievementsRequested)
        {
            _achievementsRequested = true;
            RaiseNetworkEvent(new RequestAchievementsEvent());
        }

        ProcessNotificationQueue();
    }

    public override List<AchievementsEnum> GetUnlockedAchievements()
        => _unlockedAchievements;

    private void ProcessNotificationQueue()
    {
        var currentTime = _timing.RealTime;

        if (_currentNotification != null &&
            currentTime - _notificationStartTime > TimeSpan.FromSeconds(NOTIFICATION_DURATION))
        {
            _ui.WindowRoot.RemoveChild(_currentNotification);
            _currentNotification = null;
        }

        if (_currentNotification == null && _notificationQueue.Count > 0)
        {
            var (achievement, showTime) = _notificationQueue.Dequeue();
            if (currentTime - showTime < TimeSpan.FromSeconds(30))
            {
                ShowAchievementNotification(achievement);
            }
        }
    }

    private void OnAchievementsResponse(ResponseAchievementsEvent ev)
    {
        _unlockedAchievements = ev.Achievements;
        OnAchievementsUpdated?.Invoke();
    }

    private void OnAchievementUnlockedEvent(AchievementUnlockedEvent ev)
    {
        if (_playerManager.LocalEntity != null &&
            GetNetEntity(_playerManager.LocalEntity) == ev.User)
        {
            if (!_unlockedAchievements.Contains(ev.Achievement))
            {
                _unlockedAchievements.Add(ev.Achievement);

                _notificationQueue.Enqueue((ev.Achievement, _timing.RealTime));

                OnAchievementUnlocked?.Invoke(ev.Achievement);
                OnAchievementsUpdated?.Invoke();
            }
        }
    }

    private void ShowAchievementNotification(AchievementsEnum achievement)
    {
        var prototype = _prototype.EnumeratePrototypes<AchievementPrototype>()
            .FirstOrDefault(p => p.Key == achievement);

        if (prototype == null)
            return;

        if (_currentNotification == null)
        {
            _currentNotification = new AchievementNotification();

            _ui.WindowRoot.AddChild(_currentNotification);

            LayoutContainer.SetAnchorRight(_currentNotification, 1.0f);
            LayoutContainer.SetAnchorBottom(_currentNotification, 1.0f);
            LayoutContainer.SetMarginRight(_currentNotification, -20);
            LayoutContainer.SetMarginBottom(_currentNotification, -20);
        }

        var icon = _sprite.Frame0(prototype.AchievementIcon);

        var name = Loc.GetString(prototype.Name);
        var description = prototype.Description != null
            ? Loc.GetString(prototype.Description)
            : string.Empty;

        _currentNotification.Show(name, description, icon);
        _notificationStartTime = _timing.RealTime;

        PlayAchievementSound();

        StartNotificationAnimation(_currentNotification);
    }

    private void PlayAchievementSound()
    {
        if (_playerManager.LocalSession is not { } session)
            return;

        var soundPath = new SoundPathSpecifier("/Audio/_Wega/Achievements/achievement_unlocked.ogg");
        _audio.PlayGlobal(soundPath, Filter.SinglePlayer(session), false, AudioParams.Default);
    }

    private void StartNotificationAnimation(AchievementNotification notification)
    {
        var control = notification.NotificationRoot;
        control.Modulate = Color.Transparent;

        var animation = new Animation
        {
            Length = TimeSpan.FromSeconds(NOTIFICATION_SLIDE_TIME),
            AnimationTracks =
            {
                new AnimationTrackControlProperty
                {
                    Property = nameof(Control.Modulate),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(Color.White, 0f),
                    }
                }
            }
        };

        control.PlayAnimation(animation, "achievement_notification_show");
    }

    public void RequestAchievements()
    {
        if (_playerManager.LocalSession?.UserId is not { } userId)
            return;

        RaiseNetworkEvent(new RequestAchievementsEvent());
    }

    public bool HasAchievement(AchievementsEnum achievement)
    {
        return _unlockedAchievements.Contains(achievement);
    }
}
