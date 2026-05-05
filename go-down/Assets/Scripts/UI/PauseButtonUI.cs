using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 顶部左侧的暂停按钮：点击后打开 SettingsPanel（UI Toolkit），由其 OnEnable 调用 UIPause.Acquire 暂停游戏。
/// 不依赖 GameStateManager 的 GameOver 暂停 —— 单纯靠 UIPause 引用计数控制 timeScale。
/// </summary>
[RequireComponent(typeof(Button))]
public class PauseButtonUI : MonoBehaviour
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (button == null) button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveListener(OnClick);
            button.onClick.AddListener(OnClick);
        }
    }

    private void OnDisable()
    {
        if (button != null) button.onClick.RemoveListener(OnClick);
    }

    private void OnClick()
    {
        var panel = SettingsPanel.Instance ?? FindFirstObjectByType<SettingsPanel>(FindObjectsInactive.Include);
        if (panel == null)
        {
            Debug.LogWarning("[PauseButtonUI] SettingsPanel not found in scene.");
            return;
        }

        // 游戏内打开设置：returnTarget = null，关闭时不再激活别的面板
        panel.Show(null);
    }
}
