using DG.Tweening;
using UnityEngine;

namespace Enemy.Movement
{
    public class EnemyNoMovement : EnemyMovement
    {
        [SerializeField] private float m_WaveRepeatDelay = 2f;
        [SerializeField] private float m_DipMagnitude = .2f;
        [SerializeField] private float m_DipDuration = .4f;

        private float _nextWaveTime = 0;
        private Tween _dipAnimationTween;

        public override void Tick()
        {
            // periodically make a wave animation
            if (_nextWaveTime <= Time.time)
            {
                MakeWave();
                _nextWaveTime = Time.time + m_WaveRepeatDelay;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_dipAnimationTween != null && _dipAnimationTween.IsActive() && _dipAnimationTween.IsPlaying())
                _dipAnimationTween.Kill();
        }

        public override void FixedTick()
        {
            
        }

        private void MakeWave()
        {
            _dipAnimationTween = transform.DOMoveY(transform.position.y - m_DipMagnitude, m_DipDuration).SetEase(Ease.InSine).OnComplete(
                () =>
                {
                    Controller.WaterRippleParticleEmitter.EmitParticles();
                    _dipAnimationTween = transform.DOMoveY(transform.position.y + m_DipMagnitude, m_DipDuration).SetEase(Ease.OutSine);
                });
        }
    }
}