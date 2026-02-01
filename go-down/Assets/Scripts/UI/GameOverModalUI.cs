using UnityEngine;
using UnityEngine.UI;

public class GameOverModalUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("可选：遮罩根节点；为空则使用当前物体")]
    public GameObject modalRoot;

    [Tooltip("可选：显示 GameOver 原因的文本")]
    public Text reasonText;

    [Tooltip("重新游戏按钮（可选，不填则自动在子物体里找 Button）")]
    public Button restartButton;

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

        if (reasonText != null)
        {
            reasonText.text = string.IsNullOrEmpty(reason) ? "Game Over" : reason;
        }
    }

    public void Hide()
    {
        if (modalRoot != null) modalRoot.SetActive(false);
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
