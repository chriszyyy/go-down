using UnityEngine;

/// <summary>
/// 摄像机跟随塔尖
/// </summary>
public class CameraFollower : MonoBehaviour
{
    [Header("跟随配置")]
    [Tooltip("目标塔构建器")]
    public TowerBuilder towerBuilder;

    [Tooltip("摄像机相对于塔尖的偏移")]
    public Vector3 offset = new Vector3(0, -3f, -10f);

    [Tooltip("跟随平滑速度")]
    public float smoothSpeed = 5f;

    [Tooltip("最小Y坐标")]
    public float minY = 0f;

    private float targetY;

    void Start()
    {
        if (towerBuilder == null)
        {
            towerBuilder = FindObjectOfType<TowerBuilder>();
        }

        if (towerBuilder != null)
        {
            UpdateTargetPosition();
        }
    }

    void LateUpdate()
    {
        if (towerBuilder == null) return;

        UpdateTargetPosition();

        // 计算塔的中心X坐标
        float towerCenterX = towerBuilder.layerWidth / 2f;

        Vector3 desiredPosition = new Vector3(
            towerCenterX + offset.x,
            Mathf.Max(targetY + offset.y, minY),
            offset.z
        );

        Vector3 smoothedPosition = Vector3.Lerp(
            transform.position,
            desiredPosition,
            smoothSpeed * Time.deltaTime
        );

        transform.position = smoothedPosition;
    }

    void UpdateTargetPosition()
    {
        targetY = towerBuilder.GetTowerTopY();
    }

    public void SetTargetY(float y)
    {
        targetY = y;
    }
}
