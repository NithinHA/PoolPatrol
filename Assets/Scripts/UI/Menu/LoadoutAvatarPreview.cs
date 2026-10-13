using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Menu
{
    /// <summary>
    /// The floating character preview on the loadout screen. Layers floatie + head + weapon images,
    /// loops a gentle idle bob with soft ripples, and on any loadout change plays a squash-and-stretch
    /// settle plus one heavier ripple. Pure UI (DOTween-driven); no physics.
    /// </summary>
    public class LoadoutAvatarPreview : MonoBehaviour
    {
        [Header("Layers")]
        [Tooltip("The group that bobs and squashes (holds floatie/head/weapon).")]
        [SerializeField] private RectTransform m_AnimateRoot;
        [SerializeField] private Image m_FloatieImage;
        [SerializeField] private Image m_HeadImage;
        [SerializeField] private Image m_WeaponImage;

        [Header("Idle bob")]
        [SerializeField] private float m_BobHeight = 14f;
        [SerializeField] private float m_BobDuration = 1.6f;

        [Header("Ripples")]
        [Tooltip("Ring image cloned for each ripple. Should sit behind the avatar.")]
        [SerializeField] private Image m_RippleTemplate;
        [SerializeField] private Transform m_RippleParent;
        [SerializeField] private float m_IdleRippleInterval = 1.6f;
        [SerializeField] private float m_IdleRippleScale = 1.6f;
        [SerializeField] private float m_HeavyRippleScale = 2.4f;

        [Header("Squash & stretch")]
        [SerializeField] private float m_SettleDuration = 0.55f;

        private Tween _bobTween;
        private Sequence _squashSequence;
        private float _idleRippleTimer;
        private float _baseY;

        private void Awake()
        {
            if (m_AnimateRoot != null) _baseY = m_AnimateRoot.anchoredPosition.y;
            if (m_RippleTemplate != null) m_RippleTemplate.gameObject.SetActive(false);
        }

        private void OnEnable() => StartIdle();

        private void OnDisable()
        {
            _bobTween?.Kill();
            _squashSequence?.Kill();
        }

        private void OnDestroy()
        {
            _bobTween?.Kill();
            _squashSequence?.Kill();
        }

        private void Update()
        {
            if (m_RippleTemplate == null) return;
            _idleRippleTimer += Time.unscaledDeltaTime;
            if (_idleRippleTimer >= m_IdleRippleInterval)
            {
                _idleRippleTimer = 0f;
                SpawnRipple(m_IdleRippleScale, 0.35f, 1.8f);
            }
        }

        private void StartIdle()
        {
            if (m_AnimateRoot == null) return;
            _bobTween?.Kill();
            m_AnimateRoot.anchoredPosition = new Vector2(m_AnimateRoot.anchoredPosition.x, _baseY);
            _bobTween = m_AnimateRoot
                .DOAnchorPosY(_baseY + m_BobHeight, m_BobDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
        }

        public void SetFloatie(Sprite sprite) => SetSprite(m_FloatieImage, sprite);
        public void SetCharacter(Sprite sprite) => SetSprite(m_HeadImage, sprite);
        public void SetWeapon(Sprite sprite) => SetSprite(m_WeaponImage, sprite);

        private static void SetSprite(Image image, Sprite sprite)
        {
            if (image == null) return;
            image.sprite = sprite;
            image.enabled = sprite != null;
        }

        /// <summary>
        /// One heavy ripple with no squash/stretch — for a change that doesn't touch the avatar's own
        /// body (e.g. the pool texture underneath it).
        /// </summary>
        public void PlayRippleOnly() => SpawnRipple(m_HeavyRippleScale, 0.55f, 1.2f);

        /// <summary>Squash-and-stretch settle plus one heavy ripple — call after a change to the avatar itself (character/floatie/weapon).</summary>
        public void PlayChangeReaction()
        {
            SpawnRipple(m_HeavyRippleScale, 0.55f, 1.2f);

            if (m_AnimateRoot == null) return;
            _squashSequence?.Kill(true);
            m_AnimateRoot.localScale = Vector3.one;

            _squashSequence = DOTween.Sequence().SetUpdate(true);
            _squashSequence.Append(m_AnimateRoot.DOScale(new Vector3(1.14f, 0.86f, 1f), 0.08f).SetEase(Ease.OutQuad));
            _squashSequence.Append(m_AnimateRoot.DOScale(new Vector3(0.9f, 1.12f, 1f), 0.10f).SetEase(Ease.OutQuad));
            _squashSequence.Append(m_AnimateRoot.DOScale(Vector3.one, m_SettleDuration).SetEase(Ease.OutElastic));
        }

        private void SpawnRipple(float targetScale, float startAlpha, float duration)
        {
            if (m_RippleTemplate == null) return;

            Transform parent = m_RippleParent != null ? m_RippleParent : m_RippleTemplate.transform.parent;
            Image ring = Instantiate(m_RippleTemplate, parent);
            ring.transform.SetAsFirstSibling();
            ring.gameObject.SetActive(true);
            ring.transform.localScale = Vector3.one * 0.4f;

            Color c = ring.color;
            c.a = startAlpha;
            ring.color = c;

            Sequence s = DOTween.Sequence().SetUpdate(true);
            s.Append(ring.transform.DOScale(targetScale, duration).SetEase(Ease.OutCubic));
            s.Join(ring.DOFade(0f, duration).SetEase(Ease.OutQuad));
            s.OnComplete(() => Destroy(ring.gameObject));
        }
    }
}
