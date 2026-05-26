using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Android 返回键 / 手势退出确认。
/// 使用运行时创建的 uGUI 覆盖层，避免依赖当前哪个 UI Toolkit panel 处于激活状态。
/// </summary>
public class ExitConfirmOverlay : MonoBehaviour
{
    private static ExitConfirmOverlay instance;

    private Canvas canvas;
    private GameObject panelRoot;
    private bool visible;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void BootstrapBeforeScene()
    {
        EnsureInstance();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapAfterScene()
    {
        EnsureInstance();
    }

    private static void EnsureInstance()
    {
        var existing = FindFirstObjectByType<ExitConfirmOverlay>(FindObjectsInactive.Include);
        if (existing != null)
        {
            instance = existing;
            existing.enabled = true;
            existing.EnsureHiddenOnBoot();
            return;
        }

        var go = new GameObject("ExitConfirmOverlay");
        instance = go.AddComponent<ExitConfirmOverlay>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        if (transform.parent != null)
        {
            transform.SetParent(null);
        }
        DontDestroyOnLoad(gameObject);
        BuildUi();
        Hide();
    }

    private void OnEnable()
    {
        if (!visible && panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    private void Start()
    {
        // Unity Simulator / domain reload can leave runtime-created UI active for one frame.
        // Start 再关一次，确保游戏首帧不会误显示退出确认。
        Hide();
    }

    private void Update()
    {
        if (!visible && panelRoot != null && panelRoot.activeSelf)
        {
            panelRoot.SetActive(false);
        }

        // Android back button / gesture maps to Escape in Unity.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (visible) Hide();
            else Show();
        }
    }

    private void BuildUi()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        panelRoot = new GameObject("ExitConfirmPanel");
        panelRoot.transform.SetParent(transform, false);

        var blocker = panelRoot.AddComponent<Image>();
        blocker.color = new Color(0f, 0f, 0f, 0.62f);
        var blockerRect = blocker.rectTransform;
        blockerRect.anchorMin = Vector2.zero;
        blockerRect.anchorMax = Vector2.one;
        blockerRect.offsetMin = Vector2.zero;
        blockerRect.offsetMax = Vector2.zero;

        var card = CreateImage("Card", panelRoot.transform, new Color(0.04f, 0.08f, 0.22f, 0.96f));
        var cardRect = card.rectTransform;
        cardRect.anchorMin = new Vector2(0.08f, 0.39f);
        cardRect.anchorMax = new Vector2(0.92f, 0.61f);
        cardRect.offsetMin = Vector2.zero;
        cardRect.offsetMax = Vector2.zero;

        var title = CreateText("Title", card.transform, "Exit Game?", 54, FontStyle.Bold);
        var titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0.08f, 0.64f);
        titleRect.anchorMax = new Vector2(0.92f, 0.92f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        var message = CreateText("Message", card.transform, "Are you sure you want to exit?", 30, FontStyle.Normal);
        var messageRect = message.rectTransform;
        messageRect.anchorMin = new Vector2(0.08f, 0.43f);
        messageRect.anchorMax = new Vector2(0.92f, 0.62f);
        messageRect.offsetMin = Vector2.zero;
        messageRect.offsetMax = Vector2.zero;

        var cancel = CreateButton("Cancel", card.transform, "CANCEL", new Color(0.15f, 0.34f, 0.84f, 1f));
        var cancelRect = cancel.GetComponent<RectTransform>();
        cancelRect.anchorMin = new Vector2(0.08f, 0.12f);
        cancelRect.anchorMax = new Vector2(0.46f, 0.34f);
        cancelRect.offsetMin = Vector2.zero;
        cancelRect.offsetMax = Vector2.zero;
        cancel.onClick.AddListener(Hide);

        var confirm = CreateButton("Confirm", card.transform, "CONFIRM", new Color(0.95f, 0.27f, 0.27f, 1f));
        var confirmRect = confirm.GetComponent<RectTransform>();
        confirmRect.anchorMin = new Vector2(0.54f, 0.12f);
        confirmRect.anchorMax = new Vector2(0.92f, 0.34f);
        confirmRect.offsetMin = Vector2.zero;
        confirmRect.offsetMax = Vector2.zero;
        confirm.onClick.AddListener(Application.Quit);
    }

    private Image CreateImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private Text CreateText(string name, Transform parent, string text, int fontSize, FontStyle fontStyle)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var label = go.AddComponent<Text>();
        label.text = text;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = fontSize;
        label.fontStyle = fontStyle;
        label.color = Color.white;
        label.alignment = TextAnchor.MiddleCenter;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        return label;
    }

    private Button CreateButton(string name, Transform parent, string text, Color color)
    {
        var image = CreateImage(name, parent, color);
        var button = image.gameObject.AddComponent<Button>();

        var label = CreateText("Label", image.transform, text, 32, FontStyle.Bold);
        var labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        return button;
    }

    private void Show()
    {
        visible = true;
        panelRoot.SetActive(true);
        UIPause.Acquire();
    }

    private void Hide()
    {
        if (visible) UIPause.Release();
        visible = false;
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void EnsureHiddenOnBoot()
    {
        visible = false;
        if (panelRoot == null)
        {
            var child = transform.Find("ExitConfirmPanel");
            if (child != null) panelRoot = child.gameObject;
        }
        if (panelRoot != null) panelRoot.SetActive(false);
    }
}
