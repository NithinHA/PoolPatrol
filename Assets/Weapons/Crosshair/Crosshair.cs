using DG.Tweening;
using UnityEngine;
using Weapon;

public class Crosshair : MonoBehaviour
{
    [SerializeField] private Transform m_Gfx;
    [SerializeField] private SpriteRenderer m_SpriteRenderer;

    private Material _material;

    private Bullet _bullet;
    private bool _isBulletAlive = false;
    private bool _isAnimating = false;

    private void Awake()
    {
        _material = m_SpriteRenderer.material;
    }

    public void SubscribeToBullet(Bullet bullet)
    {
        _bullet = bullet;
        _isBulletAlive = true;
        _bullet.OnBulletImpact += AnimateOnHit;
        _bullet.OnBulletDestroy += OnBulletDestroy;
    }

    private void UnsubscribeFromBullet()
    {
        _isBulletAlive = false;
        _bullet.OnBulletImpact -= AnimateOnHit;
        _bullet.OnBulletDestroy -= OnBulletDestroy;
    }

    private void AnimateOut()
    {
        m_Gfx.DOScale(0, .4f).SetEase(Ease.InBack)
            .OnComplete(() => Destroy(this.gameObject));
    }

#region Event listeners
    
    private void AnimateOnHit(BulletSource bulletSource)
    {
        _isAnimating = true;
        /// perform hit animation.
        _isAnimating = false;
        if(!_isBulletAlive)
            AnimateOut();
    }

    private void OnBulletDestroy()
    {
        UnsubscribeFromBullet();
        if (!_isAnimating)
            AnimateOut();
    }

#endregion
}
