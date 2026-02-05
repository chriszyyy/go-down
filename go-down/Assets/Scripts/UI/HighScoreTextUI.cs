using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class HighScoreTextUI : MonoBehaviour
{
    [Tooltip("若不填则自动使用同物体上的 Text")]
    public Text scoreText;

    [Tooltip("显示格式，例如：High: {0}")]
    public string format = "High: {0}";

    private bool subscribed;

    private void Awake()
    {
        if (scoreText == null)
        {
            scoreText = GetComponent<Text>();
        }
    }

    private void OnEnable()
    {
        TrySubscribeAndRefresh();
    }

    private void Update()
    {
        if (!subscribed)
        {
            TrySubscribeAndRefresh();
        }
    }

    private void OnDisable()
    {
        if (subscribed && ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnHighScoreChanged -= HandleHighScoreChanged;
        }

        subscribed = false;
    }

    private void TrySubscribeAndRefresh()
    {
        if (subscribed) return;
        if (scoreText == null) scoreText = GetComponent<Text>();

        if (ScoreManager.Instance == null)
        {
            return;
        }

        ScoreManager.Instance.OnHighScoreChanged += HandleHighScoreChanged;
        subscribed = true;
        HandleHighScoreChanged(ScoreManager.Instance.HighScore);
    }

    private void HandleHighScoreChanged(int score)
    {
        if (scoreText == null) return;
        scoreText.text = string.Format(format, score);
    }
}
