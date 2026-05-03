using System;
using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    public bool IsGameOver { get; private set; }

    public static event Action<string> OnGameOver;
    public static event Action OnGameReset;

    [Header("Time & Physics")]
    [Tooltip("游戏结束时是否暂停时间（将 Time.timeScale 置为 0）")]
    public bool pauseTimeOnGameOver = true;

    [Tooltip("重开时恢复的 timeScale（通常为 1）")]
    public float resumeTimeScale = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // 进入 Play 模式时把 timeScale 拉回 1（避免上次 GameOver 残留的 0）。
        // 但要尊重 UI 面板的主动暂停 —— 比如 StartMenu 一上来就调用 UIPause.Acquire()
        // 把 timeScale 设成 0，这里不能盲目覆盖。
        if (pauseTimeOnGameOver && Time.timeScale == 0f && !UIPause.IsPaused)
        {
            Time.timeScale = resumeTimeScale;
        }
    }

    public void GameOver(string reason)
    {
        if (IsGameOver) return;
        IsGameOver = true;

        if (pauseTimeOnGameOver)
        {
            Time.timeScale = 0f;
        }

        Debug.Log($"GameOver: {reason}");
        OnGameOver?.Invoke(reason);
    }

    public void ResetGameState()
    {
        IsGameOver = false;

        if (pauseTimeOnGameOver)
        {
            Time.timeScale = resumeTimeScale;
        }

        OnGameReset?.Invoke();
    }
}
