using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{

    public void PlayGame() {
        SceneManager.LoadSceneAsync(1);
    }

    public void SpecialDemo1() {
        SceneManager.LoadSceneAsync(9);
    }

        public void SpecialDemo2() {
        SceneManager.LoadSceneAsync(10);
    }

    public void QuitGame() {
       Application.Quit();
    }
}
