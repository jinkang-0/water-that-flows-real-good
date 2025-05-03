using System;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Experimental.Rendering;

public class Simulation : MonoBehaviour
{
    [Header("Scene Settings")] 
    public float timeScale = 1;
    public bool fixedTimeStep;
    public int iterationsPerFrame;
    public Vector2 maxBounds;
    public int cellResolution;
    public int numParticles;
    public float particleDensity;
    
    [Header("Simulation Settings")]
    public float gravity = -9.81f;
    public float stiffness = 1;
    public float overRelaxation = 1.9f;
    public int incompressibilityIterations = 50;

    [Header("Interaction Settings")] 
    public float interactionRadius;

    [Header("References")] 
    public ComputeShader compute;
    public Initializer initializer;
    public Display2D display;

    // shared with display
    public Texture2D staticTerrainSDF;
    public Texture2D dynamicTerrainSDF;
    public RenderTexture terrainSDFEdit;
    public ComputeShader SDFEdit;
    
    // inferred variables
    public Vector2Int numCells { get; private set; }
    public float cellSize { get; private set; }
    public Vector2 boundsSize { get; private set; }
    public float particleRadius { get; private set; }
    public float partitionSpacing { get; private set; }
    public int partitionNumX { get; private set; }
    public int partitionNumY { get; private set; }
    public int numPartitionCells { get; private set; }
    private int totalCells;
    private int numVelocities;
    private Initializer.SpawnData spawnData;
    private CPUCompute cpuCompute;
    private float restDensity = 0;
    private static readonly Vector2 inactiveMousePos = new Vector2(-100, -100);
    private Vector2 lastMousePos = inactiveMousePos;

    // buffers
    public int[] cellTypes { get; private set; }
    public float2[] cellWeights { get; private set; }
    public float2[] cellVelocities { get; private set; }
    public float2[] particlePositions { get; private set; }
    public float2[] particleVelocities { get; private set; }
    public float[] densityBuffer { get; private set; }
    public int[] disabledParticles { get; private set; }
    public int[] isCellBucket { get; private set; }
    public int[] particleCounts { get; private set; }
    public int[] lookupStartIndices { get; private set; }
    public int[] particleLookup { get; private set; }
    
    // state
    private bool isPaused;
    private bool pauseNextFrame;
    
    // score
    public int score { get; set; }
    private int lastLoggedScore = -1;

    private void Start()
    {
        Debug.Log("Controls: Space = Play/Pause, R = Reset, RightArrow = Step, LeftClick = Delete");
        
        // target fps
        float deltaTime = 1 / 60f;
        Time.fixedDeltaTime = deltaTime;
        
        // determine cell size
        var limitingDimension = Mathf.Min(maxBounds.x, maxBounds.y);
        cellSize = limitingDimension / cellResolution;
        
        // determine bounds
        boundsSize = (Vector2)(Vector2Int.FloorToInt(maxBounds / cellSize)) * cellSize;
        numCells = Vector2Int.FloorToInt(boundsSize / cellSize);
        totalCells = numCells.x * numCells.y;
        particleRadius = cellSize / particleDensity;
        
        // determine spacing for partitions
        partitionSpacing = 2.2f * particleRadius;
        partitionNumX = Mathf.CeilToInt(boundsSize.x / partitionSpacing);
        partitionNumY = Mathf.CeilToInt(boundsSize.y / partitionSpacing);
        numPartitionCells = partitionNumX * partitionNumY;

        // create buffers
        cellTypes = new int[totalCells];
        densityBuffer = new float[totalCells];
        cellWeights = new float2[totalCells];
        cellVelocities = new float2[totalCells];
        particlePositions = new float2[numParticles];
        particleVelocities = new float2[numParticles];
        disabledParticles = new int[numParticles];
        isCellBucket = new int[totalCells];
        particleCounts = new int[numPartitionCells];
        lookupStartIndices = new int[numPartitionCells + 1];
        particleLookup = new int[numParticles];
        
        // compute.SetFloat("interactionInputRadius", interactionRadius);

        // initialize buffer data
        spawnData = initializer.GetSpawnData(numCells, numParticles, cellSize);
        staticTerrainSDF = new Texture2D(spawnData.staticTerrainSDF.width, spawnData.staticTerrainSDF.height, GraphicsFormat.R32_SFloat, 0, TextureCreationFlags.None);
        dynamicTerrainSDF = new Texture2D(spawnData.dynamicTerrainSDF.width, spawnData.dynamicTerrainSDF.height, GraphicsFormat.R32_SFloat, 0, TextureCreationFlags.None);
        
        SetInitialSceneData();

        terrainSDFEdit = new RenderTexture(dynamicTerrainSDF.width, dynamicTerrainSDF.height, 1, GraphicsFormat.R32_SFloat, 0);
        terrainSDFEdit.enableRandomWrite = true;

        // initialize display
        display.Init(this);
        
        // initialize compute helpers
        cpuCompute = new CPUCompute(this);

        isPaused = true;
    }

