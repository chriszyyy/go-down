using UnityEngine;
using UnityEngine.UI;

public class GameOverModalUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("遮罩根节点；为空则使用当前物体")]
    public GameObject modalRoot;

    [Tooltip("固定标题文字（如'游戏结束'）—— 不会被代码修改内容")]
    public Text titleText;

    [Tooltip("只显示得分数字的 Text（大号，单独一行）")]
    public Text scoreValueText;

    [Tooltip("显示最高分的 Text，{0}=最高分数值")]
    public Text highScoreText;

    [Tooltip("（兼容旧版）一行显示得分的 Text；新布局建议用 scoreValueText 代替")]
    public Text reasonText;

    [Tooltip("重新游戏按钮（可选，不填则自动在子物体里找 Button）")]
    public Button restartButton;

    [Header("Copy")]
    [Tooltip("scoreValueText 的格式，{0}=得分数字")]
    public string scoreValueFormat = "{0}";

    [Tooltip("highScoreText 的格式，{0}=最高分数字")]
    public string highScoreFormat = "最高分：{0}";

    [Tooltip("（兼容旧版）reasonText 的格式，{0}=得分数字")]
    public string gameOverScoreFormat = "游戏结束，你的得分是{0}";

    [Header("Behaviour")]
    [Tooltip("游戏开始时是否隐藏")]
    public bool hideOnStart = true;

    private void Awake()
    {
        if (modalRoot == null) modalRoot = gameObject;
        if (restartButton == null) restartButton = GetComponentInChildren<Button>(includeInactive: true);

        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(RestartGame);
            restartButton.onClick.AddListener(RestartGame);
        }
    }

    private void Start()
    {
        if (hideOnStart)
        {
            Hide();
        }
    }

    private void OnEnable()
    {
        GameStateManager.OnGameOver += HandleGameOver;
        GameStateManager.OnGameReset += HandleGameReset;
    }

    private void OnDisable()
    {
        GameStateManager.OnGameOver -= HandleGameOver;
        GameStateManager.OnGameReset -= HandleGameReset;
    }

    private void HandleGameOver(string reason)
    {
        Show(reason);
    }

    private void HandleGameReset()
    {
        Hide();
    }

    public void Show(string reason = null)
    {
        if (modalRoot != null) modalRoot.SetActive(true);

        int score = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0;
        int high = ScoreManager.Instance != null ? ScoreManager.Instance.HighScore : 0;

        // Large score number — force overflow so it never wraps.
        if (scoreValueText != null)
        {
            scoreValueText.text = string.Format(scoreValueFormat, score);
            scoreValueText.horizontalOverflow = HorizontalWrapMode.Overflow;
            scoreValueText.verticalOverflow = VerticalWrapMode.Overflow;
        }

        // High score line.
        if (highScoreText != null)
        {
            highScoreText.text = string.Format(highScoreFormat, high);
            highScoreText.horizontalOverflow = HorizontalWrapMode.Overflow;
            highScoreText.verticalOverflow = VerticalWrapMode.Overflow;
        }

        // Legacy single-line text (kept for backward compat).
        if (reasonText != null)
        {
            reasonText.text = string.Format(gameOverScoreFormat, score);
            reasonText.horizontalOverflow = HorizontalWrapMode.Overflow;
            reasonText.verticalOverflow = VerticalWrapMode.Overflow;
        }

        // Hide HUD elements that would show through the modal.
        SetHudVisibility(false);
    }

    public void Hide()
    {
        if (modalRoot != null) modalRoot.SetActive(false);

        SetHudVisibility(true);
    }

    private void SetHudVisibility(bool visible)
    {
        // Hide/show the combo progress bar so it doesn't bleed through the GameOver panel.
        BlockClearProgressUI progress = FindObjectOfType<BlockClearProgressUI>(includeInactive: true);
        if (progress != null) progress.SetVisible(visible);
    }

    public void RestartGame()
    {
        // 兼容：如果场景里已有 RestartGameButton，就直接调用它。
        RestartGameButton existing = FindObjectOfType<RestartGameButton>();
        if (existing != null)
        {
            existing.RestartGame();
            return;
        }

        // 兜底：至少要把 GameOver 状态清掉
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.ResetGameState();
        }

        // 兜底：直接调用 TowerBuilder.ResetTower()
        TowerBuilder towerBuilder = FindObjectOfType<TowerBuilder>();
        if (towerBuilder != null)
        {
            towerBuilder.ResetTower();
        }
    }
}
