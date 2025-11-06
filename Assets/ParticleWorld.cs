using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class ParticleWorld : MonoBehaviour
{
    public static ParticleWorld Instance { get; private set; }

    [Header("设置")]
    public ComputeShader particleShader;
    public int worldWidth = 1024;
    public int worldHeight = 512;
    public int maxSamples = 1024;
    public int maxSpawns = 8192;

    private RenderTexture worldTexture;

    private ComputeBuffer gridBufferA;
    private ComputeBuffer gridBufferB;

    private ComputeBuffer sampleCoordsBuffer;
    private ComputeBuffer sampleResultsBuffer;
    private ComputeBuffer spawnBuffer;

    private int kernelReactDecay;
    private int kernelPhysicsMove;
    private int kernelRender;
    private int kernelPaint;
    private int kernelSample;

    private const int PARTICLE_CELL_STRIDE = 28;
    private const int SPAWN_DATA_STRIDE = 36;
    private const int SAMPLE_COORDS_STRIDE = 8;

    public struct ParticleCell
    {
        public int elementType;
        public int physicalState;
        public float currentEnergy;
        public Vector2 velocity;
        public uint is_attack;
        public uint owner;
    }

    public struct SpawnData
    {
        public Vector2Int position;
        public ParticleCell cell;
    }

    private ParticleCell[] sampleResultsHost;
    private Vector2Int[] sampleCoordsHost;
    private List<SpawnData> tempSpawnList = new List<SpawnData>();

    private AsyncGPUReadbackRequest sampleReadbackRequest;
    private bool sampleRequestInProgress = false;
    private System.Action<ParticleCell[]> currentSampleCallback;
    private int lastRequestedSampleCount = 0;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            InitializeBuffers();
        }
    }

    void OnDestroy()
    {
        ReleaseBuffers();
    }

    void InitializeBuffers()
    {
        int totalCells = worldWidth * worldHeight;

        gridBufferA = new ComputeBuffer(totalCells, PARTICLE_CELL_STRIDE);
        gridBufferB = new ComputeBuffer(totalCells, PARTICLE_CELL_STRIDE);

        particleShader.SetBuffer(kernelReactDecay, "Grid_Read", gridBufferA);
        particleShader.SetBuffer(kernelReactDecay, "Grid_Write", gridBufferB);

        particleShader.SetBuffer(kernelPhysicsMove, "Grid_Read", gridBufferB);
        particleShader.SetBuffer(kernelPhysicsMove, "Grid_Write", gridBufferA);

        particleShader.SetBuffer(kernelRender, "Grid_Read", gridBufferA);

        if (worldTexture == null || worldTexture.width != worldWidth || worldTexture.height != worldHeight)
        {
            if (worldTexture != null) worldTexture.Release();
            worldTexture = new RenderTexture(worldWidth, worldHeight, 0, RenderTextureFormat.ARGB32);
            worldTexture.enableRandomWrite = true;
            worldTexture.Create();
        }
        particleShader.SetTexture(kernelRender, "OutputTexture", worldTexture);

        spawnBuffer = new ComputeBuffer(maxSpawns, SPAWN_DATA_STRIDE);
        particleShader.SetBuffer(kernelPaint, "SpawnBuffer", spawnBuffer);

        sampleCoordsBuffer = new ComputeBuffer(maxSamples, SAMPLE_COORDS_STRIDE);
        sampleResultsBuffer = new ComputeBuffer(maxSamples, PARTICLE_CELL_STRIDE);
        particleShader.SetBuffer(kernelSample, "SampleCoordsBuffer", sampleCoordsBuffer);
        particleShader.SetBuffer(kernelSample, "SampleResultsBuffer", sampleResultsBuffer);
        particleShader.SetBuffer(kernelSample, "Grid_Read", gridBufferA);

        sampleResultsHost = new ParticleCell[maxSamples];
        sampleCoordsHost = new Vector2Int[maxSamples];

        particleShader.SetInt("_WorldWidth", worldWidth);
        particleShader.SetInt("_WorldHeight", worldHeight);
    }

    void ReleaseBuffers()
    {
        gridBufferA?.Release();
        gridBufferB?.Release();
        spawnBuffer?.Release();
        sampleCoordsBuffer?.Release();
        sampleResultsBuffer?.Release();
        worldTexture?.Release();
    }

    void FixedUpdate()
    {
        particleShader.SetFloat("_DeltaTime", Time.fixedDeltaTime);
        particleShader.SetInt("_SimFrameOffset", Time.frameCount);

        particleShader.SetBuffer(kernelReactDecay, "Grid_Read", gridBufferA);
        particleShader.SetBuffer(kernelReactDecay, "Grid_Write", gridBufferB);
        particleShader.Dispatch(kernelReactDecay, worldWidth / 8, worldHeight / 8, 1);

        particleShader.SetBuffer(kernelPhysicsMove, "Grid_Read", gridBufferB);
        particleShader.SetBuffer(kernelPhysicsMove, "Grid_Write", gridBufferA);
        particleShader.Dispatch(kernelPhysicsMove, worldWidth / 8, worldHeight / 8, 1);

        HandleAsyncSampling();
    }

    void Update()
    {
        particleShader.SetBuffer(kernelRender, "Grid_Read", gridBufferA);
        particleShader.SetTexture(kernelRender, "OutputTexture", worldTexture);
        particleShader.Dispatch(kernelRender, worldWidth / 8, worldHeight / 8, 1);
    }

    public RenderTexture GetWorldTexture()
    {
        return worldTexture;
    }

    public Vector2Int WorldToGrid(Vector2 worldPos)
    {
        float normX = worldPos.x / Screen.width;
        float normY = worldPos.y / Screen.height;
        return new Vector2Int((int)(normX * worldWidth), (int)(normY * worldHeight));
    }

    public void PaintParticles(List<SpawnData> spawnDataList)
    {
        int count = spawnDataList.Count;
        if (count == 0) return;

        if (count > maxSpawns)
        {
            count = maxSpawns;
        }

        if (spawnBuffer == null || spawnBuffer.count < count)
        {
            if (spawnBuffer != null) spawnBuffer.Release();
            spawnBuffer = new ComputeBuffer(count, SPAWN_DATA_STRIDE);
            particleShader.SetBuffer(kernelPaint, "SpawnBuffer", spawnBuffer);
        }

        spawnBuffer.SetData(spawnDataList, 0, 0, count);

        particleShader.SetInt("_SpawnCount", count);
        particleShader.SetBuffer(kernelPaint, "Grid_Write", gridBufferA);
        particleShader.Dispatch(kernelPaint, Mathf.CeilToInt(count / 64.0f), 1, 1);
    }

    public void RequestSample(List<Vector2Int> coords, int count, System.Action<ParticleCell[]> callback)
    {
        if (sampleRequestInProgress || count == 0)
        {
            return;
        }

        if (count > maxSamples)
        {
            count = maxSamples;
        }

        lastRequestedSampleCount = count;

        for (int i = 0; i < count; i++)
        {
            sampleCoordsHost[i] = coords[i];
        }

        sampleCoordsBuffer.SetData(sampleCoordsHost, 0, 0, count);

        particleShader.SetInt("_SampleCount", count);
        particleShader.SetBuffer(kernelSample, "Grid_Read", gridBufferA);
        particleShader.SetBuffer(kernelSample, "SampleResultsBuffer", sampleResultsBuffer);
        particleShader.Dispatch(kernelSample, Mathf.CeilToInt(count / 64.0f), 1, 1);

        sampleReadbackRequest = AsyncGPUReadback.Request(sampleResultsBuffer, count * PARTICLE_CELL_STRIDE, 0);
        sampleRequestInProgress = true;
        currentSampleCallback = callback;
    }

    void HandleAsyncSampling()
    {
        if (!sampleRequestInProgress)
        {
            return;
        }

        if (sampleReadbackRequest.done)
        {
            if (sampleReadbackRequest.hasError)
            {
                Debug.LogError("GPU 采样回读失败! 最后一次请求的数量: " + lastRequestedSampleCount);
            }
            else
            {
                var data = sampleReadbackRequest.GetData<ParticleCell>();

                for (int i = 0; i < lastRequestedSampleCount; i++)
                {
                    sampleResultsHost[i] = data[i];
                }

                currentSampleCallback?.Invoke(sampleResultsHost);
            }

            sampleRequestInProgress = false;
            currentSampleCallback = null;
        }
    }
}
