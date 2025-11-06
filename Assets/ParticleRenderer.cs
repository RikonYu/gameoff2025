using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 一个简单的脚本，将 ParticleWorld 的 RenderTexture 显示在 UI RawImage 或 3D Quad 上
/// </summary>
public class ParticleRenderer : MonoBehaviour
{
    public ParticleWorld particleWorld;

    [Header("渲染目标")]
    public RawImage uiRawImage;         // 拖拽一个 UI RawImage 到这里
    public MeshRenderer worldRenderer;  // 或者拖拽一个 3D Quad/SpriteRenderer 到这里

    void Update()
    {
        if (particleWorld == null || particleWorld.worldTexture == null)
        {
            Debug.LogWarning("ParticleWorld 或其 worldTexture 未设置!");
            return;
        }

        // 将 RenderTexture 应用到材质上
        if (uiRawImage != null)
        {
            uiRawImage.texture = particleWorld.worldTexture;
        }

        if (worldRenderer != null)
        {
            worldRenderer.material.mainTexture = particleWorld.worldTexture;
        }
    }
}
