using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 附加到玩家或Boss上，处理GPU粒子的伤害采样
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class UnitHealth : MonoBehaviour
{
    [Header("单位设置")]
    public float maxHealth = 100f;
    public float currentHealth;
    public uint myOwnerId = 1; // 1 = Player, 2 = Boss

    [Header("采样设置")]
    public Collider2D hitBox;
    public int sampleDensity = 10; // 每帧采样 10 个点

    private ParticleWorld world;
    private Vector2Int[] sampleCoords;
    private List<Vector2Int> tempSampleCoords = new List<Vector2Int>();

    void Start()
    {
        currentHealth = maxHealth;
        world = ParticleWorld.Instance;

        if (hitBox == null)
            hitBox = GetComponent<Collider2D>();

        sampleCoords = new Vector2Int[sampleDensity];
    }

    void FixedUpdate()
    {
        if (world == null || hitBox == null)
        {
            world = ParticleWorld.Instance; // 尝试重新获取
            return;
        }

        // 1. 生成随机采样坐标
        GenerateSampleCoords();

        // 2. 向 ParticleWorld 请求采样
        //    HandleSampledData 将作为回调函数被调用
        Debug.Log($"{sampleCoords[0]}, {sampleCoords[1]}");
        world.RequestSample(sampleCoords, HandleSampledData);
    }

    void GenerateSampleCoords()
    {
        Bounds bounds = hitBox.bounds;
        for (int i = 0; i < sampleDensity; i++)
        {
            Vector2 randomWorldPos = new Vector2(
                UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
                UnityEngine.Random.Range(bounds.min.y, bounds.max.y)
            );

            // 只采样 Hitbox 内部的点
            if (hitBox.OverlapPoint(randomWorldPos))
            {
                sampleCoords[i] = world.WorldToGrid(randomWorldPos);
            }
            else
            {
                // 失败，随便给个中心点 (避免无效采样)
                sampleCoords[i] = world.WorldToGrid(transform.position);
            }
        }
    }

    /// <summary>
    /// 这是 ParticleWorld 异步返回数据时的回调
    /// [已修复] 使用 ParticleWorld.ParticleCell[] 替换 ParticleCell[]
    /// </summary>
    void HandleSampledData(ParticleWorld.ParticleCell[] results)
    {
        if (results == null) return;

        bool tookDamage = false;

        for (int i = 0; i < sampleDensity; i++)
        {
            // 'var' 会自动推断类型为 ParticleWorld.ParticleCell
            var cell = results[i];

            // 检查是否是被一个"有攻击性"且"非自己"的粒子击中
            if (cell.is_attack == 1 && cell.owner != myOwnerId && cell.owner != 0)
            {
                // 造成伤害
                // (简单起见，每个攻击性粒子每帧造成1点伤害)
                currentHealth -= 1.0f;
                tookDamage = true;

                // [重要] 熄灭这个攻击性粒子
                // 我们在它所在的位置刷一个"空"粒子，防止它造成多帧伤害
                // 这模拟了 is_attack 变为 false
                world.PaintParticle(sampleCoords[i], ParticleWorld.ParticleCell.CreateEmpty());
            }
        }

        if (tookDamage)
        {
            Debug.Log($"Owner {myOwnerId} 受到伤害! 剩余HP: {currentHealth}");
            if (currentHealth <= 0)
            {
                Die();
            }
        }
    }

    void Die()
    {
        Debug.Log($"Owner {myOwnerId} 死亡!");
        // (触发死亡动画, 销毁, ...)
        Destroy(gameObject);
    }
}

