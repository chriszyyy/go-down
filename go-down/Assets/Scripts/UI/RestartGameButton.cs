using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// UI 按钮：重新开始游戏。
/// - 清空并重建塔
/// - 复位相机到初始位置/塔尖跟随位置
/// </summary>
public class RestartGameButton : MonoBehaviour
{
    [Header("References")]
    public TowerBuilder towerBuilder;
    public CameraFollower cameraFollower;
    public Button button;

    [Header("Camera Reset")]
    [Tooltip("复位相机使用的 Z 值（2D 通常为 -10）。为 0 则使用当前相机 Z")]
    public float cameraZ = -10f;

    void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (button != null)
        {
            button.onClick.AddListener(RestartGame);
        }
    }

    void Start()
    {
        if (towerBuilder == null)
        {
            towerBuilder = FindObjectOfType<TowerBuilder>();
        }

        if (cameraFollower == null)
        {
            cameraFollower = FindObjectOfType<CameraFollower>();
        }
    }

    void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(RestartGame);
        }
    }

    public void RestartGame()
    {
        // This button panel may be deactivated during UI transitions.
        // Always run the coroutine on the persistent GameStateManager.
        if (GameStateManager.Instance == null || !GameStateManager.Instance.isActiveAndEnabled)
        {
            Debug.LogWarning("RestartGameButton: GameStateManager.Instance 不存在或未启用，无法启动重开协程。");
            return;
        }

        GameStateManager.Instance.StartCoroutine(RestartRoutine());
    }

    private IEnumerator RestartRoutine()
    {
        if (towerBuilder == null)
        {
            Debug.LogWarning("RestartGameButton: towerBuilder 未绑定。");
            yield break;
        }

        if (cameraFollower == null)
        {
            cameraFollower = FindObjectOfType<CameraFollower>();
        }

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.ResetGameState();
        }

        // Rebuild the tower. ResetTower will keep activation disabled if configured.
        towerBuilder.ResetTower();

        // Reset camera immediately, then wait until it reaches the new desired follow position.
        if (cameraFollower != null)
        {
            cameraFollower.towerBuilder = towerBuilder;
            cameraFollower.ResetToInitialPosition();

            float start = Time.unscaledTime;
            while (!cameraFollower.IsNearDesiredPosition(0.2f) && Time.unscaledTime - start < 2.0f)
            {
                yield return null;
            }
        }
        else
        {
            // Fallback: directly place camera close to the new tower top.
            Camera cam = Camera.main;
            if (cam != null)
            {
                float centerX = towerBuilder.layerWidth / 2f;
                float topY = towerBuilder.GetTowerTopY();
                float z = cameraZ != 0f ? cameraZ : cam.transform.position.z;
                cam.transform.position = new Vector3(centerX, topY - 3f, z);
            }
        }

        // Only auto-start activation if this tower isn't configured for manual activation.
        if (!towerBuilder.requireManualStartActivation)
        {
            towerBuilder.StartActivation();
        }
    }
}
