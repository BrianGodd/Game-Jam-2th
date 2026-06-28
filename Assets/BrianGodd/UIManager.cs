using StarterAssets;
using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject titleCanvas;
    [SerializeField] private GameObject settingCanvas;
    [SerializeField] private FirstPersonController firstPersonController;
    [SerializeField] private TMP_Text dayText;
    [SerializeField] private TMP_Text titleText;

    private bool hasStartedGame;
    private bool isGameWonScreen;
    private static bool showYouWin = false;

    public static void TriggerYouWin()
    {
        showYouWin = true;
    }

    private void Start()
    {
        titleCanvas.SetActive(true);
        settingCanvas.SetActive(false);
        firstPersonController.enabled = false;

        SetCursorState(isGameplayActive: false);

        if (showYouWin)
        {
            isGameWonScreen = true;
            showYouWin = false;
        }

        if (dayText != null)
        {
            dayText.text = isGameWonScreen ? "" : ("Day " + GameManager.CurrentDay);
        }

        if (isGameWonScreen && titleText != null)
        {
            titleText.text = "You Win!!!";
        }
    }

    private void Update()
    {
        if (isGameWonScreen)
        {
            return;
        }

        if (!hasStartedGame)
        {
            if (Input.anyKeyDown)
            {
                StartGame();
            }

            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleSettings();
        }
    }

    private void StartGame()
    {
        hasStartedGame = true;
        titleCanvas.SetActive(false);
        settingCanvas.SetActive(false);
        firstPersonController.enabled = true;

        SetCursorState(isGameplayActive: true);
    }

    private void ToggleSettings()
    {
        bool shouldOpenSettings = !settingCanvas.activeSelf;
        settingCanvas.SetActive(shouldOpenSettings);
        firstPersonController.enabled = !shouldOpenSettings;

        SetCursorState(isGameplayActive: !shouldOpenSettings);
    }

    private void SetCursorState(bool isGameplayActive)
    {
        Cursor.visible = !isGameplayActive;
        Cursor.lockState = isGameplayActive ? CursorLockMode.Locked : CursorLockMode.None;
    }
}
