using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WonTrigger : MonoBehaviour
{
    
    public Simulation sim;

    void Update() {
        if (sim.score >= sim.numParticles * 2/3)
        {
            SceneManager.LoadScene(2);
        }
    }

}
