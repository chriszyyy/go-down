using System;
using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    public bool IsGameOver { get; private set; }

    public static event Action<string> OnGameOver;
    public static event Action OnGameReset;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void GameOver(string reason)
    {
        if (IsGameOver) return;
        IsGameOver = true;
        Debug.Log($"GameOver: {reason}");
        OnGameOver?.Invoke(reason);
    }

    public void ResetGameState()
    {
        IsGameOver = false;
        OnGameReset?.Invoke();
    }
}
