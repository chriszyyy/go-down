using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class ScoreTextUI : MonoBehaviour
{
    [Tooltip("若不填则自动使用同物体上的 Text")]
    public Text scoreText;

    [Tooltip("显示格式，例如：Score: {0}")]
    public string format = "Score: {0}";

    private void Awake()
    {
        if (scoreText == null)
        {
            scoreText = GetComponent<Text>();
        }
    }

    private void OnEnable()
    {
        if (ScoreManager.Instance == null)
        {
            Debug.LogWarning("[ScoreTextUI] Scene missing ScoreManager. Create a GameObject with ScoreManager attached.");
            return;
        }

        ScoreManager.Instance.OnScoreChanged += HandleScoreChanged;
        HandleScoreChanged(ScoreManager.Instance.CurrentScore);
    }

    private void OnDisable()
    {
        if (ScoreManager.Instance == null) return;
        ScoreManager.Instance.OnScoreChanged -= HandleScoreChanged;
    }

    private void HandleScoreChanged(int score)
    {
        if (scoreText == null) return;
        scoreText.text = string.Format(format, score);
    }
}
