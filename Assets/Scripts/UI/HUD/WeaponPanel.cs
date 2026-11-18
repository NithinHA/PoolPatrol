using System.Collections.Generic;
using DG.Tweening;
using PTL.Framework;
using Player;
using UnityEngine;
using UnityEngine.UI;
using Weapon;

namespace UI.HUD
{
    public class WeaponPanel : MonoBehaviour
    {
        [SerializeField] private BulletIcon      m_BulletIconPrefab;
        [SerializeField] private Transform       m_BulletContainer;   // BulletRingLayout parent under WeaponIndicator
        [SerializeField] private Image           m_WeaponIcon;        // Swapped when weapon changes
        [SerializeField] private CooldownIndicator m_CooldownIndicator;

        [Header("Recoil (weapon icon blast per shot)")]
        [Tooltip("Scale the icon snaps to the instant a shot fires.")]
        [SerializeField] private float m_RecoilPeakScale = 1.4f;
        [Tooltip("Rotation (degrees) the icon snaps to, muzzle kicking upward.")]
        [SerializeField] private float m_RecoilAngle = 16f;
        [Tooltip("Canvas units the icon is thrown back along the barrel's opposite direction.")]
        [SerializeField] private Vector2 m_RecoilOffset = new Vector2(-12f, -8f);
        [SerializeField] private float m_RecoilSnapDuration = 0.05f;
        [SerializeField] private float m_RecoilSettleDuration = 0.35f;

        [Header("Reloading (weapon icon)")]
        [Tooltip("While reloading, the icon's alpha breathes between these on a sine.")]
        [SerializeField, Range(0f, 1f)] private float m_ReloadAlphaMin = 0.5f;
        [SerializeField, Range(0f, 1f)] private float m_ReloadAlphaMax = 0.85f;
        [Tooltip("Seconds for one half of the breath (max → min).")]
        [SerializeField] private float m_ReloadAlphaHalfPeriod = 0.45f;
        [Tooltip("Scale the icon pops to when the reload completes.")]
        [SerializeField] private float m_ReloadPopScale = 1.3f;
        [SerializeField] private float m_ReloadPopDuration = 0.35f;

        private WeaponMagazine _magazine;
        private Tween _recoilTween;
        private Tween _reloadPopTween;

        // Reload alpha pulse. Alpha goes through the icon's CanvasRenderer (no mesh rebuild, and it
        // multiplies over the Image colour rather than replacing it); the tween delegates are cached
        // so starting/stopping the pulse allocates nothing beyond the tween itself.
        private Tween _reloadPulseTween;
        // The pulse tweens its own 0..1 phase (mapped onto max→min alpha), not the alpha itself:
        // DOTween reads a getter tween's start value lazily, so tweening alpha directly would pick up
        // the full-alpha hold of a shot that lands before the pulse's first update.
        private float _pulsePhase;
        private DG.Tweening.Core.DOGetter<float> _pulseGetter;
        private DG.Tweening.Core.DOSetter<float> _pulseSetter;
        private TweenCallback _resumeReloadPulse;
        private ReloadStyle _reloadStyle;
        private readonly List<BulletIcon> _icons = new List<BulletIcon>();

        private bool _pollReload;   // True only while a reload is in progress — gates the Update poll.

        private PlayerWeaponController _weaponController;

        private void Awake()
        {
            _pulseGetter = () => _pulsePhase;
            _pulseSetter = SetPulsePhase;
            _resumeReloadPulse = ResumeReloadPulse;
        }

        private void Start()
        {
            PlayerController localPlayer = PlayerController.Local;
            if (localPlayer == null)
            {
                Debug.LogWarning("[WeaponPanel] PlayerController.Local is null. " +
                                 "Ensure PlayerController.Local is set before this Start().");
                return;
            }

            _weaponController = localPlayer.PlayerWeaponController;
            _weaponController.OnWeaponChanged += BindWeapon;

            // Bind whatever is active right now — correct whether the loadout applier has already
            // switched weapons (Start-order dependent) or switches later, since BindWeapon also runs
            // off OnWeaponChanged above.
            BindWeapon(_weaponController.ActiveWeapon);
        }

        private void OnDestroy()
        {
            if (_weaponController != null)
                _weaponController.OnWeaponChanged -= BindWeapon;
            UnbindMagazine();
            _recoilTween?.Kill();
            _reloadPopTween?.Kill();
            _reloadPulseTween?.Kill();
        }

        private void Update()
        {
            if (!_pollReload || _magazine == null)
                return;

            float t = _magazine.ReloadProgress;

            switch (_reloadStyle)
            {
                case ReloadStyle.MagazineAtOnce:
                    // Drive all spent icons simultaneously.
                    for (int i = _magazine.CurrentAmmo; i < _icons.Count; i++)
                        _icons[i].SetReloadProgress(t);
                    break;

                case ReloadStyle.BoltAction:
                    // Drive only the bullet currently being loaded
                    // (the one just above the last fully loaded ammo count).
                    int loadingIndex = _magazine.CurrentAmmo; // 0-based index of bullet in progress
                    if (loadingIndex >= 0 && loadingIndex < _icons.Count)
                        _icons[loadingIndex].SetReloadProgress(t);
                    break;
            }
        }

