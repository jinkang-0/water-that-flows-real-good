using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class PopUp : MonoBehaviour
{
    public GameObject popupObject;
    public Simulation sim;

    public void Update() {
        if (sim.score >= sim.numParticles * 1/4000 && !popupObject.activeSelf)
        {
            popupObject.SetActive(true);
        }
    }

    public void ReturnToMenu() {
        SceneManager.LoadSceneAsync(0);
    }
}
