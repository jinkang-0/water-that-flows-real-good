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
    [Range(0,1)] 
    public float collisionDamping = 0.05f;

    [Header("References")] 
    public ComputeShader compute;
    public ParticleSpawner spawner;
    public Display2D display;
    
    // buffers
    public ComputeBuffer positionBuffer { get; private set; }
    public ComputeBuffer velocityBuffer { get; private set; }
    
    // kernel IDs
    private const int externalForcesKernel = 0;
    private const int updatePositionKernel = 1;

    // state
    private bool isPaused;
    private bool pauseNextFrame;
    private ParticleSpawner.ParticleSpawnData spawnData;
    
    public int numParticles { get; private set; }

    private void Start()
    {
        // target 60fps
        float deltaTime = 1 / 60f;
        Time.fixedDeltaTime = deltaTime;
        
        // initialize spawner data
        spawnData = spawner.GetSpawnData();
        numParticles = spawnData.positions.Length;
        
        // create buffers
        positionBuffer = ComputeHelper.CreateStructuredBuffer<float2>(numParticles);
        velocityBuffer = ComputeHelper.CreateStructuredBuffer<float2>(numParticles);
        
        // initialize buffer data
        SetInitialBufferData(spawnData);
        
        // load buffers to compute shaders
        ComputeHelper.SetBuffer(compute, positionBuffer, "positions", externalForcesKernel, updatePositionKernel);
        ComputeHelper.SetBuffer(compute, velocityBuffer, "velocities", externalForcesKernel, updatePositionKernel);
        
        compute.SetInt("numParticles", numParticles);
        
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
        ComputeHelper.Dispatch(compute, numParticles, kernelIndex: externalForcesKernel);
        ComputeHelper.Dispatch(compute, numParticles, kernelIndex: updatePositionKernel);
    }

    // update compute shader settings
    private void UpdateSettings(float deltaTime)
    {
        compute.SetFloat("deltaTime", deltaTime);
        compute.SetFloat("gravity", gravity);
        compute.SetFloat("collisionDamping", collisionDamping);
        compute.SetVector("boundsSize", boundsSize);
        
        // mouse interactions
        // Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    }
    
    // reset buffer data to spawner data
    private void SetInitialBufferData(ParticleSpawner.ParticleSpawnData spawnData)
    {
        // get initial positions
        float2[] initialPos = new float2[spawnData.positions.Length];
        System.Array.Copy(spawnData.positions, initialPos, spawnData.positions.Length);
        
        // update buffers
        positionBuffer.SetData(initialPos);
        velocityBuffer.SetData(spawnData.velocities);
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
            SetInitialBufferData(spawnData);
        }
    }

    private void OnDestroy()
    {
        ComputeHelper.Release(positionBuffer, velocityBuffer);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 1, 0, 0.4f);
        Gizmos.DrawWireCube(Vector2.zero, boundsSize);
    }
}
