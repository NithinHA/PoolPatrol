using System;
using Movement;
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

        public Action<Vector2, Vector2> OnFireInput;

        private Camera _mainCam;

#region Unity callbacks
        
        private void Awake()
        {
            // in multiplayer scenario, ImpulseMover should be attached to the common floatie instead of PlayerController.
            _impulseMover = GetComponent<ImpulseMover>();
            _impulseMover.AssignParticleEmitter(WaterRippleParticleEmitter);
            _mainCam = Camera.main;
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
            _impulseMover.Tick();

            if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
            {
                Vector2 pointerPos = Pointer.current.position.ReadValue();
                Vector3 worldPos = _mainCam.ScreenToWorldPoint(pointerPos);
                Vector2 fireDir = ((Vector2)worldPos - (Vector2)transform.position).normalized;
                
                OnFireInput?.Invoke(fireDir, worldPos);
                _impulseMover.ApplyImpulse(-fireDir);
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
