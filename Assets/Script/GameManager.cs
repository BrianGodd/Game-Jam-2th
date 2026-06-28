using MissionSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    const int MaxDay = 4;

    static GameManager instance;
    static int currentDay;
    static bool hasRuntimeDay;

    static public GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<GameManager>();
            }
            return instance;
        }
    }

    public static int CurrentDay => hasRuntimeDay ? currentDay : 1;

    [SerializeField] private int startingDay = 1;
    [SerializeField] private float gameLengthMinutes = 5;
    [SerializeField] private GameObjective gameObjective;

    [Header("Runtime")]
    [SerializeField] private float timeCompleteness = 0f;
    [SerializeField] private float elapsedTimeSeconds = 0f;

    public float TimeCompleteness => timeCompleteness;
    public float ElapsedTimeSeconds => elapsedTimeSeconds;
    public float RemainingTimeSeconds => Mathf.Max(0f, gameLengthMinutes * 60f - elapsedTimeSeconds);

    public event Action OnGameCleanup;

    private void Awake()
    {
        instance = this;

        if (!hasRuntimeDay)
        {
            currentDay = Mathf.Clamp(startingDay, 1, MaxDay);
            hasRuntimeDay = true;
        }
    }

    private void Start()
    {
        StartCoroutine(MainGameLoop());
    }

    IEnumerator GameTimer()
    {
        float gameLengthSeconds = gameLengthMinutes * 60f;
        elapsedTimeSeconds = 0f;
        while (elapsedTimeSeconds < gameLengthSeconds)
        {
            elapsedTimeSeconds += Time.deltaTime;
            timeCompleteness = elapsedTimeSeconds / gameLengthSeconds;
            yield return null;
        }

        elapsedTimeSeconds = gameLengthSeconds;
        timeCompleteness = 1f;
    }
    
    IEnumerator MainGameLoop()
    {
        var gameTimer = StartCoroutine(GameTimer());

        float gameLengthSeconds = gameLengthMinutes * 60f;
        while (elapsedTimeSeconds < gameLengthSeconds)
        {
            if (gameObjective.IsCompleted())
            {
                StopCoroutine(gameTimer);
                WinGame();
                yield break;
            }
            yield return null;
        }

        yield return gameTimer;

        // Game time over. check if all missions are completed
        if (gameObjective.IsCompleted())
        {
            WinGame();
        }
        else
        {
            LoseGame();
        }
        // the game will be cleaned up in WinGame or LoseGame
        // don't put any logic after this point in the main game loop
    }

    public void LoseGame(bool playJumpscare = true)
    {
        if (playJumpscare)
        {
            Horror.MonsterPresenceDirector.Instance.StartJumpscare();
            return;
        }

        Debug.Log($"Day {CurrentDay}: You Lose!");
        CleanupGame();
        ReloadActiveScene();
    }

    public void WinGame()
    {
        if (CurrentDay >= MaxDay)
        {
            Debug.Log("Game Over: You Win!");
            UIManager.TriggerYouWin();
            currentDay = 1;
            CleanupGame();
            ReloadActiveScene();
            return;
        }

        currentDay++;
        Debug.Log($"Day {currentDay - 1} complete. Loading Day {currentDay}.");
        CleanupGame();
        ReloadActiveScene();
    }

    private void Update()
    {
        bool nextDayPressed = false;
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.nKey.wasPressedThisFrame)
        {
            nextDayPressed = true;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (!nextDayPressed && Input.GetKeyDown(KeyCode.N))
        {
            nextDayPressed = true;
        }
#endif

        if (nextDayPressed)
        {
            Debug.Log("[Debug] Direct transition to the next day triggered.");
            WinGame();
        }
    }

    public void CleanupGame()
    {
        OnGameCleanup?.Invoke();
    }

    private void ReloadActiveScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
