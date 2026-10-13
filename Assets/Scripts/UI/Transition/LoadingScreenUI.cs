using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Transition
{
    /// <summary>
    /// The splash / loading panel: a logo and a progress bar. Shown while a scene loads; its fill is
    /// driven by the scene service's progress callback.
    /// </summary>
    public class LoadingScreenUI : MonoBehaviour
    {
        [SerializeField] private GameObject m_Root;
        [Tooltip("A filled Image (Image Type = Filled) used as the progress bar. Optional if a Slider is used.")]
        [SerializeField] private Image m_ProgressFill;
        [SerializeField] private Slider m_ProgressSlider;
        [SerializeField] private TMP_Text m_ProgressText;

        public void Show()
        {
            if (m_Root != null) m_Root.SetActive(true);
            SetProgress(0f);
        }

        public void Hide()
        {
            if (m_Root != null) m_Root.SetActive(false);
        }

        public void SetProgress(float value01)
        {
            value01 = Mathf.Clamp01(value01);
            if (m_ProgressFill != null) m_ProgressFill.fillAmount = value01;
            if (m_ProgressSlider != null) m_ProgressSlider.value = value01;
            if (m_ProgressText != null) m_ProgressText.text = Mathf.RoundToInt(value01 * 100f) + "%";
        }
    }
}
