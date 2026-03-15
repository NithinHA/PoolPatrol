using Unity.Cinemachine;
using UnityEngine;

namespace Utils
{
    public enum ShakeIntensity
    {
        Light,
        Medium,
        Heavy,
        PlayerDamage
    }

    public class CameraShaker : MonoBehaviour
    {
        public static CameraShaker Instance { get; private set; }

        [Header("Impulse Sources")]
        public CinemachineImpulseSource LightShake;
        public CinemachineImpulseSource MediumShake;
        public CinemachineImpulseSource HeavyShake;
        public CinemachineImpulseSource PlayerDamageShake;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void Shake(ShakeIntensity intensity, float amplitudeScale = 1f)
        {
            CinemachineImpulseSource source = intensity switch
            {
                ShakeIntensity.Light => LightShake,
                ShakeIntensity.Medium => MediumShake,
                ShakeIntensity.Heavy => HeavyShake,
                ShakeIntensity.PlayerDamage => PlayerDamageShake,
                _ => null
            };

            if (source != null)
            {
                // Multiplies the base signal amplitude by this scale modifier
                source.DefaultVelocity = Random.onUnitSphere * amplitudeScale;
                source.GenerateImpulse();
            }
        }
    }
}
