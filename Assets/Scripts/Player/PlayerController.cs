using System;
using Movement;
using PTL.Framework;
using PTL.Framework.Services;
using UnityEngine;
using UnityEngine.InputSystem;
using Weapon;

namespace Player
{
    public class PlayerController : MonoBehaviour
    {
        /// <summary>
        /// The local player on this device.
        /// Set by LevelManager.SetLocalPlayer() at game-start.
        /// In multiplayer, only the locally-controlled PlayerController has IsLocalPlayer = true.
        /// </summary>
        public static PlayerController Local { get; private set; }
        public bool IsLocalPlayer { get; private set; }

        [Header("Player references")]
        public PlayerHealth PlayerHealth;
        public PlayerWeaponController PlayerWeaponController;
        public RippleCausingParticleEmitter WaterRippleParticleEmitter;
        public PlayerCombo PlayerCombo;

        private ImpulseMover _impulseMover;
        public ImpulseMover ImpulseMover => _impulseMover;

        public Action<Vector2> OnPointerDownEvent;
        public Action<Vector2> OnPointerUpdateEvent;
        public Action<Vector2, Vector2> OnHoldStartEvent;
        public Action<Vector2, Vector2> OnHoldUpdateEvent;
        public Action<Vector2, Vector2> OnFireReleaseEvent;

        [Header("Input Settings")]
        public float HoldThreshold = 0.15f;
        private bool _isPointerDown;
        private bool _isHolding;
        private float _pointerDownTime;
        private Vector2 _currentPointerPos;

        private Camera _mainCam;
        private IPauseService _pause;

#region Unity callbacks
        
        private void Awake()
        {
            // in multiplayer scenario, ImpulseMover should be attached to the common floatie instead of PlayerController.
            _impulseMover = GetComponent<ImpulseMover>();
            _impulseMover.AssignParticleEmitter(WaterRippleParticleEmitter);
            _mainCam = Camera.main;
        }

        private void Start()
        {
            _pause = ServiceLocator.GetPauseService();
        }

        /// <summary>
        /// Designates this instance as the local player for this session.
        /// </summary>
        public void SetLocal()
        {
            Local = this;
            IsLocalPlayer = true;
        }

        private void OnDestroy()
        {
            if (Local == this) Local = null;
        }

        void Update()
        {
            // A timescale freeze stops physics and every deltaTime-driven system, but Update still
            // runs - so input has to be gated explicitly or the player could shoot from inside the
            // Goddess shop. Any half-finished press is dropped so releasing over the UI cannot fire.
            if (_pause != null && _pause.IsPaused)
            {
                _isPointerDown = false;
                _isHolding = false;
                return;
            }

            _impulseMover.Tick();

            if (Pointer.current != null)
            {
                if (Pointer.current.press.wasPressedThisFrame)
                {
                    _isPointerDown = true;
                    _isHolding = false;
                    _pointerDownTime = Time.time;
                    _currentPointerPos = Pointer.current.position.ReadValue();
                    Vector3 worldPos = _mainCam.ScreenToWorldPoint(_currentPointerPos);
                    OnPointerDownEvent?.Invoke(worldPos);
                }

                if (_isPointerDown)
                {
                    _currentPointerPos = Pointer.current.position.ReadValue();
                    Vector3 worldPos = _mainCam.ScreenToWorldPoint(_currentPointerPos);
                    Vector2 fireDir = ((Vector2)worldPos - (Vector2)transform.position).normalized;

                    OnPointerUpdateEvent?.Invoke(worldPos);

                    if (!_isHolding && Time.time - _pointerDownTime >= HoldThreshold)
                    {
                        _isHolding = true;
                        OnHoldStartEvent?.Invoke(fireDir, worldPos);
                    }

                    if (_isHolding)
                    {
                        OnHoldUpdateEvent?.Invoke(fireDir, worldPos);
                    }

                    if (Pointer.current.press.wasReleasedThisFrame)
                    {
                        _isPointerDown = false;
                        _isHolding = false;
                        OnFireReleaseEvent?.Invoke(fireDir, worldPos);
                        _impulseMover.ApplyImpulse(-fireDir);
                    }
                }
            }
        }

        private void FixedUpdate()
        {
            _impulseMover.FixedTick();
        }

        private void OnCollisionEnter2D(Collision2D other)
        {
            if (other.gameObject.CompareTag(Constants.GameConstants.TAG_Enemy))
            {
                PlayerHealth.TakeDamage();
            }
        }

#endregion
    }
}
