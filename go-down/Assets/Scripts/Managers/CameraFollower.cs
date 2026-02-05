using UnityEngine;

/// <summary>
/// 摄像机跟随六边形球（只跟随Y，X保持在塔中心）
/// 具备“速度自适应”的平滑：移动不大时慢慢跟，目标移动很快时跟得更快。
/// </summary>
public class CameraFollower : MonoBehaviour
{
    [Header("跟随配置")]
    [Tooltip("目标塔构建器")]
    public TowerBuilder towerBuilder;

    [Tooltip("跟随目标（默认自动找名为 HexagonBall 的对象）")]
    public Transform target;

    [Tooltip("摄像机相对于目标Y的偏移")]
    public Vector3 offset = new Vector3(0, 0f, -10f);

    [Header("平滑与速度")]
    [Tooltip("慢速跟随的时间常数（秒）。越大越慢")]
    public float slowSmoothTime = 0.35f;

    [Tooltip("目标移动很快时的时间常数（秒）。越小越快")]
    public float fastSmoothTime = 0.08f;

    [Tooltip("目标速度达到该值（单位/秒）时进入快速跟随")]
    public float fastSpeedThreshold = 6f;

    [Tooltip("为了避免微小抖动，目标变化小于该值时不更新")]
    public float deadZone = 0.05f;

    [Tooltip("相机最大跟随速度（单位/秒），防止瞬移")]
    public float maxCameraSpeed = 50f;

    private float targetY;
    private float lastTargetY;
    private float currentYVelocity;

    private Vector3 initialCameraPosition;
    private bool initialCameraPositionCaptured;

    private void OnEnable()
    {
        GameStateManager.OnGameReset += HandleGameReset;
    }

    private void OnDisable()
    {
        GameStateManager.OnGameReset -= HandleGameReset;
    }

    void Start()
    {
        if (towerBuilder == null)
        {
            towerBuilder = FindObjectOfType<TowerBuilder>();
        }

        if (target == null)
        {
            GameObject ball = GameObject.Find("HexagonBall");
            if (ball != null) target = ball.transform;
        }

        UpdateTargetPosition();
        lastTargetY = targetY;

        // Capture the camera's "default" position in the scene as the reset anchor.
        // On restart, we teleport here first so subsequent smoothing only travels a short distance.
        if (!initialCameraPositionCaptured)
        {
            initialCameraPosition = transform.position;
            initialCameraPositionCaptured = true;
        }
    }

    private void HandleGameReset()
    {
        ResetToInitialPosition();
    }

    void LateUpdate()
    {
        if (towerBuilder == null) return;

        // 处理重生/重开导致的 target 失效
        if (target == null)
        {
            GameObject ball = GameObject.Find("HexagonBall");
            if (ball != null) target = ball.transform;
        }

        UpdateTargetPosition();

        // 计算塔的中心X坐标
        float towerCenterX = towerBuilder.layerWidth / 2f;

        Vector3 desiredPosition = new Vector3(
            towerCenterX + offset.x,
            targetY + offset.y,
            offset.z
        );

        float deltaTarget = targetY - lastTargetY;
        float targetSpeed = Mathf.Abs(deltaTarget) / Mathf.Max(0.0001f, Time.deltaTime);
        lastTargetY = targetY;

        float distanceToTarget = Mathf.Abs(desiredPosition.y - transform.position.y);
        if (distanceToTarget < deadZone)
        {
            // still: only lock X/Z
            transform.position = new Vector3(desiredPosition.x, transform.position.y, desiredPosition.z);
            return;
        }

        float t = Mathf.InverseLerp(0f, fastSpeedThreshold, targetSpeed);
        float smoothTime = Mathf.Lerp(slowSmoothTime, fastSmoothTime, t);
        smoothTime = Mathf.Max(0.0001f, smoothTime);

        float newY = Mathf.SmoothDamp(
            transform.position.y,
            desiredPosition.y,
            ref currentYVelocity,
            smoothTime,
            maxCameraSpeed,
            Time.deltaTime
        );

        transform.position = new Vector3(desiredPosition.x, newY, desiredPosition.z);
    }

    void UpdateTargetPosition()
    {
        if (target != null)
        {
            targetY = target.position.y;
            return;
        }

        // 兜底：如果球没找到，保持原逻辑（避免相机卡死）。
        targetY = towerBuilder.GetTowerTopY();
    }

    public void ResetToInitialPosition()
    {
        if (!initialCameraPositionCaptured)
        {
            initialCameraPosition = transform.position;
            initialCameraPositionCaptured = true;
        }

        transform.position = initialCameraPosition;

        // Clear smoothing state and align internal target tracking to current camera position
        // so the next LateUpdate won't think the target "teleported" a huge distance.
        currentYVelocity = 0f;
        targetY = transform.position.y - offset.y;
        lastTargetY = targetY;
    }

    public void SetTargetY(float y)
    {
        targetY = y;
    }
}
