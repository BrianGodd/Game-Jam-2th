using TMPro;
using UnityEngine;

public class LevelOneObjectiveDisplay : MonoBehaviour
{
    [SerializeField] private LevelOneObjective objective;
    [SerializeField] private TMP_Text statusText;

    private void Update()
    {
        statusText.text = BuildStatusText();
    }

    private string BuildStatusText()
    {
        if (objective.IsCompleted())
        {
            return "Objectives complete";
        }

        int lightsOn = objective.LightsOnCount;
        int unlockedDoors = objective.UnlockedDoorCount;

        return $"Lights on: {lightsOn}\nDoors unlocked: {unlockedDoors}";
    }
}
