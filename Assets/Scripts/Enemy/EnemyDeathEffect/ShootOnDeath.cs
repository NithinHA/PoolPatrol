using System.Collections.Generic;
using UnityEngine;

namespace Enemy.Death
{
    public class ShootOnDeath : MonoBehaviour, IOnDeathEffect
    {
        /// <summary>
        /// Instead this will get EnemyWeaponController and call fire
        /// </summary>
        public GameObject bulletPrefab;
        public int count = 3;

        public void Execute(Dictionary<string, object> parameters)
        {
            float angleStep = 360f / count;
            for (int i = 0; i < count; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
                bullet.GetComponent<Rigidbody2D>().linearVelocity = dir * 5f;
            }
        }
    }
}