using System;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public event Action<int> OnScoreChanged;

    public int CurrentScore { get; private set; }

    [Header("计分规则")]
    [Tooltip("消除一个单格小方块的基础分")]
    public int baseScorePerCell = 10;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Ensure UI shows 0 even before first score event.
        OnScoreChanged?.Invoke(CurrentScore);
    }

    private void OnEnable()
    {
        if (Instance != this)
        {
            // Another instance may have taken over in Awake.
            return;
        }

        TowerBlock.OnBlockScored += HandleBlockScored;

        GameStateManager.OnGameReset += HandleGameReset;

        // Push current score to any UI that just enabled.
        OnScoreChanged?.Invoke(CurrentScore);
    }

    private void OnDisable()
    {
        TowerBlock.OnBlockScored -= HandleBlockScored;

        GameStateManager.OnGameReset -= HandleGameReset;
    }

    private void HandleBlockScored(TowerBlock block, int scoreValue)
    {
        if (block == null) return;

        int points = scoreValue;
        if (points <= 0)
        {
            // 兼容：如果 prefab 没配 scoreValue，就按占用格子数 * baseScorePerCell 计算
            float rotZ = block.transform.eulerAngles.z;
            int cells = 1;
            try
            {
                cells = Mathf.Max(1, block.GetOccupiedCells(rotZ)?.Count ?? 1);
            }
            catch
            {
                cells = 1;
            }

            points = cells * baseScorePerCell;
        }

        AddScore(points);
    }

    private void HandleGameReset()
    {
        ResetScore();
    }

    public void AddScore(int delta)
    {
        if (delta == 0) return;
        CurrentScore += delta;
        OnScoreChanged?.Invoke(CurrentScore);
    }

    public void ResetScore()
    {
        CurrentScore = 0;
        OnScoreChanged?.Invoke(CurrentScore);
    }
}