        /// <summary>
        /// Binds to a new weapon (called on Start and on weapon switch).
        /// Unsubscribes from the old magazine first.
        /// </summary>
        public void BindWeapon(WeaponBase weapon)
        {
            UnbindMagazine();

            if (weapon == null) return;

            _magazine     = weapon.Magazine;
            _reloadStyle  = _magazine.ReloadStyle;

            RebuildIcons(_magazine.MagazineSize, _magazine.CurrentAmmo);
            UpdateWeaponIcon(weapon);

            _magazine.OnBulletFired += OnBulletFired;
            _magazine.OnReloadStarted += OnReloadStarted;
            _magazine.OnBulletReloaded += OnBulletReloaded;
            _magazine.OnReloadComplete += OnReloadComplete;
            _magazine.OnMagazineSizeChanged += OnMagazineSizeChanged;
            _magazine.OnDryFire += OnDryFire;

            m_CooldownIndicator?.Bind(_magazine);

            // A weapon swapped in mid-reload picks the pulse up straight away.
            if (_magazine.IsReloading)
                StartReloadPulse();
        }

        /// <summary>
        /// Looks up the loadout catalog entry matching this weapon's Kind and swaps the HUD portrait to
        /// its icon. No-op (keeps whatever icon is currently shown) if the catalog or a match isn't
        /// found, so the panel still degrades gracefully without the Loadout service.
        /// </summary>
        private void UpdateWeaponIcon(WeaponBase weapon)
        {
            if (m_WeaponIcon == null) return;

            var loadout = ServiceLocator.GetLoadoutService();
            if (loadout == null) return;

            foreach (var definition in loadout.Weapons)
            {
                if (definition.Kind == weapon.Kind)
                {
                    if (definition.Icon != null) m_WeaponIcon.sprite = definition.Icon;
                    return;
                }
            }
        }

        private void UnbindMagazine()
        {
            if (_magazine == null) return;

            _magazine.OnBulletFired -= OnBulletFired;
            _magazine.OnReloadStarted -= OnReloadStarted;
            _magazine.OnBulletReloaded -= OnBulletReloaded;
            _magazine.OnReloadComplete -= OnReloadComplete;
            _magazine.OnMagazineSizeChanged -= OnMagazineSizeChanged;
            _magazine.OnDryFire -= OnDryFire;

            m_CooldownIndicator?.Bind(null);
            StopReloadPulse();
            _magazine = null;
        }

        /// <summary>
        /// Capacity changed at runtime (e.g. "Extended Mag" purchased in the Goddess shop).
        /// RebuildIcons only adds/removes the delta and leaves existing icons' state alone, so
        /// this is safe to call mid-reload. Plain Instantiate + direct SetLive/SetSpent calls, no
        /// tweens, so it reflects instantly even while the shop has Time.timeScale frozen.
        /// </summary>
        private void OnMagazineSizeChanged(int newSize)
        {
            RebuildIcons(newSize, _magazine.CurrentAmmo);
        }

        private void RebuildIcons(int magazineSize, int currentAmmo)
        {
            // Remove excess icons
            while (_icons.Count > magazineSize)
            {
                Destroy(_icons[^1].gameObject);
                _icons.RemoveAt(_icons.Count - 1);
            }

            // Add missing icons
            while (_icons.Count < magazineSize)
            {
                BulletIcon icon = Instantiate(m_BulletIconPrefab, m_BulletContainer);
                _icons.Add(icon);
            }

            // Set initial state without animation
            for (int i = 0; i < _icons.Count; i++)
            {
                if (i < currentAmmo) 
                {
                    _icons[i].SetLive();
                }
                else 
                {
                    _icons[i].SetSpent();
                    if (_magazine.IsReloading)
                        _icons[i].BeginReload(_reloadStyle);
                }
            }

            _pollReload = _magazine.IsReloading;
        }

        private void OnBulletFired(int newAmmo)
        {
            int spentIndex = newAmmo;
            if (spentIndex >= 0 && spentIndex < _icons.Count)
                _icons[spentIndex].Fire();

            // Shooting mid-reload (BoltAction): the breathing pauses at full alpha for the kick and
            // picks back up when the recoil settles.
            PlayRecoil();
            PauseReloadPulse();

            if (_pollReload && _reloadStyle == ReloadStyle.BoltAction)
            {
                // Reset visual for bullet that was being loaded (gap shifted)
                int interruptedIndex = newAmmo + 1; 
                if (interruptedIndex < _icons.Count)
                    _icons[interruptedIndex].SetSpent();

                // Start reload visual for newly fired slot
                if (spentIndex >= 0 && spentIndex < _icons.Count)
                    _icons[spentIndex].BeginReload(_reloadStyle);
            }
        }

