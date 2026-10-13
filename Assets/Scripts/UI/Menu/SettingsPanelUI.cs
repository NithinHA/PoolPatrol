using DG.Tweening;
using PTL.Framework;
using PTL.Framework.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Menu
{
    /// <summary>
    /// The settings popup: music/SFX volume sliders, a haptics toggle, and Terms/Privacy links. Reads
    /// and writes <see cref="ISettingsService"/> (which persists and notifies AudioManager). Opens from
    /// the gear button with a small scale/fade pop.
    /// </summary>
    public class SettingsPanelUI : MonoBehaviour
    {
        [Header("Root")]
        [Tooltip("The popup container that is shown/hidden (often a dimmer + box).")]
        [SerializeField] private GameObject m_Root;
        [SerializeField] private RectTransform m_Box;

        [Header("Open / close")]
        [SerializeField] private Button m_OpenButton;
        [SerializeField] private Button m_CloseButton;

        [Header("Controls")]
        [SerializeField] private Slider m_MusicSlider;
        [SerializeField] private Slider m_SfxSlider;
        [SerializeField] private Toggle m_HapticsToggle;
        [SerializeField] private TMP_Text m_MusicValueText;
        [SerializeField] private TMP_Text m_SfxValueText;

        [Header("Links")]
        [SerializeField] private Button m_TermsButton;
        [SerializeField] private Button m_PrivacyButton;
        [SerializeField] private string m_TermsUrl = "https://example.com/terms";
        [SerializeField] private string m_PrivacyUrl = "https://example.com/privacy";

        private ISettingsService _settings;
        private Tween _tween;

        private void Awake()
        {
            _settings = ServiceLocator.GetSettingsService();

            if (m_OpenButton != null) m_OpenButton.onClick.AddListener(Open);
            if (m_CloseButton != null) m_CloseButton.onClick.AddListener(Close);
            if (m_TermsButton != null) m_TermsButton.onClick.AddListener(() => OpenUrl(m_TermsUrl));
            if (m_PrivacyButton != null) m_PrivacyButton.onClick.AddListener(() => OpenUrl(m_PrivacyUrl));

            if (m_MusicSlider != null) m_MusicSlider.onValueChanged.AddListener(OnMusicChanged);
            if (m_SfxSlider != null) m_SfxSlider.onValueChanged.AddListener(OnSfxChanged);
            if (m_HapticsToggle != null) m_HapticsToggle.onValueChanged.AddListener(OnHapticsChanged);

            if (m_Root != null) m_Root.SetActive(false);
        }

        private void OnDestroy() => _tween?.Kill();

        public void Open()
        {
            SyncFromService();
            if (m_Root != null) m_Root.SetActive(true);

            if (m_Box != null)
            {
                _tween?.Kill();
                m_Box.localScale = Vector3.one * 0.85f;
                _tween = m_Box.DOScale(1f, 0.25f).SetEase(Ease.OutBack).SetUpdate(true);
            }
        }

        public void Close()
        {
            if (m_Box == null)
            {
                if (m_Root != null) m_Root.SetActive(false);
                return;
            }

            _tween?.Kill();
            _tween = m_Box.DOScale(0.85f, 0.18f).SetEase(Ease.InBack).SetUpdate(true)
                .OnComplete(() => { if (m_Root != null) m_Root.SetActive(false); });
        }

        private void SyncFromService()
        {
            if (_settings == null) return;

            if (m_MusicSlider != null) m_MusicSlider.SetValueWithoutNotify(_settings.MusicVolume);
            if (m_SfxSlider != null) m_SfxSlider.SetValueWithoutNotify(_settings.SFXVolume);
            if (m_HapticsToggle != null) m_HapticsToggle.SetIsOnWithoutNotify(_settings.HapticsEnabled);
            UpdateValueText(m_MusicValueText, _settings.MusicVolume);
            UpdateValueText(m_SfxValueText, _settings.SFXVolume);
        }

        private void OnMusicChanged(float v)
        {
            _settings?.SetMusicVolume(v);
            UpdateValueText(m_MusicValueText, v);
        }

        private void OnSfxChanged(float v)
        {
            _settings?.SetSFXVolume(v);
            UpdateValueText(m_SfxValueText, v);
        }

        private void OnHapticsChanged(bool on) => _settings?.SetHapticsEnabled(on);

        private static void UpdateValueText(TMP_Text text, float value01)
        {
            if (text != null) text.text = Mathf.RoundToInt(value01 * 100f) + "%";
        }

        private static void OpenUrl(string url)
        {
            if (!string.IsNullOrEmpty(url)) Application.OpenURL(url);
        }
    }
}
