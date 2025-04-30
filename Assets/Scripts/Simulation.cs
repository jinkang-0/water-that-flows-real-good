using System;
using UnityEngine;
using Unity.Mathematics;

public class Simulation : MonoBehaviour
{
    public event System.Action SimulationStepCompleted;

    [Header("Simulation Settings")] 
    public float timeScale = 1;
    public bool fixedTimeStep;
    public int iterationsPerFrame;
    public float gravity = -9.81f;
    public Vector2 boundsSize;
    public Vector2Int numCells;
    public int numParticles;
    public float particleRadius;

    [Header("Interaction Settings")] 
    public float interactionRadius;

    [Header("References")] 
    public Initializer initializer;
    public ComputeShader gpuCompute;
    public Display2D display;
    
    // inferred variables
    public Vector2 cellSize { get; private set; }
    private int totalCells;
    private int numVelocities;
    private Initializer.SpawnData spawnData;
    private CPUCompute cpuCompute;
    private float restDensity = 0;
    private Vector2 lastCursorPosition;
    private Vector2 cursorPosition;
    private ComputeBuffer cellTypeBuffer;
    
    // gpu compute shader kernels
    private const int userInputKernel = 0;

    // buffers
    public int[] cellTypes { get; private set; }
    public float2[] cellWeights { get; private set; }
    public float2[] cellVelocities { get; private set; }
    public float2[] particlePositions { get; private set; }
    public float2[] particleVelocities { get; private set; }
    public float[] densityBuffer { get; private set; }
    
    // state
    private bool isPaused;
    private bool pauseNextFrame;
    
    private void Start()
    {
        Debug.Log("Controls: Space = Play/Pause, R = Reset, RightArrow = Step, RightClick = Delete");
        
        // target fps
        float deltaTime = 1 / 120f;
        Time.fixedDeltaTime = deltaTime;
        
        // determine cell size
        cellSize = boundsSize / numCells;
        totalCells = numCells.x * numCells.y;

        // create buffers
        cellTypes = new int[totalCells];
        densityBuffer = new float[totalCells];
        cellWeights = new float2[totalCells];
        cellVelocities = new float2[totalCells];
        particlePositions = new float2[numParticles];
        particleVelocities = new float2[numParticles];
        cellTypeBuffer = ComputeHelper.CreateStructuredBuffer<float>(totalCells);

        // initialize buffer data
        spawnData = initializer.GetSpawnData(numCells, numParticles);
        SetInitialBufferData();
        
        // set gpu compute variables
        gpuCompute.SetFloat("interactionRadius", interactionRadius);
        gpuCompute.SetInt("totalCells", totalCells);
        gpuCompute.SetInts("size", new int[]{numCells.x, numCells.y});
        gpuCompute.SetVector("cellSize", cellSize);
        gpuCompute.SetVector("boundsSize", boundsSize);

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
        // UpdateSettings();

        // run steps
        for (int i = 0; i < iterationsPerFrame; i++)
        {
            RunSimulationStep(timeStep);
            SimulationStepCompleted?.Invoke();
        }
    }

    // run one step
    private void RunSimulationStep(float deltaTime)
    {
        // integrate particles and handle collisions
        cpuCompute.SimulateParticles(particlePositions, particleVelocities, gravity, deltaTime);
        cpuCompute.PushApartParticles(particlePositions);
        cpuCompute.HandleObstacleCollisions(particlePositions, particleVelocities);

        // solve for incompressibility and account for particle drift
        cpuCompute.VelocityTransferParticle(cellTypes, cellVelocities, cellWeights, particlePositions, particleVelocities);
        restDensity = cpuCompute.ComputeDensities(cellTypes, particlePositions, densityBuffer, restDensity);
        cpuCompute.SolveIncompressibility(cellVelocities, cellTypes, densityBuffer, restDensity, 50, 1.9f);
        cpuCompute.VelocityTransferGrid(cellTypes, cellVelocities, particlePositions, particleVelocities);

        // handle mouse interactions
        // cellTypeBuffer.SetData(cellTypes);
        // ComputeHelper.SetBuffer(gpuCompute, cellTypeBuffer, "cellType", userInputKernel);
        // ComputeHelper.Dispatch(gpuCompute, totalCells, kernelIndex: userInputKernel);
        // cellTypeBuffer.GetData(cellTypes);
    }

    // update compute shader settings
    private void UpdateSettings()
    {
        // mouse interactions
        if (Camera.main == null) return;
        
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        int interactionType = 0;

        if (Input.GetMouseButton(0))
        {
            lastCursorPosition = cursorPosition;
            cursorPosition = mousePos;
            interactionType = 1;
        }
        else
        {
            lastCursorPosition = mousePos;
            cursorPosition = mousePos;
        }

        gpuCompute.SetVector("lastCursorPosition", lastCursorPosition);
        gpuCompute.SetVector("cursorPosition", cursorPosition);
        gpuCompute.SetInt("interactionType", interactionType);
    }

    // reset buffer data to spawner data
    private void SetInitialBufferData()
    {
        Array.Copy(spawnData.cellTypes, cellTypes, totalCells);
        Array.Copy(spawnData.cellVelocities, cellVelocities, totalCells);
        
        Array.Copy(spawnData.particleVelocities, particleVelocities, numParticles);
        Array.Copy(spawnData.positions, particlePositions, numParticles);
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
            SetInitialBufferData();
        }
    }

    private void OnDestroy()
    {
        cellTypeBuffer.Release();
    }

    private void OnDrawGizmos()
    {
        // draw simulation bounds
        Gizmos.color = new Color(0, 1, 0, 0.4f);
        Gizmos.DrawWireCube(Vector2.zero, boundsSize);

        // draw cells
        Vector2 size = boundsSize / numCells;
        Vector2 lowerCorner = -boundsSize / 2;
        for (int y = 0; y < numCells.y; y++)
        {
            for (int x = 0; x < numCells.x; x++)
            {
                Gizmos.DrawWireCube(lowerCorner + new Vector2(x+0.5f, y+0.5f)*size, size);
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
