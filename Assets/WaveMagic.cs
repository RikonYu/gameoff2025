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

        
    }
}
