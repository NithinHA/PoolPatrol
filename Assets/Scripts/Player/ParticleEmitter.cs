using UnityEngine;

public class ParticleEmitter : MonoBehaviour
{
    public ParticleSystem Particles;

    void Start()
    {
        if (Particles == null)
            Particles = GetComponent<ParticleSystem>();

        ParticleSystem.EmissionModule emission = Particles.emission;
        emission.enabled = false;
    }

    public void EmitParticles(int count = 1)
    {
        Particles.Emit(count);
    }
}
