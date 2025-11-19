using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[RequireComponent(typeof(Rigidbody2D))]
public class MagicController : MonoBehaviour
{
    public MagicType Typ;
    public float MoveSpeed;
    public float MoveDelay;
    float lifetime = 0f;
    public ParticleSystem ps;
    ParticleSystem.MainModule psMain;
    ParticleSystem.ShapeModule psShape;
    ParticleSystem.TrailModule psTrails;

    [Header("Layer Settings")]
    public LayerMask TerrainLayer;
    public LayerMask PlayerTriggerLayer;
    public LayerMask EnemyTriggerLayer;
    public LayerMask EnvTriggerLayer;
    public LayerMask MagicInteractionLayer;

    private List<ParticleSystem.Particle> triggerParticles;
    private ParticleSystem.Particle[] allParticles;

    private GameObject sourceCaster;
    bool isAlly, isEnemy;

    private Dictionary<MagicController, int> magicHitsThisFrame;
    private static HashSet<long> interactionsProcessedThisFrame;

    private HashSet<uint> particlesHitTerrain;
    private Dictionary<uint, ParticleSystem.Particle> previousParticles;

    void Awake()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        ps = GetComponentInChildren<ParticleSystem>();

        psMain = ps.main;
        psShape = ps.shape;
        psTrails = ps.trails;

        psMain.startSpeed = MoveSpeed;
        psMain.startLifetime = Consts.WaveLifetime;

        triggerParticles = new List<ParticleSystem.Particle>();
        allParticles = new ParticleSystem.Particle[psMain.maxParticles];

        particlesHitTerrain = new HashSet<uint>();
        previousParticles = new Dictionary<uint, ParticleSystem.Particle>();

