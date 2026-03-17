using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
public class GameOverBoundary : MonoBehaviour
{
    [Tooltip("Optional: if set, only this tag triggers GameOver")]
    public string requiredTag = "HexagonBall";

    [Tooltip("GameOver reason text")]
    public string reason = "Ball hit boundary";

    [Header("Out-of-Bounds Cleanup")]
    [Tooltip("When non-ball TowerBlocks touch this boundary, auto-destroy them after delay to avoid off-screen accumulation.")]
    public bool autoCleanupNonBallBlocks = true;

    [Tooltip("Delay before destroying out-of-bounds TowerBlocks (seconds, unscaled time).")]
    public float cleanupDelaySeconds = 5f;

    private readonly HashSet<int> scheduledCleanup = new HashSet<int>();

    private void Reset()
    {
        Collider2D c = GetComponent<Collider2D>();
        if (c != null) c.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryScheduleOutOfBoundsCleanup(other);

        if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag))
        {
            return;
        }

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.GameOver(reason);
        }
        else
        {
            Debug.LogWarning("GameOverBoundary: no GameStateManager in scene.");
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        scheduledCleanup.Clear();
    }

    private void TryScheduleOutOfBoundsCleanup(Collider2D other)
    {
        if (!autoCleanupNonBallBlocks || other == null) return;

        // Never auto-cleanup the main ball.
        if (!string.IsNullOrEmpty(requiredTag) && other.CompareTag(requiredTag)) return;

        TowerBlock block = other.GetComponent<TowerBlock>();
        if (block == null) block = other.GetComponentInParent<TowerBlock>();
        if (block == null) return;

        int id = block.GetInstanceID();
        if (scheduledCleanup.Contains(id)) return;

        scheduledCleanup.Add(id);
        StartCoroutine(DestroyBlockAfterDelay(block, id));
    }

    private System.Collections.IEnumerator DestroyBlockAfterDelay(TowerBlock block, int id)
    {
        float delay = Mathf.Max(0.05f, cleanupDelaySeconds);
        yield return new WaitForSecondsRealtime(delay);

        if (block != null)
        {
            // Direct destroy: no score, no click-destroy event.
            Destroy(block.gameObject);
        }

        scheduledCleanup.Remove(id);
    }
}
