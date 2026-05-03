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
    [Tooltip("点击 START GAME 时激活的对象（一般是 TowerBuilder）。\n" +
             "默认 inactive，避免起始菜单期间 tower 物理已经在跑、被透过 UI 点到。")]
    [SerializeField] private GameObject gameRoot;

    private Button playButton;
    private Button shopButton;
    private Button optionsButton;

    private void OnEnable()
    {
        UIPause.Acquire(); // 菜单可见 → 暂停游戏，防止 tower 物理、点击穿透

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
        UIPause.Release();

        if (playButton != null) playButton.clicked -= OnPlay;
        if (shopButton != null) shopButton.clicked -= OnShop;
        if (optionsButton != null) optionsButton.clicked -= OnOptions;
    }

    // ---------------- 点击处理 ----------------
    private void OnPlay()
    {
        Debug.Log("[StartMenu] Play clicked");
        // 激活实际游戏（TowerBuilder GameObject 默认 inactive，
        // 这样起始菜单期间 tower 不会被构建、物理也不会跑）
        if (gameRoot != null) gameRoot.SetActive(true);

        // 隐藏菜单 → 进入游戏
        gameObject.SetActive(false);
    }

    private void OnShop()
    {
        Debug.Log("[StartMenu] Shop clicked");
        var panel = ShopPanel.Instance ?? FindFirstObjectByType<ShopPanel>(FindObjectsInactive.Include);
        if (panel != null)
        {
            // 先打开新面板再隐藏本面板，保证 UIPause refcount 始终 >= 1，避免一帧 timeScale=1
            panel.Show(gameObject);
            gameObject.SetActive(false);
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
            panel.Show(gameObject);
            gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[StartMenu] SettingsPanel not found in scene. " +
                             "添加一个带 UIDocument 的 GameObject (Source Asset = Settings.uxml) 并挂上 SettingsPanel 脚本。");
        }
    }
}
