using Content.Client._Wega.Lobby.UI; // Corvax-Wega
using Content.Client.Corvax.TTS;
using Content.Client.Speech.Synthesis.System; // Corvax-Wega
using Content.Shared.CCVar; // Corvax-Wega
using Content.Shared.Corvax.CCCVars;
using Robust.Client.UserInterface;

namespace Content.Client.Lobby.UI;

// Corvax-Wega-Edit-start
public sealed partial class HumanoidProfileEditor
{
    private VoiceTab? _voiceTab;

    private void RefreshVoiceTab()
    {
        _voiceTab = new VoiceTab();

        var children = new List<Control>();
        foreach (var child in TabContainer.Children)
            children.Add(child);

        TabContainer.RemoveAllChildren();

        for (int i = 0; i < children.Count; i++)
        {
            if (i == 1) // 2nd place
                TabContainer.AddChild(_voiceTab);
            TabContainer.AddChild(children[i]);
        }

        TabContainer.SetTabTitle(1, Loc.GetString("humanoid-profile-editor-voice-tab"));

        _voiceTab.OnTtsVoiceSelected += voiceId =>
        {
            SetVoice(voiceId);
            _voiceTab.SetSelectedTtsVoice(voiceId);
        };

        _voiceTab.OnTtsPreviewRequested += voiceId =>
        {
            _entManager.System<TTSSystem>().RequestPreviewTTS(voiceId);
        };

        _voiceTab.OnBarkVoiceSelected += barkId =>
        {
            SetBarkVoice(barkId);
            _voiceTab.SetSelectedBarkVoice(barkId);
        };

        _voiceTab.OnBarkPreviewRequested += barkId =>
        {
            _entManager.System<BarkSystem>().RequestPreviewBark(barkId);
        };

        _cfgManager.OnValueChanged(CCCVars.TTSEnabled, OnVoiceTabConfigChanged);
        _cfgManager.OnValueChanged(WegaCVars.BarksEnabled, OnVoiceTabConfigChanged);

        UpdateVoiceTabVisibility();
    }

    private void OnVoiceTabConfigChanged(bool _) => UpdateVoiceTabVisibility();

    private void UpdateVoiceTabVisibility()
    {
        if (_voiceTab == null || _voiceTab.Disposed)
            return;

        var ttsOn = _cfgManager.GetCVar(CCCVars.TTSEnabled);
        var barksOn = _cfgManager.GetCVar(WegaCVars.BarksEnabled);

        _voiceTab.SetTtsVisible(ttsOn);
        _voiceTab.SetBarksVisible(barksOn);

        var index = -1;
        for (var i = 0; i < TabContainer.ChildCount; i++)
        {
            if (TabContainer.GetChild(i) == _voiceTab)
            {
                index = i;
                break;
            }
        }

        if (index < 0)
            return;

        var voiceTabVisible = ttsOn || barksOn;
        TabContainer.SetTabVisible(index, voiceTabVisible);

        if (!voiceTabVisible && TabContainer.CurrentTab == index)
        {
            for (var i = 0; i < TabContainer.ChildCount; i++)
            {
                if (i != index && TabContainer.GetTabVisible(i))
                {
                    TabContainer.CurrentTab = i;
                    break;
                }
            }
        }
    }

    private void UpdateTTSVoicesControls()
    {
        if (Profile is null || _voiceTab is null)
            return;

        _voiceTab.UpdateTtsControls(Profile, Profile.Voice);
        _voiceTab.UpdateBarkControls(Profile);
    }

    private void SetVoice(string newVoice)
    {
        Profile = Profile?.WithVoice(newVoice);
        IsDirty = true;
    }

    private void SetBarkVoice(string newBark)
    {
        Profile = Profile?.WithBarkVoice(newBark);
        IsDirty = true;
    }
}
// Corvax-Wega-Edit-end
