using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveMagic : MonoBehaviour
{
    private ParticleSystem ps;

    private List<ParticleSystem.Particle> enterParticles = new List<ParticleSystem.Particle>();
    void Start()
    {
        ps = GetComponent<ParticleSystem>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnParticleTrigger()
    {
        int numEnter = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Enter, enterParticles, out var enterColliderData);

        for (int i = 0; i < numEnter; i++)
        {
            var targetCollider = enterColliderData.GetCollider(i, 0);

            if (targetCollider != null)
            {
                PlayerController target = targetCollider.GetComponent<PlayerController>();

                if (target != null)
                {
                    target.OnHit();
                }
            }
        }
    }
}
