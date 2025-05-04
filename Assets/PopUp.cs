using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class PopUp : MonoBehaviour
{
    public GameObject popupObject;
    public Simulation sim;
    public int levelnum;

    

    public void Update() {
        Dictionary<int, int> levelScores = new Dictionary<int, int>
        {
            { 1, sim.numParticles * 3/4 },  
            { 2, sim.numParticles * 1/10 },  
            { 3, sim.numParticles * 3/4 },
            { 4, sim.numParticles * 3/4 },
            { 5, sim.numParticles * 3/4 },   
            { 6, sim.numParticles * 3/4 },
            { 7, sim.numParticles * 1/10 },
            { 8, sim.numParticles * 1/50 }, 
            { 9, sim.numParticles * 3/4 } 
        };

        if (sim.score >= levelScores[levelnum] && !popupObject.activeSelf)
        {
            popupObject.SetActive(true);
        }
    }

    public void ReturnToMenu() {
        SceneManager.LoadSceneAsync(0);
    }
}
