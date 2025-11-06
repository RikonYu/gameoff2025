using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 示例脚本：玩家如何施法
/// </summary>
public class PlayerMagic : MonoBehaviour
{
    public ParticleWorld world;
    public Transform castPoint; // 法术发射点

    [Header("法术 A: 火球 (粒子群)")]
    public int fireballParticles = 50;
    public float fireballSpeed = 20f;
    public float fireballEnergy = 100f;

    [Header("法术 B: 岩石尖刺 (刚体)")]
    public GameObject rockSpikePrefab; // 拖拽一个带有 ParticleRigidBody 的 Prefab
    public float rockSpikeForce = 30f;

    private uint myOwnerId = 1; // 1 = 玩家

    void Update()
    {
        if (world == null) world = ParticleWorld.Instance;
        if (world == null) return; // 等待 ParticleWorld 初始化

        // --- 法术 A: 火球术 (粒子群) ---
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            CastFireball();
        }

        // --- 法术 B: 岩石尖刺 (粒子刚体) ---
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            CastRockSpike();
        }
    }

    void CastFireball()
    {
        Vector2 castDirection = castPoint.right; // 假设玩家朝右
        Vector2 baseWorldPos = castPoint.position;

        List<ParticleWorld.SpawnData> spawns = new List<ParticleWorld.SpawnData>(fireballParticles);

        for (int i = 0; i < fireballParticles; i++)
        {
            // 1. 创建火粒子
            ParticleWorld.ParticleCell cell = ParticleWorld.ParticleCell.Create(
                2, // Fire
                1, // Liquid
                fireballEnergy,
                1, // is_attack = true
                myOwnerId
            );
            // 赋予初始速度 (在CA模拟中，这个速度会被重力取代，但可以用于第一帧)
            cell.velocity = castDirection * fireballSpeed;

            // 2. 计算位置 (在发射点周围随机分布)
            Vector2 worldPos = baseWorldPos + Random.insideUnitCircle * 0.2f;
            Vector2Int gridPos = world.WorldToGrid(worldPos);

            spawns.Add(new ParticleWorld.SpawnData { position = gridPos, cell = cell });
        }

        // 3. 提交到 GPU
        world.PaintParticles(spawns);
    }

    void CastRockSpike()
    {
        // 1. 实例化 CPU 刚体
        GameObject spike = Instantiate(rockSpikePrefab, castPoint.position, castPoint.rotation);

        // 2. 获取其 ParticleRigidBody 脚本 (确保 Prefab 上有)
        // (ParticleRigidBody 脚本会自动设置粒子属性)

        // 3. 赋予力 (让它飞出去)
        Rigidbody2D rb = spike.GetComponent<Rigidbody2D>();
        rb.AddForce(castPoint.right * rockSpikeForce, ForceMode2D.Impulse);
    }
}
