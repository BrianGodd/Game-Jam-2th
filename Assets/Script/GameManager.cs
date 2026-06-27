using MissionSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    static GameManager instance;
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

    [SerializeField] private float gameLengthMinutes = 5;

    [Header("Runtime")]
    [SerializeField] private float timeCompleteness = 0f;

    public float TimeCompleteness => timeCompleteness;

    public event Action OnGameCleanup;

    private void Start()
    {
        StartCoroutine(MainGameLoop());
    }

    IEnumerator GameTimer()
    {
        float gameLengthSeconds = gameLengthMinutes * 60f;
        float elapsedTime = 0f;
        while (elapsedTime < gameLengthSeconds)
        {
            elapsedTime += Time.deltaTime;
            timeCompleteness = elapsedTime / gameLengthSeconds;
            yield return null;
        }
    }
    
    IEnumerator MainGameLoop()
    {
        var gameTimer = StartCoroutine(GameTimer());
        yield return gameTimer;

        // Game time over. check if all missions are completed

        bool missionCompleted = !MissionManager.Instance.HasIncompleteMission;
        if(missionCompleted)
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

    public void LoseGame()
    {
        // TODO: Implement game lose logic
        Debug.Log("Game Over: You Lose!");

        CleanupGame();
    }

    public void WinGame()
    {
        // TODO: Implement game win logic
        Debug.Log("Game Over: You Win!");

        CleanupGame();
    }

    public void CleanupGame()
    {
        OnGameCleanup?.Invoke();
    }
}
