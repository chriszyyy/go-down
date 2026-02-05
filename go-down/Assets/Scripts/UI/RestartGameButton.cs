using UnityEngine;
using UnityEngine.UI;

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
        if (towerBuilder == null)
        {
            Debug.LogWarning("RestartGameButton: towerBuilder 未绑定。");
            return;
        }

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.ResetGameState();
        }

        // 重建塔（内部会清空旧方块、重建、并按配置重生球）
        towerBuilder.ResetTower();

        // 复位相机：优先让 CameraFollower 立即更新到新塔尖
        if (cameraFollower != null)
        {
            cameraFollower.towerBuilder = towerBuilder;

            // 简化：重开时先把相机放回“开局默认位置”附近，
            // 后续由 CameraFollower 的 SmoothDamp 从较近距离跟随到新目标。
            cameraFollower.ResetToInitialPosition();
        }
        else
        {
            // 兜底：直接找主相机移动到塔尖附近
            Camera cam = Camera.main;
            if (cam != null)
            {
                float centerX = towerBuilder.layerWidth / 2f;
                float topY = towerBuilder.GetTowerTopY();
                float z = cameraZ != 0f ? cameraZ : cam.transform.position.z;
                cam.transform.position = new Vector3(centerX, topY - 3f, z);
            }
        }
    }
}