    private void FixedUpdate()
    {
        if (!fixedTimeStep) return;
        RunSimulationFrame(Time.fixedDeltaTime);
    }

    private void Update()
    {
        // run simulation if not in fixed time step mode
        // skip first few frames since deltaTime can be disproportionately large
        if (!fixedTimeStep && Time.frameCount > 10)
        {
            RunSimulationFrame(Time.deltaTime);
        }

        // handle stepping
        if (pauseNextFrame)
        {
            isPaused = true;
            pauseNextFrame = false;
        }

        // handle user input
        HandleInput();
    }

    // run one frame
    private void RunSimulationFrame(float frameTime)
    {
        if (isPaused) return;

        // adjust delta time for shader
        float timeStep = frameTime / iterationsPerFrame * timeScale;
        // UpdateSettings(timeStep);

        // run steps
        for (int i = 0; i < iterationsPerFrame; i++)
        {
            RunSimulationStep(timeStep);
        }

        if (score != lastLoggedScore)
        {
            Debug.Log($"Score: {score}");
            lastLoggedScore = score;
        }

    }

    // run one step
    private void RunSimulationStep(float deltaTime)
    {
        // simulate particle physics
        cpuCompute.SimulateParticles(particlePositions, particleVelocities, gravity, deltaTime);
        cpuCompute.PushApartParticles(particlePositions, particleCounts, lookupStartIndices, particleLookup);
        cpuCompute.ConstrainToBounds(particlePositions, particleVelocities);
        cpuCompute.TerrainCollisions(dynamicTerrainSDF, particlePositions, particleVelocities);
        cpuCompute.TerrainCollisions(staticTerrainSDF, particlePositions, particleVelocities);
        cpuCompute.HandleDrainCollisions(cellTypes, particlePositions);
        
        cpuCompute.VelocityTransferParticle(cellTypes, cellVelocities, cellWeights, particlePositions, particleVelocities, disabledParticles);
        restDensity = cpuCompute.ComputeDensities(cellTypes, particlePositions, densityBuffer, restDensity);
        cpuCompute.SolveIncompressibility(cellVelocities, cellTypes, densityBuffer, restDensity);
        cpuCompute.VelocityTransferGrid(cellTypes, cellVelocities, particlePositions, particleVelocities, disabledParticles);

        // handle mouse interactions
        // ComputeHelper.Dispatch(compute, totalCells, kernelIndex: userInputKernel);
    }

    // update compute shader settings
    // private void UpdateSettings(float deltaTime)
    // {
    //     // mouse interactions
    //     if (Camera.main == null) return;
    //
    //     Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    //     int interactionType = 0;
    //     bool isPush = Input.GetMouseButton(0);
    //     bool isDelete = Input.GetMouseButton(1);
    //     if (isPush)
    //     {
    //         interactionType = 1;
    //     } 
    //     else if (isDelete)
    //     {
    //         interactionType = 2;
    //     }
    //
    //     compute.SetVector("interactionInputPoint", mousePos);
    //     compute.SetInt("interactionInputType", interactionType);
    // }

