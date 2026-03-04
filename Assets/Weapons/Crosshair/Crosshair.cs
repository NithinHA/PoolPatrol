using DG.Tweening;
using Pooling;
using UnityEngine;
using UnityEngine.Serialization;
using Weapon;

public class Crosshair : MonoBehaviour, IPoolableObject
{
    [SerializeField] private Transform m_Gfx;
    [SerializeField] private Renderer m_Renderer;
    [FormerlySerializedAs("m_StartColor")] [SerializeField] private Color m_DefaultColor;
    [SerializeField] private Color m_HitColor;
    [Header("Lifetime Anim Values")]
    // Define target values
    [SerializeField] private float m_TargetDotSize = 0.05f;
    [SerializeField] private float m_TargetRingCrossCutout = 0.12f;
    [SerializeField] private float m_TargetRingDistance = 0.28f;
    [SerializeField] private float m_TargetLineLengthMin = 0.15f;
    [SerializeField] private float m_TargetLineLengthMax = 0.35f;
    [Space]
    // Initial values
    [SerializeField] private float m_StartDotSize = 0f;
    [SerializeField] private float m_StartRingCrossCutout = 0.05f;
    [SerializeField] private float m_StartRingDistance = 0.4f;
    [SerializeField] private float m_StartLineLengthMin = 0.08f;
    [SerializeField] private float m_StartLineLengthMax = 0.08f;

    private MaterialPropertyBlock _propBlock;

    private static readonly int DotSizeProperty = Shader.PropertyToID("_DotSize");
    private static readonly int RingCrossCutoutProperty = Shader.PropertyToID("_RingCrossCutout");
    private static readonly int RingDistanceProperty = Shader.PropertyToID("_RingDistance");
    private static readonly int LineLengthMinProperty = Shader.PropertyToID("_LineLengthMin");
    private static readonly int LineLengthMaxProperty = Shader.PropertyToID("_LineLengthMax");
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");

    private PoolableItemType _itemType;

    private Bullet _bullet;
    private bool _isBulletAlive = false;
    private bool _isAnimating = false;

    private Sequence _generalAnimationSequence;
    private readonly float _generalAnimationDuration = .6f;
    private Sequence _hitTween;
    private readonly float _hitTweenDuration = .2f;

    private void Awake()
    {
        _propBlock = new MaterialPropertyBlock();
        m_Renderer.GetPropertyBlock(_propBlock);
    }

    public void Initialize(PoolableItemType type)
    {
        _itemType = type;
        if (_propBlock == null)
        {
            _propBlock = new MaterialPropertyBlock();
            m_Renderer.GetPropertyBlock(_propBlock);
        }
    }

    /// <summary>
    /// Resets all tweens and state so the crosshair is ready for reuse.
    /// </summary>
    public void Reset()
    {
        m_Gfx.DOKill();
        _generalAnimationSequence?.Kill();
        _hitTween?.Kill();

        _isBulletAlive = false;
        _isAnimating = false;
        _bullet = null;

        m_Gfx.localScale = Vector3.one;
        m_Gfx.localRotation = Quaternion.identity;

        _propBlock.SetColor(ColorProperty, m_DefaultColor);
        m_Renderer.SetPropertyBlock(_propBlock);
    }

    public void ReturnToPool()
    {
        ObjectPoolManager.Instance.ReleaseItem(_itemType, this);
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
            .OnComplete(() => ReturnToPool());
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
        
        

        float t = 0;
        _generalAnimationSequence.Join(DOTween.To(() => t, x => {
            t = x;
            _propBlock.SetFloat(DotSizeProperty, Mathf.Lerp(m_StartDotSize, m_TargetDotSize, t));
            _propBlock.SetFloat(RingCrossCutoutProperty, Mathf.Lerp(m_StartRingCrossCutout, m_TargetRingCrossCutout, t));
            _propBlock.SetFloat(RingDistanceProperty, Mathf.Lerp(m_StartRingDistance, m_TargetRingDistance, t));
            _propBlock.SetFloat(LineLengthMinProperty, Mathf.Lerp(m_StartLineLengthMin, m_TargetLineLengthMin, t));
            _propBlock.SetFloat(LineLengthMaxProperty, Mathf.Lerp(m_StartLineLengthMax, m_TargetLineLengthMax, t));
            m_Renderer.SetPropertyBlock(_propBlock);
        }, 1f, _generalAnimationDuration).SetEase(Ease.InOutSine));
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
        _hitTween.Join(DOTween.To(
            () => _propBlock.HasColor(ColorProperty) ? _propBlock.GetColor(ColorProperty) : m_DefaultColor, 
            x => { _propBlock.SetColor(ColorProperty, x); m_Renderer.SetPropertyBlock(_propBlock); }, 
            m_HitColor, .1f));
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
