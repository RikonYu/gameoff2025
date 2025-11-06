using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class ParticleRigidBody : MonoBehaviour
{
    [Header("刚体属性")]
    public int elementType = 3;
    public int physicalState = 2;
    public uint owner = 1;
    public bool isAttack = false;
    public float energy = 100f;

    [Header("碎裂设置")]
    public bool shatterOnCollision = false;
    public bool shatterOnDeath = true;
    public int particleCountOnShatter = 100;
    public float shatterInheritVelocity = 0.5f;
    public float shatterSpreadVelocity = 5.0f;

    private ParticleWorld world;
    private Rigidbody2D rb;

    void Start()
    {
        world = ParticleWorld.Instance;
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (shatterOnCollision)
        {
            Vector2 collisionPoint = collision.contacts[0].point;
            Shatter(collisionPoint);
        }
    }

    public void Shatter(Vector2 shatterCenter)
    {
        if (world == null)
        {
            Debug.LogError("ParticleWorld 未找到，无法碎裂!");
            return;
        }

        Vector2Int gridPos = world.WorldToGrid(shatterCenter);

        List<Vector2Int> positions = new List<Vector2Int>(particleCountOnShatter);
        List<ParticleWorld.ParticleCell> cells = new List<ParticleWorld.ParticleCell>(particleCountOnShatter);

        for (int i = 0; i < particleCountOnShatter; i++)
        {
            ParticleWorld.ParticleCell cell = new ParticleWorld.ParticleCell();
            cell.elementType = this.elementType;
            cell.physicalState = this.physicalState;
            cell.currentEnergy = this.energy / particleCountOnShatter;
            cell.owner = this.owner;
            cell.is_attack = (uint)(this.isAttack ? 1 : 0);

            Vector2 randomDir = Random.insideUnitCircle.normalized;
            Vector2 inheritedVel = rb.velocity * shatterInheritVelocity;
            Vector2 spreadVel = randomDir * shatterSpreadVelocity;
            cell.velocity = inheritedVel + spreadVel;

            Vector2Int spawnPos = gridPos + new Vector2Int(
                (int)(randomDir.x * 5),
                (int)(randomDir.y * 5)
            );

            positions.Add(spawnPos);
            cells.Add(cell);
        }

        world.PaintParticles(positions, cells);

        Destroy(gameObject);
    }
}

