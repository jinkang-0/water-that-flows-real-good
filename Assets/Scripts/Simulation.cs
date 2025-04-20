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
    public DoubleBuffer<float> densityBuffer { get; private set; }
    public DoubleBuffer<float> vrVelocityBuffer { get; private set; }
    public DoubleBuffer<float> hrVelocityBuffer { get; private set; }

    // kernel IDs
    private const int externalForcesKernel = 0;
    private const int updateCellsKernel = 1;
    private const int projectionKernel = 2;
    private const int advectVelocityKernel = 3;
    private const int advectFluidKernel = 4;

    // state
    private bool isPaused;
    private bool pauseNextFrame;
    
    private void Start()
    {
        Debug.Log("Controls: Space = Play/Pause, R = Reset, RightArrow = Step, RightClick = Delete");
    
        // target 60fps
        float deltaTime = 1 / 60f;
        Time.fixedDeltaTime = deltaTime;
        
        // determine cell size
        cellSize = boundsSize / numCells;
        totalCells = numCells.x * numCells.y;

        // create buffers
        cellTypeBuffer = ComputeHelper.CreateStructuredBuffer<int>(totalCells);
        densityBuffer = new DoubleBuffer<float>(totalCells);
        vrVelocityBuffer = new DoubleBuffer<float>(totalCells);
        hrVelocityBuffer = new DoubleBuffer<float>(totalCells);
        
        // initialize buffer data
        spawnData = initializer.GetSpawnData(numCells);
        SetInitialBufferData();
        
        // load buffers to compute shaders
        ComputeHelper.SetBuffer(compute, cellTypeBuffer, "cellTypes", updateCellsKernel, projectionKernel, advectFluidKernel, advectVelocityKernel);
        ComputeHelper.SetBuffer(compute, vrVelocityBuffer.bufferRead, "vrVelocities", externalForcesKernel, projectionKernel, advectFluidKernel, advectVelocityKernel);
        ComputeHelper.SetBuffer(compute, vrVelocityBuffer.bufferWrite, "vrVelocitiesOut", externalForcesKernel, projectionKernel, advectFluidKernel, advectVelocityKernel);
        ComputeHelper.SetBuffer(compute, hrVelocityBuffer.bufferRead, "hrVelocities", externalForcesKernel, projectionKernel, advectFluidKernel, advectVelocityKernel);
        ComputeHelper.SetBuffer(compute, hrVelocityBuffer.bufferWrite, "hrVelocitiesOut", externalForcesKernel, projectionKernel, advectFluidKernel, advectVelocityKernel);
        ComputeHelper.SetBuffer(compute, densityBuffer.bufferRead, "densities", projectionKernel, advectFluidKernel);
        ComputeHelper.SetBuffer(compute, densityBuffer.bufferWrite, "densitiesOut", projectionKernel, advectFluidKernel);
        
        compute.SetInt("totalCells", totalCells);
        compute.SetInts("size", new int[]{numCells.x, numCells.y});
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
        ComputeHelper.Dispatch(compute, totalCells, kernelIndex: projectionKernel);
        ComputeHelper.Dispatch(compute, totalCells, kernelIndex: updateCellsKernel);
    }

    // update compute shader settings
    private void UpdateSettings(float deltaTime)
    {
        compute.SetFloat("deltaTime", deltaTime);
        compute.SetFloat("gravity", gravity);
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
        compute.SetInt("interactionInputType", interactionType);
        compute.SetFloat("interactionInputRadius", interactionRadius);
    }

    // reset buffer data to spawner data
    private void SetInitialBufferData()
    {
        // update buffers
        cellTypeBuffer.SetData(spawnData.cellTypes);
        vrVelocityBuffer.bufferRead.SetData(spawnData.vrVelocities);
        hrVelocityBuffer.bufferRead.SetData(spawnData.hrVelocities);
        densityBuffer.bufferRead.SetData(spawnData.densities);
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
        ComputeHelper.Release(cellTypeBuffer);
        vrVelocityBuffer.Destroy();
        hrVelocityBuffer.Destroy();
        densityBuffer.Destroy();
    }

    private void OnDrawGizmos()
    {
        // draw simulation bounds
        Gizmos.color = new Color(0, 1, 0, 0.4f);
        Gizmos.DrawWireCube(Vector2.zero, boundsSize);

        // draw cells
        Vector2 cellSize = boundsSize / numCells;
        Vector2 lowerCorner = -boundsSize / 2;
        for (int y = 0; y < numCells.y; y++)
        {
            for (int x = 0; x < numCells.x; x++)
            {
                Gizmos.DrawWireCube(lowerCorner + new Vector2(x+0.5f, y+0.5f)*cellSize, cellSize);
            }
        }

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
