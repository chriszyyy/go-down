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

    [Tooltip("重新游戏按钮（可选，不填则自动在子物体里找 Button）")]
    public Button restartButton;

    [Tooltip("设置/主界面按钮（可选）。点击后打开 StartMenu 主界面。")]
    public Button settingsButton;

    [Tooltip("可选：StartMenuUI 引用；为空则运行时自动查找。")]
    public StartMenuUI startMenuUI;

    // 记录当前是否持有 UIPause refcount，避免 Show/Hide 不配对造成 refcount 错位。
    private bool _heldUIPause;

    [Header("Copy")]
    [Tooltip("scoreValueText 的格式，{0}=得分数字")]
    public string scoreValueFormat = "{0}";

    [Tooltip("highScoreText 的格式，{0}=最高分数字")]
    public string highScoreFormat = "最高分：{0}";

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

        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveListener(OpenSettingsMenu);
            settingsButton.onClick.AddListener(OpenSettingsMenu);
        }

        if (startMenuUI == null)
        {
            startMenuUI = FindObjectOfType<StartMenuUI>(includeInactive: true);
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

        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveListener(OpenSettingsMenu);
        }
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

        // 获取一次 UIPause，这样在 GameOver 面板上叠加子面板（如 Settings）后，
        // 子面板关闭时 refcount 仍为 1，timeScale 保持 0，不会提前息复游戏。
        if (!_heldUIPause)
        {
            UIPause.Acquire();
            _heldUIPause = true;
        }

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

        // Hide HUD elements that would show through the modal.
        SetHudVisibility(false);
    }

    public void Hide()
    {
        if (modalRoot != null) modalRoot.SetActive(false);

        if (_heldUIPause)
        {
            UIPause.Release();
            _heldUIPause = false;
        }

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

    public void OpenSettingsMenu()
    {
        // 不走旧的 StartMenuUI，直接打开 UI Toolkit 的 SettingsPanel。
        // 不隐藏 GameOver：让 SettingsPanel 叠在上面，Back 后仍是 GameOver 状态。
        var panel = SettingsPanel.Instance ?? FindObjectOfType<SettingsPanel>(includeInactive: true);
        if (panel == null)
        {
            Debug.LogWarning("GameOverModalUI: SettingsPanel not found in scene.");
            return;
        }

        // returnTarget = null：Settings 关闭时不重新激活别的面板；GameOver 本身从未被关闭。
        panel.Show(null);
    }
}