        magicHitsThisFrame = new Dictionary<MagicController, int>();
        if (interactionsProcessedThisFrame == null)
        {
            interactionsProcessedThisFrame = new HashSet<long>();
        }
    }

    public void Init(Vector2 pos, Quaternion direction, MagicType typ, int degree, bool isAlly, bool isEnemy, GameObject source)
    {
        this.transform.position = pos;
        this.Typ = typ;

        var baseColorMinMax = Consts.ElementColors[typ];
        psMain.startColor = baseColorMinMax;

        psMain.maxParticles = degree * Consts.ParticlePerDegree;
        ParticleSystem.Burst firstBurst = ps.emission.GetBurst(0);
        firstBurst.count = psMain.maxParticles;
        ps.emission.SetBurst(0, firstBurst);
        this.SetDegree(degree);
        gameObject.transform.rotation = direction;
        this.isAlly = isAlly;
        this.isEnemy = isEnemy;
        this.sourceCaster = source;

        psTrails.enabled = true;
        float trailAlpha = 0.33f;
        psTrails.mode = ParticleSystemTrailMode.PerParticle; 
        psTrails.lifetime = new ParticleSystem.MinMaxCurve(0.1f);

        if (this.isEnemy)
        {
            Color purple;
            if (ColorUtility.TryParseHtmlString("#7a367b", out purple))
            {
                Color targetColor = baseColorMinMax;
                purple.a = trailAlpha;
                Gradient gradient = new Gradient();
                gradient.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(purple, 0.0f), new GradientColorKey(targetColor, 1.0f) },
                    new GradientAlphaKey[] { new GradientAlphaKey(trailAlpha, 0.0f), new GradientAlphaKey(trailAlpha, 1.0f) }
                );
                psTrails.colorOverLifetime = gradient;
            }
        }
        else
        {
            psTrails.colorOverLifetime = baseColorMinMax;
        }

        magicHitsThisFrame.Clear();
        isstopped = false;
        lifetime = 0f;

        particlesHitTerrain.Clear();
        previousParticles.Clear();
        psMain.gravityModifier = 0;
    }

    void SetDegree(int degree)
    {
        if (allParticles.Length != psMain.maxParticles)
        {
            allParticles = new ParticleSystem.Particle[psMain.maxParticles];
        }
        psShape.angle = degree / 2;
    }

    void Update()
    {
        if (Time.frameCount % 2 == 0)
            interactionsProcessedThisFrame.Clear();

        lifetime += Time.deltaTime;

        if (lifetime >= MoveDelay)
        {
            StopParticles();
        }
        if (lifetime >= Consts.WaveLifetime)
            Destroy(gameObject);
    }

    bool isstopped = false;
    bool once = false;
    void StopParticles()
    {
        /* if (isstopped) return;
                isstopped = true;
                once = true;*/
    }

    void LateUpdate()
    {
        int numParticlesAlive = ps.GetParticles(allParticles);
        bool particlesModified = false;
        float dt = Time.deltaTime;

        var currentFrameParticles = new Dictionary<uint, ParticleSystem.Particle>(numParticlesAlive);

        for (int i = 0; i < numParticlesAlive; i++)
        {
            ParticleSystem.Particle p = allParticles[i];
            if (p.remainingLifetime == 0) continue;

            currentFrameParticles[p.randomSeed] = p;

            if (previousParticles.TryGetValue(p.randomSeed, out var prevP))
            {
                float distanceSq = (p.position - prevP.position).sqrMagnitude;
                float allowedDistance = prevP.velocity.magnitude * 1.5f * dt;
                float allowedDistanceSq = allowedDistance * allowedDistance;

                if (distanceSq > allowedDistanceSq)
                {
                    allParticles[i].remainingLifetime = 0;
                    particlesModified = true;
                }
            }
        }
        previousParticles = currentFrameParticles;


        if (once)
        {
            once = false;
            for (int i = 0; i < numParticlesAlive; i++)
            {
                if (allParticles[i].remainingLifetime == 0) continue;

                if (particlesHitTerrain.Contains(allParticles[i].randomSeed))
                {
                    Vector3 vel = allParticles[i].velocity;
                    allParticles[i].velocity = vel;
                }
                else
                {
                    allParticles[i].velocity = this.transform.up * MoveSpeed;
                }
            }
            particlesModified = true;
        }
        else if (isstopped)
        {
            for (int i = 0; i < numParticlesAlive; i++)
            {
                if (allParticles[i].remainingLifetime == 0) continue;

                if (particlesHitTerrain.Contains(allParticles[i].randomSeed))
                {
                    Vector3 vel = allParticles[i].velocity;
                    allParticles[i].velocity = vel;
                    particlesModified = true;
                }
            }
        }

        if (particlesModified)
        {
            ps.SetParticles(allParticles, numParticlesAlive);
        }

        ProcessMagicInteractions();
        magicHitsThisFrame.Clear();
    }

    void OnParticleCollision(GameObject other)
    {
    }

    void OnParticleTrigger()
    {
        int numEnter = ps.GetTriggerParticles(ParticleSystemTriggerEventType.Enter, triggerParticles, out var enterColliderData);
        bool particlesModified = false;

        for (int i = 0; i < numEnter; i++)
        {
            ParticleSystem.Particle p = triggerParticles[i];
            if (p.remainingLifetime == 0) continue;

            Component hitComponent = enterColliderData.GetCollider(i, 0);
            int hitLayer = hitComponent.gameObject.layer;

            if (((1 << hitLayer) & TerrainLayer.value) != 0)
            {
                if (this.Typ != MagicType.EarthWave)
                {
                    if (!particlesHitTerrain.Contains(p.randomSeed))
                    {
                        particlesHitTerrain.Add(p.randomSeed);
                    }
                }
            }
            else
            {
                bool absorbed = CheckAbsorptionLogic(hitComponent);
                if (absorbed)
                {
                    p.remainingLifetime = 0;
                    particlesModified = true;
                }
                else if (((1 << hitLayer) & MagicInteractionLayer.value) != 0)
                {
                    MagicController otherMagic = hitComponent.GetComponent<MagicController>();
                    if (otherMagic != null && otherMagic != this)
                    {
                        if (!magicHitsThisFrame.ContainsKey(otherMagic))
                        {
                            magicHitsThisFrame[otherMagic] = 0;
                        }
                        magicHitsThisFrame[otherMagic]++;

                        p.remainingLifetime = 0;
                        particlesModified = true;
                    }
                }
            }

            triggerParticles[i] = p;
        }

        if (particlesModified)
        {
            ps.SetTriggerParticles(ParticleSystemTriggerEventType.Enter, triggerParticles);
        }
    }

    bool CheckAbsorptionLogic(Component hitComponent)
    {
        if (this.sourceCaster == null || hitComponent == null)
        {
            return false;
        }
        bool canAbsorb = Utils.CanAbsorb(this.Typ, this.sourceCaster, hitComponent.gameObject);
        if (canAbsorb)
        {
            hitComponent.GetComponent<IAbsorbable>()?.OnParticleAbsorbed(this.Typ);
            return true;
        }
        return false;
    }

    void ProcessMagicInteractions()
    {
        foreach (var hit in magicHitsThisFrame)
        {
            MagicController other = hit.Key;
            int myHitCount = hit.Value;

            long interactionID = GetInteractionID(this, other);
            if (interactionsProcessedThisFrame.Contains(interactionID))
            {
                continue;
            }
            interactionsProcessedThisFrame.Add(interactionID);

            int otherHitCount = 0;
            if (other.magicHitsThisFrame.ContainsKey(this))
            {
                otherHitCount = other.magicHitsThisFrame[this];
            }

            if (this.Typ == other.Typ) continue;

            bool meCountersOther = Utils.IsCounter(this.Typ, other.Typ);
            bool otherCountersMe = Utils.IsCounter(other.Typ, this.Typ);

            if (!meCountersOther && !otherCountersMe) continue;

            if (meCountersOther)
            {
                int myParticlesToKill = otherHitCount;
                int otherParticlesToKill = myHitCount * 2;

                this.AnnihilateClosest(other.transform.position, myParticlesToKill);
                other.AnnihilateClosest(this.transform.position, otherParticlesToKill);
            }
            else
            {
                int myParticlesToKill = otherHitCount * 2;
                int otherParticlesToKill = myHitCount;

                this.AnnihilateClosest(other.transform.position, myParticlesToKill);
                other.AnnihilateClosest(this.transform.position, otherParticlesToKill);
            }
        }
    }

    public void AnnihilateClosest(Vector3 position, int count)
    {
        if (count <= 0) return;

        int numParticlesAlive = ps.GetParticles(allParticles);
        if (numParticlesAlive == 0) return;

        var distances = new List<(float dist, int index)>(numParticlesAlive);
        for (int i = 0; i < numParticlesAlive; i++)
        {
            if (allParticles[i].remainingLifetime > 0)
            {
                distances.Add(((allParticles[i].position - position).sqrMagnitude, i));
            }
        }

        var sorted = distances.OrderBy(d => d.dist).Take(count);

        foreach (var particleInfo in sorted)
            allParticles[particleInfo.index].remainingLifetime = 0;

        ps.SetParticles(allParticles, numParticlesAlive);
    }

    private long GetInteractionID(MagicController a, MagicController b)
    {
        int id1 = a.GetInstanceID();
        int id2 = b.GetInstanceID();
        return (id1 < id2 ? id1 : id2) << 32 | (id1 < id2 ? id2 : id1);
    }
}