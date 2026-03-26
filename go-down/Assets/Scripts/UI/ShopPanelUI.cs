using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shop panel for buying tool uses with coins.
/// Reset use: 100 coins each.
/// Rainbow use: 50 coins each.
/// </summary>
public class ShopPanelUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Shop panel root. If null, uses this game object.")]
    public GameObject panelRoot;

    [Tooltip("Optional content root whose children should be forced active when opening shop. If empty, uses panelRoot.")]
    public Transform contentRootToActivate;

    public Button buyResetButton;
    public Button buyRainbowButton;
    public Button backButton;

    [Header("Prices")]
    public int resetUsePrice = 100;
    public int rainbowUsePrice = 50;

    [Header("Labels (optional)")]
    public Text coinsText;
    public Text resetUsesText;
    public Text rainbowUsesText;
    public Text resetPriceText;
    public Text rainbowPriceText;

    public string coinsFormat = "金币: {0}";
    public string resetUsesFormat = "复位次数: {0}";
    public string rainbowUsesFormat = "彩块次数: {0}";
    public string resetPriceFormat = "价格: {0}";
    public string rainbowPriceFormat = "价格: {0}";

    [Header("Behaviour")]
    public bool hideOnStart = true;

    [Tooltip("Force all shop child objects active when opening (useful when authoring starts inactive).")]
    public bool forceActivateChildrenOnOpen = true;

    private StartMenuUI ownerMenu;

    private void Awake()
    {
        if (panelRoot == null) panelRoot = gameObject;

        if (contentRootToActivate == null && panelRoot != null)
        {
            contentRootToActivate = panelRoot.transform;
        }

        WireButtons();
    }

    private void Start()
    {
        if (hideOnStart)
        {
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        RefreshUI();
    }

    private void OnEnable()
    {
        ToolUsageInventory.OnUsesChanged += HandleUsesChanged;
        RefreshUI();
    }

    private void OnDisable()
    {
        ToolUsageInventory.OnUsesChanged -= HandleUsesChanged;
    }

    public void Open(StartMenuUI owner)
    {
        ownerMenu = owner;

        if (panelRoot != null) panelRoot.SetActive(true);
        if (panelRoot != null) panelRoot.transform.SetAsLastSibling();

        if (forceActivateChildrenOnOpen)
        {
            Transform root = contentRootToActivate != null
                ? contentRootToActivate
                : (panelRoot != null ? panelRoot.transform : null);

            if (root != null)
            {
                SetDescendantsActive(root, true);
            }
        }

        RefreshUI();
    }

    public void Close()
    {
        if (panelRoot != null) panelRoot.SetActive(false);

        if (ownerMenu != null)
        {
            ownerMenu.OpenMenuFromExternal();
        }
    }

    private void WireButtons()
    {
        if (buyResetButton != null)
        {
            buyResetButton.onClick.RemoveListener(BuyResetUse);
            buyResetButton.onClick.AddListener(BuyResetUse);
        }

        if (buyRainbowButton != null)
        {
            buyRainbowButton.onClick.RemoveListener(BuyRainbowUse);
            buyRainbowButton.onClick.AddListener(BuyRainbowUse);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(Close);
            backButton.onClick.AddListener(Close);
        }
    }

    private void BuyResetUse()
    {
        int price = Mathf.Max(0, resetUsePrice);

        if (CoinManager.Instance == null) return;
        if (!CoinManager.Instance.TrySpendCoins(price)) return;

        if (ToolUsageInventory.Instance != null)
        {
            ToolUsageInventory.Instance.AddResetUses(1);
        }

        RefreshUI();
    }

    private void BuyRainbowUse()
    {
        int price = Mathf.Max(0, rainbowUsePrice);

        if (CoinManager.Instance == null) return;
        if (!CoinManager.Instance.TrySpendCoins(price)) return;

        if (ToolUsageInventory.Instance != null)
        {
            ToolUsageInventory.Instance.AddRainbowUses(1);
        }

        RefreshUI();
    }

    private void HandleUsesChanged()
    {
        RefreshUI();
    }

    private void RefreshUI()
    {
        int coins = CoinManager.Instance != null ? CoinManager.Instance.CurrentCoins : 0;
        int resetUses = ToolUsageInventory.Instance != null ? ToolUsageInventory.Instance.ResetUses : 0;
        int rainbowUses = ToolUsageInventory.Instance != null ? ToolUsageInventory.Instance.RainbowUses : 0;

        if (coinsText != null) coinsText.text = string.Format(coinsFormat, coins);
        if (resetUsesText != null) resetUsesText.text = string.Format(resetUsesFormat, resetUses);
        if (rainbowUsesText != null) rainbowUsesText.text = string.Format(rainbowUsesFormat, rainbowUses);
        if (resetPriceText != null) resetPriceText.text = string.Format(resetPriceFormat, Mathf.Max(0, resetUsePrice));
        if (rainbowPriceText != null) rainbowPriceText.text = string.Format(rainbowPriceFormat, Mathf.Max(0, rainbowUsePrice));

        if (buyResetButton != null) buyResetButton.interactable = coins >= Mathf.Max(0, resetUsePrice);
        if (buyRainbowButton != null) buyRainbowButton.interactable = coins >= Mathf.Max(0, rainbowUsePrice);
    }

    private static void SetDescendantsActive(Transform root, bool active)
    {
        if (root == null) return;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child == null) continue;

            child.gameObject.SetActive(active);
            SetDescendantsActive(child, active);
        }
    }
}
