using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 起始菜单（UI Toolkit 版本）。
/// 挂在带 <see cref="UIDocument"/> 的 GameObject 上，
/// 通过 Q&lt;T&gt;("name") 找到 UXML 中的元素并绑定回调。
/// 当前仅保留三个主按钮：开始游戏 / 商店 / 设置。
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class StartMenuPanel : MonoBehaviour
{
    private Button playButton;
    private Button shopButton;
    private Button optionsButton;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        if (root == null) return;

        playButton = root.Q<Button>("play-btn");
        shopButton = root.Q<Button>("shop-btn");
        optionsButton = root.Q<Button>("options-btn");

        if (playButton != null) playButton.clicked += OnPlay;
        if (shopButton != null) shopButton.clicked += OnShop;
        if (optionsButton != null) optionsButton.clicked += OnOptions;
    }

    private void OnDisable()
    {
        if (playButton != null) playButton.clicked -= OnPlay;
        if (shopButton != null) shopButton.clicked -= OnShop;
        if (optionsButton != null) optionsButton.clicked -= OnOptions;
    }

    // ---------------- 点击处理 ----------------
    private void OnPlay()
    {
        Debug.Log("[StartMenu] Play clicked");
        // 隐藏菜单 → 进入游戏
        gameObject.SetActive(false);
    }

    private void OnShop()
    {
        Debug.Log("[StartMenu] Shop clicked");
        var panel = ShopPanel.Instance ?? FindFirstObjectByType<ShopPanel>(FindObjectsInactive.Include);
        if (panel != null)
        {
            gameObject.SetActive(false);
            panel.Show(gameObject);
        }
        else
        {
            Debug.LogWarning("[StartMenu] ShopPanel not found in scene. " +
                             "添加一个带 UIDocument 的 GameObject (Source Asset = Shop.uxml) 并挂上 ShopPanel 脚本。");
        }
    }

    private void OnOptions()
    {
        Debug.Log("[StartMenu] Settings/Options clicked");
        var panel = SettingsPanel.Instance ?? FindFirstObjectByType<SettingsPanel>(FindObjectsInactive.Include);
        if (panel != null)
        {
            gameObject.SetActive(false);
            panel.Show(gameObject);
        }
        else
        {
            Debug.LogWarning("[StartMenu] SettingsPanel not found in scene. " +
                             "添加一个带 UIDocument 的 GameObject (Source Asset = Settings.uxml) 并挂上 SettingsPanel 脚本。");
        }
    }
}
