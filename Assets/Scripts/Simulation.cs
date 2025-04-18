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
    private Initializer.SpawnData spawnData;

    // buffers
    public ComputeBuffer cellTypeBuffer { get; private set; }
    public DoubleBufferHelper<Vector2> velocitiesBuffer { get; private set; }
    public DoubleBufferHelper<float> pressuresBuffer { get; private set; }

    // kernel IDs
    private int userInputKernel = 0;
    private int advectKernel = 0;
    private int diffuseKernel = 0;
    private int divergenceKernel = 0;
    private int gradientKernel = 0;

    // state
    private bool isPaused;
    private bool pauseNextFrame;
    
    private void Start()
    {
        userInputKernel = compute.FindKernel("user_input");
        advectKernel = compute.FindKernel("advect");
        diffuseKernel = compute.FindKernel("jacobi_diffuse");
        divergenceKernel = compute.FindKernel("projection_divergence");
        gradientKernel = compute.FindKernel("gradient_subtraction");

        Debug.Log("Controls: Space = Play/Pause, R = Reset, RightArrow = Step, RightClick = Delete");
    
        // target 60fps
        float deltaTime = 1 / 60f;
        Time.fixedDeltaTime = deltaTime;
        
        // determine cell size
        cellSize = boundsSize / numCells;
        totalCells = numCells.x * numCells.y;

        // create buffers
        //cellTypeBuffer = new DoubleBufferHelper<uint>(totalCells);
        cellTypeBuffer = ComputeHelper.CreateStructuredBuffer<int>(totalCells);
        velocitiesBuffer = new DoubleBufferHelper<Vector2>(totalCells);
        pressuresBuffer = new DoubleBufferHelper<float>(totalCells);
        
        // initialize buffer data
        spawnData = initializer.GetSpawnData(numCells);
        SetInitialBufferData();
        
        compute.SetInts("size", new int[]{numCells.x, numCells.y});
        compute.SetVector("cell_size", cellSize);
        
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
        // update state according to user input
        compute.SetBuffer(userInputKernel, "inout_cell_types", cellTypeBuffer);
        compute.SetBuffer(userInputKernel, "in_velocities", velocitiesBuffer.buffer_read);
        compute.SetBuffer(userInputKernel, "out_velocities", velocitiesBuffer.buffer_write);
        ComputeHelper.Dispatch(compute, numCells.x, numCells.y, kernelIndex: userInputKernel);
        velocitiesBuffer.swap();

        // advect
        compute.SetBuffer(advectKernel, "inout_cell_types", cellTypeBuffer);
        compute.SetBuffer(advectKernel, "in_velocities", velocitiesBuffer.buffer_read);
        compute.SetBuffer(advectKernel, "out_velocities", velocitiesBuffer.buffer_write);
        ComputeHelper.Dispatch(compute, numCells.x, numCells.y, kernelIndex: advectKernel);
        velocitiesBuffer.swap();

        // diffuse
        // run jacobi iterations to find solution
        
        for (int i = 0; i < 80; ++i)
        {
            compute.SetBuffer(diffuseKernel, "inout_cell_types", cellTypeBuffer);
            compute.SetBuffer(diffuseKernel, "in_velocities", velocitiesBuffer.buffer_read);
            compute.SetBuffer(diffuseKernel, "out_velocities", velocitiesBuffer.buffer_write);
            ComputeHelper.Dispatch(compute, numCells.x, numCells.y, kernelIndex: diffuseKernel);
            velocitiesBuffer.swap();
        }
        
        
        compute.SetBuffer(divergenceKernel, "inout_cell_types", cellTypeBuffer);
        compute.SetBuffer(divergenceKernel, "in_velocities", velocitiesBuffer.buffer_read);
        compute.SetBuffer(divergenceKernel, "out_pressures", pressuresBuffer.buffer_write);
        ComputeHelper.Dispatch(compute, numCells.x, numCells.y, kernelIndex: divergenceKernel);
        pressuresBuffer.swap();
        
        compute.SetBuffer(gradientKernel, "inout_cell_types", cellTypeBuffer);
        compute.SetBuffer(gradientKernel, "in_velocities", velocitiesBuffer.buffer_read);
        compute.SetBuffer(gradientKernel, "out_velocities", velocitiesBuffer.buffer_write);
        compute.SetBuffer(gradientKernel, "in_pressures", pressuresBuffer.buffer_read);
        ComputeHelper.Dispatch(compute, numCells.x, numCells.y, kernelIndex: gradientKernel);
        velocitiesBuffer.swap();
    }

    // update compute shader settings
    private void UpdateSettings(float deltaTime)
    {
        compute.SetFloat("delta_time", deltaTime);
        compute.SetFloat("gravity", gravity);
        compute.SetVector("bounds_size", boundsSize);
        
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
        
        compute.SetVector("interaction_pos", mousePos);
        compute.SetInt("interaction_type", interactionType);
        compute.SetFloat("interaction_radius", interactionRadius);
    }

    // reset buffer data to spawner data
    private void SetInitialBufferData()
    {
        // get initial cell types
        //int[] initialCellTypes = new int[spawnData.cellTypes.Length];
        //System.Array.Copy(spawnData.cellTypes, initialCellTypes, spawnData.cellTypes.Length);
        
        //// get initial velocities
        //Vector2[] initialVrVelocities = new Vector2[spawnData.velocities.Length];
        //float[] initialPressures = new float[spawnData.Pressures.Length];
        //float[] initialPressures2 = new float[spawnData.Pressures.Length];
        //System.Array.Copy(spawnData.vrVelocities, initialVrVelocities, spawnData.vrVelocities.Length);
        //System.Array.Copy(spawnData.vrVelocities, initialVrVelocities2, spawnData.vrVelocities.Length);
        //System.Array.Copy(spawnData.hrVelocities, initialHrVelocities, spawnData.hrVelocities.Length);
        //System.Array.Copy(spawnData.hrVelocities, initialHrVelocities2, spawnData.hrVelocities.Length);
        //System.Array.Copy(spawnData.Pressures, initialPressures, spawnData.Pressures.Length);
        //System.Array.Copy(spawnData.Pressures, initialPressures2, spawnData.Pressures.Length);
        
        // update buffers
        cellTypeBuffer.SetData(spawnData.cellTypes);
        velocitiesBuffer.buffer_read.SetData(spawnData.velocities);
        pressuresBuffer.buffer_read.SetData(spawnData.Pressures);
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

    private void OnDestroy()
    {
        cellTypeBuffer.Release();
        velocitiesBuffer.destroy();
        pressuresBuffer.destroy();
    }
}
