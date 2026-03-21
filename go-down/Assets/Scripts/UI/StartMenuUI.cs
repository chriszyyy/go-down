using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Start menu panel controller for single-scene flow.
/// Includes start button + audio/vibration toggles + share button.
/// </summary>
public class StartMenuUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Menu root object. If null, uses this game object.")]
    public GameObject panelRoot;

    public Button startButton;
    public Toggle audioToggle;
    public Toggle vibrationToggle;
    public Button shareButton;

    [Header("Behavior")]
    [Tooltip("Show start panel when game launches.")]
    public bool showOnStart = true;

    [Tooltip("Show start panel again when game is reset.")]
    public bool showOnGameReset = false;

    [Tooltip("Pause gameplay while panel is visible.")]
    public bool pauseTimeWhileVisible = true;

    [Tooltip("While menu is open, continuously keep it on top and keep combo progress hidden.")]
    public bool enforceMenuTopWhileVisible = true;

    private bool started;
    private bool menuVisible;
    private BlockClearProgressUI cachedProgress;
    private float nextProgressLookupTime;

    private void Awake()
    {
        if (panelRoot == null) panelRoot = gameObject;

        if (startButton == null) startButton = GetComponentInChildren<Button>(includeInactive: true);
    }

    private void OnEnable()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(OnStartClicked);
            startButton.onClick.AddListener(OnStartClicked);
        }

        if (audioToggle != null)
        {
            audioToggle.onValueChanged.RemoveListener(OnAudioToggleChanged);
            audioToggle.onValueChanged.AddListener(OnAudioToggleChanged);
        }

        if (vibrationToggle != null)
        {
            vibrationToggle.onValueChanged.RemoveListener(OnVibrationToggleChanged);
            vibrationToggle.onValueChanged.AddListener(OnVibrationToggleChanged);
        }

        if (shareButton != null)
        {
            shareButton.onClick.RemoveListener(OnShareClicked);
            shareButton.onClick.AddListener(OnShareClicked);
        }

        GameStateManager.OnGameReset += HandleGameReset;
    }

    private void Start()
    {
        started = true;
        SyncTogglesFromSettings();

        if (showOnStart)
        {
            ShowMenu();
        }
        else
        {
            HideMenu();
        }
    }

    private void OnDisable()
    {
        if (startButton != null) startButton.onClick.RemoveListener(OnStartClicked);
        if (audioToggle != null) audioToggle.onValueChanged.RemoveListener(OnAudioToggleChanged);
        if (vibrationToggle != null) vibrationToggle.onValueChanged.RemoveListener(OnVibrationToggleChanged);
        if (shareButton != null) shareButton.onClick.RemoveListener(OnShareClicked);

        GameStateManager.OnGameReset -= HandleGameReset;
    }

    private void HandleGameReset()
    {
        if (!showOnGameReset) return;
        SyncTogglesFromSettings();
        ShowMenu();
    }

    private void LateUpdate()
    {
        if (!menuVisible || !enforceMenuTopWhileVisible) return;

        if (panelRoot != null)
        {
            panelRoot.transform.SetAsLastSibling();
        }

        EnsureProgressVisible(false);
    }

    private void OnStartClicked()
    {
        HideMenu();
    }

    private void ShowMenu()
    {
        if (panelRoot != null) panelRoot.SetActive(true);
        menuVisible = true;

        if (panelRoot != null)
        {
            panelRoot.transform.SetAsLastSibling();
        }

        if (pauseTimeWhileVisible) Time.timeScale = 0f;

        // Keep HUD clean when menu is open.
        EnsureProgressVisible(false);
    }

    private void HideMenu()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        menuVisible = false;

        if (pauseTimeWhileVisible)
        {
            float resume = 1f;
            if (GameStateManager.Instance != null)
            {
                resume = GameStateManager.Instance.resumeTimeScale;
            }
            Time.timeScale = resume;
        }

        EnsureProgressVisible(true);
    }

    private void EnsureProgressVisible(bool visible)
    {
        if (cachedProgress == null)
        {
            if (Time.unscaledTime < nextProgressLookupTime) return;
            nextProgressLookupTime = Time.unscaledTime + 0.25f;
            cachedProgress = FindObjectOfType<BlockClearProgressUI>(includeInactive: true);
        }

        if (cachedProgress != null)
        {
            cachedProgress.SetVisible(visible);
        }
    }

    private void SyncTogglesFromSettings()
    {
        if (audioToggle != null)
            audioToggle.SetIsOnWithoutNotify(GameUserSettings.AudioEnabled);

        if (vibrationToggle != null)
            vibrationToggle.SetIsOnWithoutNotify(GameUserSettings.VibrationEnabled);
    }

    private void OnAudioToggleChanged(bool value)
    {
        if (!started) return;
        GameUserSettings.AudioEnabled = value;
    }

    private void OnVibrationToggleChanged(bool value)
    {
        if (!started) return;
        GameUserSettings.VibrationEnabled = value;
    }

    private void OnShareClicked()
    {
        // TODO: Hook platform share SDK here.
        // Intentionally left blank for now.
    }
}
