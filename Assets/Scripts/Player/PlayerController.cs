using System;
using Movement;
using UnityEngine;

namespace Player
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Player references")]
        public PlayerHealth PlayerHealth;
        public ParticleEmitter WaterRippleParticleEmitter;

        private ImpulseMover _impulseMover;
        public Action<Vector2> OnFireInput;

        private Camera _mainCam;

#region Unity callbacks
        
        private void Awake()
        {
            // in multiplayer scenario, ImpulseMover should be attached to the common floatie instead of PlayerController.
            _impulseMover = GetComponent<ImpulseMover>();
            _impulseMover.AssignParticleEmitter(WaterRippleParticleEmitter);
            _mainCam = Camera.main;
        }

        void Update()
        {
            _impulseMover.Tick();

#if UNITY_EDITOR
             if (Input.GetMouseButtonDown(0))
             {
                 Vector2 fireDir = (_mainCam.ScreenToWorldPoint(Input.mousePosition) - transform.position).normalized;
                 OnFireInput?.Invoke(fireDir);
                 _impulseMover.ApplyImpulse(-fireDir);
             }
#elif UNITY_ANDROID || UNITY_IOS
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                Vector2 touchPosition = _mainCam.ScreenToWorldPoint(Input.GetTouch(0).position);
                Vector2 fireDir = (touchPosition - (Vector2)transform.position).normalized;
                OnFireInput?.Invoke(fireDir);
                _impulseMover.ApplyImpulse(-fireDir);
            }
#endif
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
