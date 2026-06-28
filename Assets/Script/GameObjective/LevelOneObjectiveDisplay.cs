using TMPro;
using UnityEngine;

public class LevelOneObjectiveDisplay : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private LevelOneObjective objective;
    [SerializeField] private TMP_Text statusText;

    private void Update()
    {
        statusText.text = BuildStatusText();
    }

    private string BuildStatusText()
    {
        float remainingMinutes = gameManager.RemainingTimeSeconds / 60f;
        string timeText = remainingMinutes.ToString("00.0");

        int lightsOn = objective.LightsOnCount;
        int unlockedDoors = objective.UnlockedDoorCount;

        if (objective.IsCompleted())
        {
            return $"Time: {timeText}\nLights on: {lightsOn}\nDoors unlocked: {unlockedDoors}\nObjectives complete";
        }

        return $"Time: {timeText}\nLights on: {lightsOn}\nDoors unlocked: {unlockedDoors}";
    }
}
