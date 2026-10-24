using System;
using UnityEngine;

namespace PTL.Framework.Services
{
    /// <summary>
    /// Default <see cref="ISettingsService"/>. Backs every option with PlayerPrefs and raises a single
    /// change event; consumers (AudioManager, haptic callers, PoolEnvironment) read the current value
    /// and refresh.
    /// </summary>
    public class SettingsService : ISettingsService
    {
        private const string KeyMusic   = "Settings_MusicVolume";
        private const string KeySfx     = "Settings_SFXVolume";
        private const string KeyHaptics = "Settings_Haptics";
        private const string KeyMusicOn = "Settings_MusicEnabled";
        private const string KeySfxOn   = "Settings_SFXEnabled";
        private const string KeyQuality = "Settings_GraphicsQuality";

        private const float DefaultMusic = 0.7f;
        private const float DefaultSfx   = 0.8f;

        private float _musicVolume;
        private float _sfxVolume;
        private bool _hapticsEnabled;
        private bool _musicEnabled;
        private bool _sfxEnabled;
        private GraphicsQuality _graphicsQuality;

        public event Action OnSettingsChanged;

        public float MusicVolume => _musicVolume;
        public float SFXVolume => _sfxVolume;
        public bool HapticsEnabled => _hapticsEnabled;
        public bool MusicEnabled => _musicEnabled;
        public bool SFXEnabled => _sfxEnabled;
        public GraphicsQuality GraphicsQuality => _graphicsQuality;
        public GraphicsQualityProfile GraphicsProfile => GraphicsQualityProfile.For(_graphicsQuality);

        public float EffectiveMusicVolume => _musicEnabled ? _musicVolume : 0f;
        public float EffectiveSFXVolume => _sfxEnabled ? _sfxVolume : 0f;

        public void Start()
        {
            _musicVolume    = PlayerPrefs.GetFloat(KeyMusic, DefaultMusic);
            _sfxVolume      = PlayerPrefs.GetFloat(KeySfx, DefaultSfx);
            _hapticsEnabled = PlayerPrefs.GetInt(KeyHaptics, 1) == 1;
            _musicEnabled   = PlayerPrefs.GetInt(KeyMusicOn, 1) == 1;
            _sfxEnabled     = PlayerPrefs.GetInt(KeySfxOn, 1) == 1;
            // Until the player picks a tier, follow the device; the guess is not saved, so a better
            // heuristic in a later build still applies to players who never touched the setting.
            _graphicsQuality = PlayerPrefs.HasKey(KeyQuality)
                ? (GraphicsQuality)Mathf.Clamp(PlayerPrefs.GetInt(KeyQuality), (int)GraphicsQuality.Low, (int)GraphicsQuality.High)
                : GraphicsQualityProfile.DefaultForDevice();
        }

        public void OnDestroy() { PlayerPrefs.Save(); }

        public void SetMusicVolume(float value)
        {
            value = Mathf.Clamp01(value);
            if (Mathf.Approximately(value, _musicVolume)) return;
            _musicVolume = value;
            PlayerPrefs.SetFloat(KeyMusic, value);
            Changed();
        }

        public void SetSFXVolume(float value)
        {
            value = Mathf.Clamp01(value);
            if (Mathf.Approximately(value, _sfxVolume)) return;
            _sfxVolume = value;
            PlayerPrefs.SetFloat(KeySfx, value);
            Changed();
        }

        public void SetHapticsEnabled(bool enabled)
        {
            if (enabled == _hapticsEnabled) return;
            _hapticsEnabled = enabled;
            PlayerPrefs.SetInt(KeyHaptics, enabled ? 1 : 0);
            Changed();
        }

        public void SetMusicEnabled(bool enabled)
        {
            if (enabled == _musicEnabled) return;
            _musicEnabled = enabled;
            PlayerPrefs.SetInt(KeyMusicOn, enabled ? 1 : 0);
            Changed();
        }

        public void SetSFXEnabled(bool enabled)
        {
            if (enabled == _sfxEnabled) return;
            _sfxEnabled = enabled;
            PlayerPrefs.SetInt(KeySfxOn, enabled ? 1 : 0);
            Changed();
        }

        public void SetGraphicsQuality(GraphicsQuality quality)
        {
            bool firstChoice = !PlayerPrefs.HasKey(KeyQuality);
            if (quality == _graphicsQuality && !firstChoice) return;
            _graphicsQuality = quality;
            PlayerPrefs.SetInt(KeyQuality, (int)quality);
            Changed();
        }

        private void Changed()
        {
            PlayerPrefs.Save();
            OnSettingsChanged?.Invoke();
        }
    }
}
