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

    private ParticleSystem.Particle[] allParticles;

    private GameObject sourceCaster;

    bool isEnemy;

    private Dictionary<MagicController, int> magicHitsThisFrame;
    private Dictionary<EnvObj, int> envHitsThisFrame;
    private Dictionary<EnvObj, bool> tempAbsorbCache;

    private Dictionary<IAbsorbable, int> absorbableHitsThisFrame;
    private Dictionary<IAbsorbable, bool> absorbableCheckCache;

    private static HashSet<long> interactionsProcessedThisFrame;
    private HashSet<uint> particlesHitTerrain;
    private Dictionary<uint, ParticleSystem.Particle> previousParticles;
    private Collider2D[] m_HitBuffer = new Collider2D[16];

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

        allParticles = new ParticleSystem.Particle[psMain.maxParticles];

        particlesHitTerrain = new HashSet<uint>();
        previousParticles = new Dictionary<uint, ParticleSystem.Particle>();

        magicHitsThisFrame = new Dictionary<MagicController, int>();
        envHitsThisFrame = new Dictionary<EnvObj, int>();
        tempAbsorbCache = new Dictionary<EnvObj, bool>();

        absorbableHitsThisFrame = new Dictionary<IAbsorbable, int>();
        absorbableCheckCache = new Dictionary<IAbsorbable, bool>();

        if (interactionsProcessedThisFrame == null)
        {
            interactionsProcessedThisFrame = new HashSet<long>();
        }
    }

    public void Init(Vector2 pos, Quaternion direction, MagicType typ, int degree, bool isEnemy, GameObject source)
    {
        Vector2 finalPos = pos;
        if (source != null)
        {
            Vector2 sourcePos = source.transform.position;
            float dist = Vector2.Distance(sourcePos, pos);
            if (dist > 0)
            {
                RaycastHit2D hit = Physics2D.Raycast(sourcePos, (pos - sourcePos).normalized, dist, TerrainLayer);
                if (hit.collider != null)
                {
                    finalPos = hit.point + (hit.normal * 0.1f);
                }
            }
        }
        this.transform.position = finalPos;
        this.Typ = typ;

        var baseColorMinMax = Consts.ElementColors[typ];
        psMain.startColor = baseColorMinMax;

        psMain.maxParticles = degree * Consts.ParticlePerDegree;
        ParticleSystem.Burst firstBurst = ps.emission.GetBurst(0);
        firstBurst.count = psMain.maxParticles;
        ps.emission.SetBurst(0, firstBurst);

        this.SetDegree(degree);
        gameObject.transform.rotation = direction;

        this.isEnemy = isEnemy;
        this.sourceCaster = source;

        psTrails.enabled = true;
        psTrails.mode = ParticleSystemTrailMode.PerParticle;
        psTrails.lifetime = new ParticleSystem.MinMaxCurve(0.1f);

        AnimationCurve widthCurve = new AnimationCurve();
        widthCurve.AddKey(new Keyframe(0.0f, 0.0f));
        widthCurve.AddKey(new Keyframe(1.0f, 1.0f));
        psTrails.widthOverTrail = new ParticleSystem.MinMaxCurve(0.5f, widthCurve);

        float alphaHead = 0.3f;
        float alphaTail = 0.0f;

        Gradient gradient = new Gradient();
        Color mainColor = baseColorMinMax;

        if (this.isEnemy)
        {
            Color purple;
            if (ColorUtility.TryParseHtmlString("#7a367b", out purple))
            {
                gradient.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(purple, 0.0f), new GradientColorKey(mainColor, 1.0f) },
                    new GradientAlphaKey[] { new GradientAlphaKey(alphaTail, 0.0f), new GradientAlphaKey(alphaHead, 1.0f) }
                );
            }
            else
            {
                gradient.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(mainColor, 0.0f), new GradientColorKey(mainColor, 1.0f) },
                    new GradientAlphaKey[] { new GradientAlphaKey(alphaTail, 0.0f), new GradientAlphaKey(alphaHead, 1.0f) }
                );
            }
        }
        else
        {
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(mainColor, 0.0f), new GradientColorKey(mainColor, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(alphaTail, 0.0f), new GradientAlphaKey(alphaHead, 1.0f) }
            );
        }
        psTrails.colorOverTrail = gradient;

        magicHitsThisFrame.Clear();
        envHitsThisFrame.Clear();
        tempAbsorbCache.Clear();

        absorbableHitsThisFrame.Clear();
        absorbableCheckCache.Clear();

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
        if (degree == 360)
        {
            psShape.shapeType = ParticleSystemShapeType.Circle;
            psShape.arc = 360f;
            psShape.radiusThickness = 0f;
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
    }

    void LateUpdate()
    {
        if (ps == null) return;

        if (allParticles == null || allParticles.Length < ps.main.maxParticles)
        {
            allParticles = new ParticleSystem.Particle[ps.main.maxParticles];
        }

        int numParticlesAlive = ps.GetParticles(allParticles);
        bool particlesModified = false;
        float dt = Time.deltaTime;

        int envLayerIndex = LayerMask.NameToLayer("EnvTrigger");
        int envLayerMask = (envLayerIndex != -1) ? (1 << envLayerIndex) : 0;
        int targetLayers = TerrainLayer.value | MagicInteractionLayer.value | envLayerMask;

        bool isLocalSpace = ps.main.simulationSpace == ParticleSystemSimulationSpace.Local;
        Matrix4x4 localToWorld = ps.transform.localToWorldMatrix;

        var currentFrameParticles = new Dictionary<uint, ParticleSystem.Particle>(numParticlesAlive);

        envHitsThisFrame.Clear();
        magicHitsThisFrame.Clear();
        tempAbsorbCache.Clear();

        absorbableHitsThisFrame.Clear();
        absorbableCheckCache.Clear();

        for (int i = 0; i < numParticlesAlive; i++)
        {
            ref ParticleSystem.Particle p = ref allParticles[i];
            if (p.remainingLifetime <= 0) continue;

            currentFrameParticles[p.randomSeed] = p;
            if (previousParticles.TryGetValue(p.randomSeed, out var prevP))
            {
                float distanceSq = (p.position - prevP.position).sqrMagnitude;
                float allowedDistance = prevP.velocity.magnitude * 1.5f * dt;
                float allowedDistanceSq = allowedDistance * allowedDistance;

                if (distanceSq > allowedDistanceSq)
                {
                    p.remainingLifetime = 0;
                    particlesModified = true;
                    continue;
                }
            }

            Vector2 worldPos = isLocalSpace ? localToWorld.MultiplyPoint3x4(p.position) : p.position;
            float checkRadius = p.GetCurrentSize(ps) * 0.5f;

            int numHits = Physics2D.OverlapCircleNonAlloc(worldPos, checkRadius, m_HitBuffer, targetLayers);
            bool particleKilled = false;
            bool hitTerrainThisFrame = false;

            for (int j = 0; j < numHits; j++)
            {
                Collider2D hitCol = m_HitBuffer[j];
                int hitLayer = hitCol.gameObject.layer;

                EnvObj envObj = hitCol.GetComponentInParent<EnvObj>();
                if (envObj != null)
                {
                    bool canAbsorb;
                    if (!tempAbsorbCache.TryGetValue(envObj, out canAbsorb))
                    {
                        canAbsorb = envObj.CanAbsorb(this.Typ);
                        tempAbsorbCache[envObj] = canAbsorb;
                    }

                    if (canAbsorb)
                    {
                        if (!envHitsThisFrame.ContainsKey(envObj))
                        {
                            envHitsThisFrame[envObj] = 0;
                        }
                        envHitsThisFrame[envObj]++;

                        p.remainingLifetime = 0;
                        particleKilled = true;
                        break;
                    }
                }

                if (((1 << hitLayer) & TerrainLayer.value) != 0)
                {
                    if ((psMain.startLifetime.constant - p.remainingLifetime) < 0.15f)
                    {
                        p.remainingLifetime = 0;
                        particleKilled = true;
                        break;
                    }

                    if (this.Typ != MagicType.EarthWave)
                    {
                        if (!particlesHitTerrain.Contains(p.randomSeed))
                        {
                            particlesHitTerrain.Add(p.randomSeed);
                        }
                        hitTerrainThisFrame = true;
                    }
                }
                else if (!hitTerrainThisFrame)
                {
                    IAbsorbable absorbable = hitCol.GetComponent<IAbsorbable>();

                    if (absorbable != null)
                    {
                        bool canAbsorbGeneric;
                        if (!absorbableCheckCache.TryGetValue(absorbable, out canAbsorbGeneric))
                        {
                            if (this.sourceCaster != null)
                            {
                                canAbsorbGeneric = Utils.CanAbsorb(this.Typ, this.sourceCaster, hitCol.gameObject);
                            }
                            else
                            {
                                canAbsorbGeneric = false;
                            }
                            absorbableCheckCache[absorbable] = canAbsorbGeneric;
                        }

                        if (canAbsorbGeneric)
                        {
                            if (!absorbableHitsThisFrame.ContainsKey(absorbable))
                            {
                                absorbableHitsThisFrame[absorbable] = 0;
                            }
                            absorbableHitsThisFrame[absorbable]++;

                            p.remainingLifetime = 0;
                            particleKilled = true;
                            break;
                        }
                    }

                    if (((1 << hitLayer) & MagicInteractionLayer.value) != 0)
                    {
                        MagicController otherMagic = hitCol.GetComponent<MagicController>();
                        if (otherMagic != null && otherMagic != this)
                        {
                            if (!magicHitsThisFrame.ContainsKey(otherMagic))
                            {
                                magicHitsThisFrame[otherMagic] = 0;
                            }
                            magicHitsThisFrame[otherMagic]++;

                            p.remainingLifetime = 0;
                            particleKilled = true;
                            break;
                        }
                    }
                }
            }

            if (particleKilled)
            {
                particlesModified = true;
                continue;
            }

            bool hitTerrainHistory = particlesHitTerrain.Contains(p.randomSeed);

            if (once)
            {
                if (!hitTerrainHistory)
                {
                    p.velocity = this.transform.up * MoveSpeed;
                }
            }
        }

        previousParticles = currentFrameParticles;
        if (once) once = false;

        if (particlesModified)
        {
            ps.SetParticles(allParticles, numParticlesAlive);
        }

        foreach (var kvp in envHitsThisFrame)
        {
            EnvObj target = kvp.Key;
            int count = kvp.Value;
            if (target != null)
            {
                target.Absorb(this.Typ, count, this.isEnemy);
            }
        }

        foreach (var kvp in absorbableHitsThisFrame)
        {
            IAbsorbable target = kvp.Key;
            int count = kvp.Value;

            if (target != null && !target.Equals(null))
            {
                target.OnParticleAbsorbed(this.Typ, count);
            }
        }

        ProcessMagicInteractions();
    }

    void OnParticleCollision(GameObject other) { }

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
        return (long)(id1 < id2 ? id1 : id2) << 32 | (id1 < id2 ? id2 : id1);
    }
}