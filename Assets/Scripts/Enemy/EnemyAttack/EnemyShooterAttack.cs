using System;
using DG.Tweening;
using UnityEngine;
using Movement;
using UnityEditor;

namespace Enemy.Attack
{
    [RequireComponent(typeof(ImpulseMover))]
    public class EnemyShooterAttack : EnemyAttack
    {
        [SerializeField] private float m_ChargeTime = 1.5f;
        [SerializeField] private float m_CooldownTime = 2f;
        [SerializeField] private float m_WeaponRotationSpeed = 180;
        [Space]
        [SerializeField] private float m_AlertRadius = 5f;
        [SerializeField] private SpriteRenderer m_AlertDisk;
        [SerializeField] private Color m_DiskNormalColor = Color.gray;
        [SerializeField] private Color m_DiskAlertColor = Color.yellow;
        [SerializeField] private float m_DiskScaleAnimDuration = .4f;

        public enum EnemyAttackState
        {
            Idle,
            Charging,
            Firing,
            Cooldown
        }

        private EnemyAttackState _state;
        private Transform _targetPlayer;
        private float _stateTimer;
        private Vector2 _aimDir;
        private ImpulseMover _mover;
        private Tween _alertDiskScaleTween;

        public Action<EnemyAttackState, EnemyAttackState> OnStateChange;

        protected override void Awake()
        {
            base.Awake();
            _mover = GetComponent<ImpulseMover>();
            UpdateDiskColor(false);
            AnimateDiskScale();

            OnStateChange += OnStateChangeHandleDiskColor;
        }

        private void OnDestroy()
        {
            OnStateChange -= OnStateChangeHandleDiskColor;
            if (_alertDiskScaleTween != null && _alertDiskScaleTween.IsActive() && _alertDiskScaleTween.IsPlaying())
                _alertDiskScaleTween.Kill();
        }

        public override void Tick()
        {
            switch (_state)
            {
                case EnemyAttackState.Idle: CheckForPlayer(); break;
                case EnemyAttackState.Charging: Charging(); break;
                case EnemyAttackState.Firing: Fire(); break;
                case EnemyAttackState.Cooldown: Cooldown(); break;
            }
        }

#region Tick States
        
        void CheckForPlayer()
        {
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");     // must change this!
            float closestDist = Mathf.Infinity;

            foreach (var player in players)
            {
                float dist = Vector2.Distance(player.transform.position, transform.position);
                if (dist < m_AlertRadius && dist < closestDist)
                {
                    _targetPlayer = player.transform;
                    closestDist = dist;
                }
            }

            if (_targetPlayer != null)
            {
                _aimDir = (_targetPlayer.position - transform.position).normalized;
                ChangeAttackState(EnemyAttackState.Charging);
                _stateTimer = m_ChargeTime;
            }
        }

        void Charging()
        {
            if (!_targetPlayer || Vector2.Distance(_targetPlayer.position, transform.position) > m_AlertRadius)
            {
                _targetPlayer = null;
                ChangeAttackState(EnemyAttackState.Idle);
                return;
            }
            
            RotateWeaponTowardsTarget();
            
            _stateTimer -= Time.deltaTime;
            if (_stateTimer <= 0f)
            {
                ChangeAttackState(EnemyAttackState.Firing);
            }
        }

        void Fire()
        {
            m_EnemyWeaponController.FireWeapon(_aimDir);
            _mover.ApplyImpulse(-_aimDir);

            _stateTimer = m_CooldownTime;
            ChangeAttackState(EnemyAttackState.Cooldown);
        }

        void Cooldown()
        {
            if (_targetPlayer == null || Vector2.Distance(_targetPlayer.position, transform.position) > m_AlertRadius)
            {
                _targetPlayer = null;
                ChangeAttackState(EnemyAttackState.Idle);
                return;
            }

            RotateWeaponTowardsTarget();
            _stateTimer -= Time.deltaTime;
            if (_stateTimer <= 0f)
            {
                ChangeAttackState(EnemyAttackState.Charging);
            }
        }

#endregion

        private void ChangeAttackState(EnemyAttackState newState)
        {
            var oldState = _state;
            _state = newState;
            OnStateChange?.Invoke(oldState, _state);
        }
        
        private void RotateWeaponTowardsTarget()
        {
            _aimDir = (_targetPlayer.position - transform.position).normalized;
// Step 1: Compute desired angle from Vector2.right to aim direction
            float targetAngle = Vector2.SignedAngle(Vector2.right, _aimDir) + 180f;
// Step 2: Get current weapon rotation (z angle in degrees)
            float currentAngle = m_EnemyWeaponController.transform.eulerAngles.z;
// Step 3: Rotate smoothly
            float maxStep = m_WeaponRotationSpeed * Time.deltaTime;
            float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, maxStep);
            m_EnemyWeaponController.transform.rotation = Quaternion.Euler(0, 0, newAngle);
        }

        private void AnimateDiskScale()
        {
            m_AlertDisk.transform.localScale = Vector3.zero;
            float diskScale = m_AlertRadius * 2;
            _alertDiskScaleTween = m_AlertDisk.transform.DOScale(diskScale, m_DiskScaleAnimDuration)
                .SetEase(Ease.OutBack).OnComplete(() => _alertDiskScaleTween = null);
        }

        private void UpdateDiskColor(bool isAlert)
        {
            m_AlertDisk.color = isAlert ? m_DiskAlertColor : m_DiskNormalColor;
        }

#region Event listeners

        private void OnStateChangeHandleDiskColor(EnemyAttackState oldState, EnemyAttackState newState)
        {
            if (newState == EnemyAttackState.Charging && oldState == EnemyAttackState.Idle)
                UpdateDiskColor(true);
            else if (newState == EnemyAttackState.Idle && oldState != EnemyAttackState.Idle)
                UpdateDiskColor(false);
        }

#endregion
        
#if UNITY_EDITOR
#region Debug
        
        private void OnDrawGizmos()
        {
            Handles.color = _targetPlayer != null ? Color.yellow : Color.green;
            Handles.DrawWireDisc(transform.position, Vector3.forward, m_AlertRadius);

            if (_targetPlayer != null)
                Handles.DrawLine(transform.position, _targetPlayer.position + (Vector3)_aimDir);
        }

#endregion
#endif
    }
}