        /// <summary>
        /// Explosive kick on the weapon portrait: it snaps big, tilted and thrown back in a few frames,
        /// then springs home with an elastic wobble. Every shot restarts it from rest.
        /// </summary>
        private void PlayRecoil()
        {
            if (m_WeaponIcon == null)
                return;

            // Complete (not Kill) so rapid fire always restarts from the rest pose.
            _recoilTween?.Complete();
            _reloadPopTween?.Complete();

            RectTransform icon = m_WeaponIcon.rectTransform;
            Vector2 rest = icon.anchoredPosition;

            Sequence seq = DOTween.Sequence();
            seq.Append(icon.DOScale(m_RecoilPeakScale, m_RecoilSnapDuration).SetEase(Ease.OutQuad));
            seq.Join(icon.DOLocalRotate(new Vector3(0f, 0f, m_RecoilAngle), m_RecoilSnapDuration).SetEase(Ease.OutQuad));
            seq.Join(icon.DOAnchorPos(rest + m_RecoilOffset, m_RecoilSnapDuration).SetEase(Ease.OutQuad));
            seq.Append(icon.DOScale(1f, m_RecoilSettleDuration).SetEase(Ease.OutElastic, 1.2f, 0.4f));
            seq.Join(icon.DOLocalRotate(Vector3.zero, m_RecoilSettleDuration).SetEase(Ease.OutElastic, 1.2f, 0.4f));
            seq.Join(icon.DOAnchorPos(rest, m_RecoilSettleDuration).SetEase(Ease.OutElastic, 1.2f, 0.4f));
            seq.OnComplete(_resumeReloadPulse);
            _recoilTween = seq;
        }

#region Reload visuals (weapon icon)

        private void SetIconAlpha(float alpha)
        {
            if (m_WeaponIcon != null)
                m_WeaponIcon.canvasRenderer.SetAlpha(alpha);
        }

        private void SetPulsePhase(float phase)
        {
            _pulsePhase = phase;
            SetIconAlpha(Mathf.Lerp(m_ReloadAlphaMax, m_ReloadAlphaMin, phase));
        }

        private bool RecoilPlaying => _recoilTween != null && _recoilTween.IsActive() && _recoilTween.IsPlaying();

        /// <summary>Starts the reload breathing; if a shot is still kicking, it waits for the kick to settle.</summary>
        private void StartReloadPulse()
        {
            if (m_WeaponIcon == null)
                return;

            _reloadPulseTween?.Kill();
            SetPulsePhase(0f);
            _reloadPulseTween = DOTween.To(_pulseGetter, _pulseSetter, 1f, m_ReloadAlphaHalfPeriod)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);

            if (RecoilPlaying)
                PauseReloadPulse();
        }

        /// <summary>Holds the breathing at full alpha (a shot is kicking). No-op when not reloading.</summary>
        private void PauseReloadPulse()
        {
            if (_reloadPulseTween == null || !_reloadPulseTween.IsActive())
                return;

            _reloadPulseTween.Pause();
            SetIconAlpha(1f);
        }

        /// <summary>Recoil settled: carry on breathing from where it paused, if still reloading.</summary>
        private void ResumeReloadPulse()
        {
            if (_reloadPulseTween != null && _reloadPulseTween.IsActive() && _magazine != null && _magazine.IsReloading)
                _reloadPulseTween.Play();
        }

        private void StopReloadPulse()
        {
            _reloadPulseTween?.Kill();
            _reloadPulseTween = null;
            SetIconAlpha(1f);
        }

        /// <summary>Reload done: full alpha and a scale pop so the "ready" moment reads.</summary>
        private void PlayReloadPop()
        {
            if (m_WeaponIcon == null)
                return;

            _recoilTween?.Complete();
            _reloadPopTween?.Complete();

            RectTransform icon = m_WeaponIcon.rectTransform;
            Sequence seq = DOTween.Sequence();
            seq.Append(icon.DOScale(m_ReloadPopScale, m_ReloadPopDuration * 0.35f).SetEase(Ease.OutQuad));
            seq.Append(icon.DOScale(1f, m_ReloadPopDuration * 0.65f).SetEase(Ease.OutBack, 2.5f));
            _reloadPopTween = seq;
        }

        private void OnDryFire()
        {
            for (int i = 0; i < _icons.Count; i++)
                _icons[i].DryFire();
        }

#endregion

        private void OnReloadStarted()
        {
            _pollReload = true;
            StartReloadPulse();

            if (_magazine == null) return;
            for (int i = _magazine.CurrentAmmo; i < _icons.Count; i++)
                _icons[i].BeginReload(_reloadStyle);
        }

        private void OnBulletReloaded(int newAmmo)
        {
            int completedIndex = newAmmo - 1;
            if (completedIndex >= 0 && completedIndex < _icons.Count)
                _icons[completedIndex].CompleteReload();

            // Ensure next slot has fill active (might have been hidden by a previous interrupt)
            if (_magazine != null && _magazine.IsReloading && newAmmo < _icons.Count)
                _icons[newAmmo].BeginReload(_reloadStyle);
        }

        private void OnReloadComplete(int newAmmo)
        {
            _pollReload = false;
            for (int i = 0; i < _icons.Count; i++)
                _icons[i].CompleteReload();

            StopReloadPulse();
            PlayReloadPop();
        }
    }
}
