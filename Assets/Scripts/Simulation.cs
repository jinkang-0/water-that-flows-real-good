using UnityEngine;
using Unity.Mathematics;

public class Simulation : MonoBehaviour
{
    public event System.Action SimulationStepCompleted;

    [Header("Simulation Settings")] 
    public float timeScale = 1;
    public bool fixedTimeStep;
    public int iterationsPerFrame;
    public float gravity;
    public Vector2 boundsSize;
    public Vector2Int numCells;
    [Range(0,1)] 
    public float collisionDamping = 0.05f;

    [Header("Interaction Settings")] 
    public float interactionRadius;
    public float interactionStrength;

    [Header("References")] 
    public ComputeShader compute;
    public Initializer initializer;
    public Display2D display;
    
    // inferred variables
    public Vector2 cellSize { get; private set; }
    private int totalCells;
    private int numVelocities;
    private Initializer.SpawnData spawnData;

    // buffers
    public ComputeBuffer cellTypeBuffer { get; private set; }
    public ComputeBuffer vrVelocityBuffer { get; private set; }
    public ComputeBuffer hrVelocityBuffer { get; private set; }
    private ComputeBuffer hrVelocityBuffer2;
    private ComputeBuffer vrVelocityBuffer2;

    // kernel IDs
    private const int externalForcesKernel = 0;
    private const int updateCellsKernel = 1;
    private const int applyVelocitiesKernel = 2;

    // state
    private bool isPaused;
    private bool pauseNextFrame;
    
    private void Start()
    {
        Debug.Log("Controls: Space = Play/Pause, R = Reset, RightArrow = Step, LeftClick = Attract, RightClick = Repel");
    
        // target 60fps
        float deltaTime = 1 / 60f;
        Time.fixedDeltaTime = deltaTime;
        
        // determine cell size
        cellSize = boundsSize / numCells;
        totalCells = numCells.x * numCells.y;

        // create buffers
        cellTypeBuffer = ComputeHelper.CreateStructuredBuffer<int>(totalCells);
        vrVelocityBuffer = ComputeHelper.CreateStructuredBuffer<float>(totalCells);
        vrVelocityBuffer2 = ComputeHelper.CreateStructuredBuffer<float>(totalCells);
        hrVelocityBuffer = ComputeHelper.CreateStructuredBuffer<float>(totalCells);
        hrVelocityBuffer2 = ComputeHelper.CreateStructuredBuffer<float>(totalCells);
        
        // initialize buffer data
        spawnData = initializer.GetSpawnData(numCells);
        SetInitialBufferData();
        
        // load buffers to compute shaders
        ComputeHelper.SetBuffer(compute, cellTypeBuffer, "cellTypes", externalForcesKernel, updateCellsKernel, applyVelocitiesKernel);
        ComputeHelper.SetBuffer(compute, vrVelocityBuffer, "vrVelocities", externalForcesKernel, applyVelocitiesKernel);
        ComputeHelper.SetBuffer(compute, vrVelocityBuffer2, "vrVelocitiesOut", externalForcesKernel, applyVelocitiesKernel);
        ComputeHelper.SetBuffer(compute, hrVelocityBuffer, "hrVelocities", externalForcesKernel, applyVelocitiesKernel);
        ComputeHelper.SetBuffer(compute, hrVelocityBuffer2, "hrVelocitiesOut", externalForcesKernel, applyVelocitiesKernel);
        
        compute.SetInt("totalCells", totalCells);
        compute.SetInt("numRows", numCells.y);
        compute.SetInt("numCols", numCells.x);
        compute.SetVector("cellSize", cellSize);
        
        // initialize display
        display.Init(this);
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
        UpdateSettings(timeStep);

        // run steps
        for (int i = 0; i < iterationsPerFrame; i++)
        {
            RunSimulationStep();
            SimulationStepCompleted?.Invoke();
        }
    }

    // run one step
    private void RunSimulationStep()
    {
        ComputeHelper.Dispatch(compute, totalCells, kernelIndex: externalForcesKernel);
        ComputeHelper.Dispatch(compute, totalCells, kernelIndex: applyVelocitiesKernel);
        ComputeHelper.Dispatch(compute, totalCells, kernelIndex: updateCellsKernel);
    }

    // update compute shader settings
    private void UpdateSettings(float deltaTime)
    {
        compute.SetFloat("deltaTime", deltaTime);
        compute.SetFloat("gravity", gravity);
        compute.SetFloat("collisionDamping", collisionDamping);
        compute.SetVector("boundsSize", boundsSize);
        
        // mouse interactions
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        int interactionType = 0;
        bool isPush = Input.GetMouseButton(0);
        bool isDelete = Input.GetMouseButton(1);
        if (isPush)
        {
            interactionType = 1;
        } 
        else if (isDelete)
        {
            interactionType = 2;
        }
        
        compute.SetVector("interactionInputPoint", mousePos);
        compute.SetFloat("interactionInputType", interactionType);
        compute.SetFloat("interactionInputRadius", interactionRadius);
    }

    // reset buffer data to spawner data
    private void SetInitialBufferData()
    {
        // get initial cell types
        int[] initialCellTypes = new int[spawnData.cellTypes.Length];
        System.Array.Copy(spawnData.cellTypes, initialCellTypes, spawnData.cellTypes.Length);
        
        // get initial velocities
        float[] initialVrVelocities = new float[spawnData.vrVelocities.Length];
        float[] initialVrVelocities2 = new float[spawnData.vrVelocities.Length];
        float[] initialHrVelocities = new float[spawnData.hrVelocities.Length];
        float[] initialHrVelocities2 = new float[spawnData.hrVelocities.Length];
        System.Array.Copy(spawnData.vrVelocities, initialVrVelocities, spawnData.vrVelocities.Length);
        System.Array.Copy(spawnData.vrVelocities, initialVrVelocities2, spawnData.vrVelocities.Length);
        System.Array.Copy(spawnData.hrVelocities, initialHrVelocities, spawnData.hrVelocities.Length);
        System.Array.Copy(spawnData.hrVelocities, initialHrVelocities2, spawnData.hrVelocities.Length);
        
        // update buffers
        cellTypeBuffer.SetData(initialCellTypes);
        vrVelocityBuffer.SetData(initialVrVelocities);
        vrVelocityBuffer2.SetData(initialVrVelocities2);
        hrVelocityBuffer.SetData(initialHrVelocities);
        hrVelocityBuffer2.SetData(initialHrVelocities2);
    }

    private void HandleInput()
    {
        // SPACE: pause
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isPaused = !isPaused;
            if (isPaused) Debug.Log("Paused");
            else Debug.Log("Unpaused");
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
        ComputeHelper.Release(cellTypeBuffer, vrVelocityBuffer, vrVelocityBuffer2, hrVelocityBuffer, hrVelocityBuffer2);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 1, 0, 0.4f);
        Gizmos.DrawWireCube(Vector2.zero, boundsSize);

        if (!Application.isPlaying) return;

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
