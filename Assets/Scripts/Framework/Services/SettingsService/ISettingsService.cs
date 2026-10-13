using System;

namespace PTL.Framework.Services
{
    /// <summary>
    /// Player-facing options (music/SFX volume, haptics) persisted across sessions. Registered as a
    /// persistent service in <see cref="Bootstrap"/>. The Settings popup writes it; <c>AudioManager</c>
    /// and haptic callers read it and listen for changes.
    /// </summary>
    public interface ISettingsService : IService
    {
        /// <summary>Fired after any setting changes.</summary>
        event Action OnSettingsChanged;

        /// <summary>Music (looping sounds) volume, 0..1.</summary>
        float MusicVolume { get; }

        /// <summary>SFX (one-shot sounds) volume, 0..1.</summary>
        float SFXVolume { get; }

        /// <summary>Whether haptic feedback is enabled.</summary>
        bool HapticsEnabled { get; }

        /// <summary>
        /// Music on/off. Independent of <see cref="MusicVolume"/> so a mute toggle (pause menu) and
        /// a volume slider (settings popup) can coexist: muting never loses the chosen volume.
        /// </summary>
        bool MusicEnabled { get; }

        /// <summary>SFX on/off. See <see cref="MusicEnabled"/>.</summary>
        bool SFXEnabled { get; }

        /// <summary><see cref="MusicVolume"/> gated by <see cref="MusicEnabled"/>. What AudioManager applies.</summary>
        float EffectiveMusicVolume { get; }

        /// <summary><see cref="SFXVolume"/> gated by <see cref="SFXEnabled"/>. What AudioManager applies.</summary>
        float EffectiveSFXVolume { get; }

        void SetMusicVolume(float value);
        void SetSFXVolume(float value);
        void SetHapticsEnabled(bool enabled);
        void SetMusicEnabled(bool enabled);
        void SetSFXEnabled(bool enabled);
    }
}
