using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class GameOverBoundary : MonoBehaviour
{
    [Tooltip("Optional: if set, only this tag triggers GameOver")]
    public string requiredTag = "HexagonBall";

    [Tooltip("GameOver reason text")]
    public string reason = "Ball hit boundary";

    private void Reset()
    {
        Collider2D c = GetComponent<Collider2D>();
        if (c != null) c.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
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
}
