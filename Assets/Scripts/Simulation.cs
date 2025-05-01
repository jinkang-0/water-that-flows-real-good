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
    private CPUCompute cpuCompute;

    // buffers
    public ComputeBuffer cellTypeBuffer { get; private set; }
    public DoubleBuffer<float2> cellVelocityBuffer { get; private set; }
    public ComputeBuffer cellWeightBuffer { get; private set; }
    public ComputeBuffer positionBuffer { get; private set; }
    public ComputeBuffer particleVelocityBuffer { get; private set; }
    public DoubleBuffer<bool> disabledParticlesBuffer { get; private set; }
    public DoubleBuffer<bool> isCellBucketBuffer { get; private set; }

    // kernel IDs
    private const int simulateParticlesKernel = 0;
    private const int userInputKernel = 1;
    private const int projectionKernel = 2;
    private const int emptyWaterKernel = 3;
    private const int fillWaterKernel = 4;
    private const int normalizeCellVelocityKernel = 5;

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
        cellWeightBuffer = ComputeHelper.CreateStructuredBuffer<float2>(totalCells);
        cellVelocityBuffer = new DoubleBuffer<float2>(totalCells);
        positionBuffer = ComputeHelper.CreateStructuredBuffer<float2>(numParticles);
        particleVelocityBuffer = ComputeHelper.CreateStructuredBuffer<float2>(numParticles);
        disabledParticlesBuffer = new DoubleBuffer<bool>(numParticles);
        isCellBucketBuffer = new DoubleBuffer<bool>(totalCells);
        
        // initialize buffer data
        spawnData = initializer.GetSpawnData(numCells, numParticles);
        SetInitialBufferData();
        
        // load buffers to compute shaders
        ComputeHelper.SetBuffer(compute, cellTypeBuffer, "cellTypes", simulateParticlesKernel, userInputKernel, projectionKernel, emptyWaterKernel, fillWaterKernel, normalizeCellVelocityKernel);
        ComputeHelper.SetBuffer(compute, cellWeightBuffer, "cellWeights", emptyWaterKernel, normalizeCellVelocityKernel);
        ComputeHelper.SetBuffer(compute, particleVelocityBuffer, "particleVelocities", simulateParticlesKernel);
        ComputeHelper.SetBuffer(compute, positionBuffer, "particlePositions", simulateParticlesKernel, fillWaterKernel);

        // set compute shader constants
        compute.SetInt("totalCells", totalCells);
        compute.SetInt("numParticles", numParticles);
        compute.SetInts("size", new int[]{numCells.x, numCells.y});
        compute.SetVector("cellSize", cellSize);
        compute.SetFloat("interactionInputStrength", interactionStrength);
        compute.SetFloat("particleRadius", particleRadius);
        
        // initialize display
        display.Init(this);
        
        // initialize cpu compute
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
        // simulate particle physics
        ComputeHelper.Dispatch(compute, numParticles, kernelIndex: simulateParticlesKernel);
        
        // clear water cells, setup for velocity transfer
        ComputeHelper.SetBuffer(compute, cellVelocityBuffer.bufferRead, "cellVelocitiesIn", emptyWaterKernel);
        ComputeHelper.SetBuffer(compute, cellVelocityBuffer.bufferWrite, "cellVelocitiesOut", emptyWaterKernel);
        ComputeHelper.Dispatch(compute, totalCells, kernelIndex: emptyWaterKernel);
        
        // update water cells based on particle position
        ComputeHelper.Dispatch(compute, numParticles, kernelIndex: fillWaterKernel);
        
        // transfer velocity from particle to grid
        VelocityTransferParticle();

        // normalize cell velocity based on weights
        ComputeHelper.SetBuffer(compute, cellVelocityBuffer.bufferRead, "cellVelocitiesIn", normalizeCellVelocityKernel);
        ComputeHelper.SetBuffer(compute, cellVelocityBuffer.bufferWrite, "cellVelocitiesOut", normalizeCellVelocityKernel);
        ComputeHelper.Dispatch(compute, totalCells, kernelIndex: normalizeCellVelocityKernel);
        cellVelocityBuffer.Swap();

        // solve incompressibility
        // int numIterations = 50;
        // for (int i = 0; i < numIterations; i++)
        // {
        //     ComputeHelper.SetBuffer(compute, cellVelocityBuffer.bufferRead, "cellVelocitiesIn", projectionKernel);
        //     ComputeHelper.SetBuffer(compute, cellVelocityBuffer.bufferWrite, "cellVelocitiesOut", projectionKernel);
        //     ComputeHelper.Dispatch(compute, totalCells, kernelIndex: projectionKernel);
        //     cellVelocityBuffer.Swap();
        // }
        SolveIncompressibility();
        cellVelocityBuffer.SyncToWrite();

        // transfer velocity from grid to particle
        VelocityTransferGrid();

        // handle mouse interactions
        // ComputeHelper.SetBuffer(compute, cellVelocityBuffer.bufferRead, "cellVelocitiesIn", userInputKernel);
        // ComputeHelper.SetBuffer(compute, cellVelocityBuffer.bufferWrite, "cellVelocitiesOut", userInputKernel);
        // ComputeHelper.Dispatch(compute, totalCells, kernelIndex: userInputKernel);
        // cellVelocityBuffer.Swap();
    }

    private void VelocityTransferParticle()
    {
        // copy buffer to CPU
        int[] cellTypes = CPUCompute.LoadIntBuffer(cellTypeBuffer, totalCells);
        float2[] cellVelocities = CPUCompute.LoadFloat2Buffer(cellVelocityBuffer.bufferWrite, totalCells);
        float2[] cellWeights = CPUCompute.LoadFloat2Buffer(cellWeightBuffer, totalCells);
        float2[] particlePositions = CPUCompute.LoadFloat2Buffer(positionBuffer, numParticles);
        float2[] particleVelocities = CPUCompute.LoadFloat2Buffer(particleVelocityBuffer, numParticles);
        bool[] disabledParticles = CPUCompute.LoadBoolBuffer(disabledParticlesBuffer.bufferWrite, numParticles);
        bool[] isCellBucket = CPUCompute.LoadBoolBuffer(disabledParticlesBuffer.bufferWrite, numParticles);
        
        // transfer velocity on CPU
        cpuCompute.VelocityTransferParticle(cellVelocities, cellWeights, particlePositions, particleVelocities, disabledParticles, isCellBucket, cellTypes);
        
        // copy buffer to GPU
        cellVelocityBuffer.bufferWrite.SetData(cellVelocities);
        cellWeightBuffer.SetData(cellWeights);
        positionBuffer.SetData(particlePositions);
        particleVelocityBuffer.SetData(particleVelocities);
        disabledParticlesBuffer.bufferWrite.SetData(disabledParticles);
    }

    private void VelocityTransferGrid()
    {
        // copy buffer to CPU
        int[] cellTypes = CPUCompute.LoadIntBuffer(cellTypeBuffer, totalCells);
        float2[] cellVelocities = CPUCompute.LoadFloat2Buffer(cellVelocityBuffer.bufferRead, totalCells);
        float2[] particlePositions = CPUCompute.LoadFloat2Buffer(positionBuffer, numParticles);
        float2[] particleVelocities = CPUCompute.LoadFloat2Buffer(particleVelocityBuffer, numParticles);
        bool[] disabledParticles = CPUCompute.LoadBoolBuffer(disabledParticlesBuffer.bufferRead, numParticles);
        bool[] isCellBucket = CPUCompute.LoadBoolBuffer(isCellBucketBuffer.bufferWrite, numParticles);
        
        // transfer velocity on CPU
        cpuCompute.VelocityTransferGrid(cellTypes, cellVelocities, particlePositions, particleVelocities, disabledParticles, isCellBucket);
        
        // copy buffer to GPU
        particleVelocityBuffer.SetData(particleVelocities);
    }

    private void SolveIncompressibility()
    {
        // copy buffers to CPU
        int[] cellTypes = CPUCompute.LoadIntBuffer(cellTypeBuffer, totalCells);
        float2[] cellVelocities = CPUCompute.LoadFloat2Buffer(cellVelocityBuffer.bufferRead, totalCells);
        
        cpuCompute.SolveIncompressibility(cellVelocities, cellTypes, 50, 1.9f);
        
        // copy buffer to GPU
        cellVelocityBuffer.bufferRead.SetData(cellVelocities);
    }

    // counts the number of disabled particles
    private int NumDisabledParticles(bool[]disabledParticles) {
        int count = 0;
        for (int i = 0; i < disabledParticles.Length; i++) {
            if (disabledParticles[i]) {
                count++;
            }
        }
        return count;
    }

    // checks if the game is over
    private bool GameWon(bool[]disabledParticles) {
        if (NumDisabledParticles(disabledParticles) >= 0.3 * numParticles) {
            return true;
        }
        return false;
    }
    
    
    // update compute shader settings
    private void UpdateSettings(float deltaTime)
    {
        compute.SetFloat("deltaTime", deltaTime);
        compute.SetFloat("gravity", gravity);
        compute.SetVector("boundsSize", boundsSize);
        
        // mouse interactions
        if (Camera.main != null)
        {
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
        }

        compute.SetFloat("interactionInputRadius", interactionRadius);
    }

    // reset buffer data to spawner data
    private void SetInitialBufferData()
    {
        // update buffers
        cellTypeBuffer.SetData(spawnData.cellTypes);
        cellWeightBuffer.SetData(spawnData.cellWeights);
        cellVelocityBuffer.bufferRead.SetData(spawnData.cellVelocities);
        cellVelocityBuffer.bufferWrite.SetData(spawnData.cellVelocities);
        particleVelocityBuffer.SetData(spawnData.particleVelocities);
        positionBuffer.SetData(spawnData.positions);
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
        ComputeHelper.Release(cellTypeBuffer);
        ComputeHelper.Release(positionBuffer);
        ComputeHelper.Release(particleVelocityBuffer);
        ComputeHelper.Release(cellWeightBuffer);
        cellVelocityBuffer.Destroy();
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
