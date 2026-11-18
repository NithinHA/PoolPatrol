using System;
using DG.Tweening;
using UnityEngine;

namespace Economy.UI
{
    /// <summary>
    /// The icon half of a currency wallet, shared by the gem and coin wallets. It is the point
    /// collectables fly to and the thing that shakes when one arrives:
    ///
    /// <list type="bullet">
    /// <item><see cref="m_Backdrop"/> — the fixed frame behind the icon. Its position is the flight
    /// destination, so it keeps working whatever sprite or size the icon has.</item>
    /// <item><see cref="m_Icon"/> — the graphic that shakes. Swap its sprite freely, or point this at a
    /// different object entirely; nothing else needs to change.</item>
    /// </list>
    ///
    /// A wallet owns one of these as a serialized field and calls <see cref="Enable"/> /
    /// <see cref="Disable"/> from OnEnable / OnDisable.
    /// </summary>
    [Serializable]
    public class CollectionIconTarget
    {
        [Tooltip("Frame behind the icon. Collectables fly to its position. Falls back to the icon.")]
        [SerializeField] private RectTransform m_Backdrop;
        [Tooltip("The graphic that shakes when a collectable arrives.")]
        [SerializeField] private RectTransform m_Icon;

        [Header("Shake")]
        [SerializeField] private float m_ShakeAngle = 22f;
        [SerializeField] private float m_ShakeScale = 0.25f;
        [SerializeField] private float m_ShakeDuration = 0.3f;
        [SerializeField] private int m_ShakeVibrato = 10;

        private CollectionTargetId _id;
        private Tween _tween;

        /// <summary>The frame collectables fly to (the icon itself if no backdrop is assigned).</summary>
        public RectTransform Anchor => m_Backdrop != null ? m_Backdrop : m_Icon;

        /// <summary>
        /// Registers this icon as the destination for <paramref name="id"/> and starts listening for
        /// arrivals. <paramref name="uiPosition"/> overrides the flight destination when the wallet
        /// is not where it will end up (see <see cref="CollectionTargets.Register"/>).
        /// </summary>
        public void Enable(CollectionTargetId id, Func<Vector3> uiPosition = null)
        {
            _id = id;
            if (Anchor != null)
                CollectionTargets.Register(id, Anchor, uiPosition);
            CollectionTargets.OnArrived += OnArrived;
        }

        public void Disable()
        {
            CollectionTargets.OnArrived -= OnArrived;
            CollectionTargets.Unregister(_id);
            _tween?.Kill();
            _tween = null;
        }

        private void OnArrived(CollectionTargetId id)
        {
            if (id == _id)
                Shake();
        }

        /// <summary>One quick shake: a rotation wobble with a small scale bump.</summary>
        public void Shake()
        {
            RectTransform target = m_Icon != null ? m_Icon : Anchor;
            if (target == null)
                return;

            // Complete (not Kill) so back-to-back pickups restart from the rest pose.
            _tween?.Complete();

            Sequence seq = DOTween.Sequence();
            seq.Join(target.DOPunchRotation(new Vector3(0f, 0f, m_ShakeAngle), m_ShakeDuration, m_ShakeVibrato, 0.8f));
            seq.Join(target.DOPunchScale(Vector3.one * m_ShakeScale, m_ShakeDuration, m_ShakeVibrato, 0.6f));
            // Unscaled: pickups can land while the Goddess shop has time frozen.
            seq.SetUpdate(true);
            _tween = seq;
        }
    }
}