    // reset buffer data to spawner data
    private void SetInitialSceneData()
    {
        // initial buffers
        Array.Copy(spawnData.cellTypes, cellTypes, totalCells);
        Array.Copy(spawnData.cellVelocities, cellVelocities, totalCells);
        Array.Copy(spawnData.particleVelocities, particleVelocities, numParticles);
        Array.Copy(spawnData.positions, particlePositions, numParticles); 
        
        // set initial SDFs
        Graphics.CopyTexture(spawnData.staticTerrainSDF, staticTerrainSDF);
        Graphics.CopyTexture(spawnData.dynamicTerrainSDF, dynamicTerrainSDF);
        
        // set score values
        score = 0;
        lastLoggedScore = -1;
    }

    private void TerrainEdit()
    {
        if (Camera.main == null) return;
    
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (Input.GetMouseButton(0))
        {
            Graphics.Blit(dynamicTerrainSDF, terrainSDFEdit);

            {
                int kernel = SDFEdit.FindKernel("Edit");
                SDFEdit.GetKernelThreadGroupSizes(kernel, out uint thread_group_w, out uint thread_group_h, out uint _z);
                int w = dynamicTerrainSDF.width / (int)thread_group_w + 1;
                int h = dynamicTerrainSDF.width / (int)thread_group_h + 1;

                SDFEdit.SetInt("width", dynamicTerrainSDF.width);
                SDFEdit.SetInt("height", dynamicTerrainSDF.height);

                var currMousePos = new Vector2(dynamicTerrainSDF.width, dynamicTerrainSDF.height) *
                    (mousePos + 0.5f * boundsSize) / boundsSize;
                
                SDFEdit.SetVector("lastMousePos", lastMousePos);
                SDFEdit.SetVector("mousePos", currMousePos);
                
                // update state
                lastMousePos = currMousePos;

                SDFEdit.SetFloat("interaction_radius", interactionRadius * dynamicTerrainSDF.width / boundsSize.x);

                SDFEdit.SetTexture(kernel, "Distance", terrainSDFEdit, 0);
                SDFEdit.Dispatch(kernel, w, h, 1);
            }
            {
                RenderTexture.active = terrainSDFEdit;
                dynamicTerrainSDF.ReadPixels(new Rect(0, 0, dynamicTerrainSDF.width, dynamicTerrainSDF.height), 0, 0);
                dynamicTerrainSDF.Apply();
            }
        }
        else
        {
            lastMousePos = inactiveMousePos;
        }
    }

    private void HandleInput()
    {
        // SPACE: pause
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isPaused = !isPaused;
            Debug.Log(isPaused ? "Paused" : "Unpaused");
        }

        // RIGHT_ARROW: run one step
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            isPaused = false;
            pauseNextFrame = true;
        }

        // R: reset
        if (Input.GetKeyDown(KeyCode.R))
        {
            isPaused = true;
            SetInitialSceneData();
        }

        TerrainEdit();
    }

    // private void OnDestroy()
    // {
    //     
    // }

    private void OnDrawGizmos()
    {
        // draw simulation bounds
        Gizmos.color = new Color(0, 1, 0, 0.4f);
        Gizmos.DrawWireCube(Vector2.zero, maxBounds);
        
        // determine cell size
        var limitingDimension = Mathf.Min(maxBounds.x, maxBounds.y);
        var cs = limitingDimension / cellResolution;
        var bounds = (Vector2)Vector2Int.FloorToInt(maxBounds / cs) * cs;
        var n = Vector2Int.FloorToInt(bounds / cs);

        // draw cells
        Vector2 lowerCorner = -bounds / 2;
        for (int y = 0; y < n.y; y++)
        {
            for (int x = 0; x < n.x; x++)
            {
                Gizmos.DrawWireCube(lowerCorner + new Vector2(x+0.5f, y+0.5f)*cs, Vector3.one * cs);
            }
        }

        if (!Application.isPlaying) return;

        if (Camera.main != null)
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            bool isPull = Input.GetMouseButton(0);
            bool isPush = Input.GetMouseButton(1);
        
            if (isPull || isPush)
            {
                Gizmos.color = isPull ? Color.green : Color.red;
                Gizmos.DrawWireSphere(mousePos, interactionRadius);
            }
        }
    }
}
