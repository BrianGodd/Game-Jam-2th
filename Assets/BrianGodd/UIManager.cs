using StarterAssets;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject titleCanvas;
    [SerializeField] private GameObject settingCanvas;
    [SerializeField] private FirstPersonController firstPersonController;

    private bool hasStartedGame;

    private void Start()
    {
        titleCanvas.SetActive(true);
        settingCanvas.SetActive(false);
        firstPersonController.enabled = false;

        SetCursorState(isGameplayActive: false);
    }

    private void Update()
    {
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
