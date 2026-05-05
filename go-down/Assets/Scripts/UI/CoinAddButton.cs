using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 顶部 CoinStat 上的"+"按钮：点击后打开商店面板（UI Toolkit 版 ShopPanel），
/// 同时由 ShopPanel 的 OnEnable 调用 UIPause.Acquire() 暂停游戏。
/// 关闭时 ShopPanel.Hide 会激活 returnTarget；游戏内打开时 returnTarget = null，
/// 只需关闭自己即可（UIPause 在 OnDisable 释放，timeScale 自动恢复）。
/// </summary>
[RequireComponent(typeof(Button))]
public class CoinAddButton : MonoBehaviour
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
        var panel = ShopPanel.Instance ?? FindFirstObjectByType<ShopPanel>(FindObjectsInactive.Include);
        if (panel == null)
        {
            Debug.LogWarning("[CoinAddButton] ShopPanel not found in scene.");
            return;
        }

        // 游戏内打开商店：returnTarget = null，关闭时不需要重新激活别的面板
        panel.Show(null);
    }
}
