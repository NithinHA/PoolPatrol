using DG.Tweening;
using UnityEngine;
using Weapon;

public class Crosshair : MonoBehaviour
{
    [SerializeField] private Transform m_Gfx;
    [SerializeField] private SpriteRenderer m_SpriteRenderer;
    [SerializeField] private Color m_StartColor;
    [SerializeField] private Color m_EndColor;
    [SerializeField] private Color m_HitColor;

    private Material _material;

    private static readonly int DotSizeProperty = Shader.PropertyToID("_DotSize");
    private static readonly int RingCrossCutoutProperty = Shader.PropertyToID("_RingCrossCutout");
    private static readonly int RingDistanceProperty = Shader.PropertyToID("_RingDistance");
    private static readonly int LineLengthMinProperty = Shader.PropertyToID("_LineLengthMin");
    private static readonly int LineLengthMaxProperty = Shader.PropertyToID("_LineLengthMax");
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");

    private Bullet _bullet;
    private bool _isBulletAlive = false;
    private bool _isAnimating = false;

    private Sequence _generalAnimationSequence;
    private readonly float _generalAnimationDuration = .6f;
    private Sequence _hitTween;
    private readonly float _hitTweenDuration = .2f;

    private void Awake()
    {
        _material = m_SpriteRenderer.material;
    }

    /// <summary>
    /// Resets all tweens and arbitrary values and is ready for use again.
    /// </summary>
    public void ResetCrosshair()
    {
        m_Gfx.DOKill();
        _generalAnimationSequence?.Kill();
        _hitTween?.Kill();

        // Reset state so it's fresh when taking from the pool
        _isBulletAlive = false;
        _isAnimating = false;
        _bullet = null;
        
        // Reset any visuals (like scale changed by DOTween)
        m_Gfx.localScale = Vector3.one;
        m_Gfx.localRotation = Quaternion.identity;
    }

    public void SubscribeToBullet(Bullet bullet)
    {
        _bullet = bullet;
        _isBulletAlive = true;
        _bullet.OnBulletImpact += AnimateOnHit;
        _bullet.OnBulletDestroy += OnBulletDestroy;
        GeneralLifetimeAnimation();
    }

    private void UnsubscribeFromBullet()
    {
        _isBulletAlive = false;
        _bullet.OnBulletImpact -= AnimateOnHit;
        _bullet.OnBulletDestroy -= OnBulletDestroy;
    }

    private void AnimateOut()
    {
        _generalAnimationSequence?.Kill();
        _hitTween?.Kill();

        m_Gfx.DOScale(0, .4f).SetEase(Ease.InSine)
            .OnComplete(() =>
            {
                if (CrosshairManager.Instance != null)
                {
                    CrosshairManager.Instance.ReleaseCrosshair(this);
                }
                else
                {
                    Destroy(this.gameObject);
                }
            });
    }

    private void GeneralLifetimeAnimation()
    {
        _generalAnimationSequence?.Kill();
        _generalAnimationSequence = DOTween.Sequence();

        // slowly tween rotate m_GFX.
        float startAngle = Random.Range(0f, 360f);
        float direction = Random.value > 0.5f ? 1f : -1f;
        m_Gfx.localRotation = Quaternion.Euler(0, 0, startAngle);
        _generalAnimationSequence.Join(m_Gfx.DORotate(new Vector3(0, 0, startAngle + (360f * direction)), 4f, RotateMode.FastBeyond360).SetEase(Ease.Linear));
        
        // tween material property "_DotSize" from 0 to 0.05
        _material.SetFloat(DotSizeProperty, 0f);
        _generalAnimationSequence.Join(_material.DOFloat(0.05f, DotSizeProperty, _generalAnimationDuration).SetEase(Ease.InOutSine));

        // tween material property "_RingCrossCutout" from 0.05 to 0.12
        _material.SetFloat(RingCrossCutoutProperty, 0.05f);
        _generalAnimationSequence.Join(_material.DOFloat(0.12f, RingCrossCutoutProperty, _generalAnimationDuration).SetEase(Ease.InOutSine));

        // tween material property "_RingDistance" from 0.4 to 0.21
        _material.SetFloat(RingDistanceProperty, 0.4f);
        _generalAnimationSequence.Join(_material.DOFloat(0.21f, RingDistanceProperty, _generalAnimationDuration).SetEase(Ease.InOutSine));
                                     
        // tween material property "_LineLengthMin" from 0.08 to  0.15
        _material.SetFloat(LineLengthMinProperty, 0.08f);
        _generalAnimationSequence.Join(_material.DOFloat(0.15f, LineLengthMinProperty, _generalAnimationDuration).SetEase(Ease.InOutSine));

        // tween material property "_LineLengthMax" from 0.08 to 0.35
        _material.SetFloat(LineLengthMaxProperty, 0.08f);
        _generalAnimationSequence.Join(_material.DOFloat(0.35f, LineLengthMaxProperty, _generalAnimationDuration).SetEase(Ease.InOutSine));

        // tween material property "_Color" from m_StartColor to m_EndColor;
        _material.SetColor(ColorProperty, m_StartColor);
        _generalAnimationSequence.Join(_material.DOColor(m_EndColor, ColorProperty, _generalAnimationDuration).SetEase(Ease.InOutSine));
    }

#region Event listeners
    
    private void AnimateOnHit(BulletSource bulletSource)
    {
        _isAnimating = true;
        
        // speed up to the final stage of GeneralAnimate
        _generalAnimationSequence.timeScale = 5f;
        _hitTween?.Kill();
        _hitTween = DOTween.Sequence();

        // quick tween material property "_Color" to m_HitColor.
        _hitTween.Join(_material.DOColor(m_HitColor, ColorProperty, .1f));
        // tween a pop effect where scale increases slightly and returns back to same value.
        _hitTween.Join(m_Gfx.DOPunchScale(Vector3.one * 0.8f, _hitTweenDuration, 10, 1f));
        
        _hitTween.OnComplete(() =>
        {
            _isAnimating = false;
            if(!_isBulletAlive)
                AnimateOut();
        });
    }

    private void OnBulletDestroy()
    {
        UnsubscribeFromBullet();
        if (!_isAnimating)
            AnimateOut();
    }

#endregion
}
