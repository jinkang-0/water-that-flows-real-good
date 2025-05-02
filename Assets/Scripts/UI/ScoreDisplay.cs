using UnityEngine;
using TMPro;

public class ScoreDisplay : MonoBehaviour
{
    public TextMeshProUGUI scoreTextElement; 
    public Simulation simulation;

    private int displayedScore = -1; 

    void Start()
    {
        
        if (scoreTextElement == null)
        {
            Debug.LogError("Score Text Element not assigned.");
            enabled = false; 
            return;
        }
        if (simulation == null)
        {
            Debug.LogError("Simulation reference not assigned");
            enabled = false;
            return;
        }

        UpdateScoreText();
    }

    void Update()
    {
        if (simulation.score != displayedScore)
        {
            UpdateScoreText();
        }
    }

    void UpdateScoreText()
    {
        displayedScore = simulation.score;
        scoreTextElement.text = $"Score: {displayedScore}";
    }
}