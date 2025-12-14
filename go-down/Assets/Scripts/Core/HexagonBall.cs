using UnityEngine;
using System;

/// <summary>
/// 六边形球脚本 - 游戏的核心物体
/// 需要保持平衡不掉落
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PolygonCollider2D))]
public class HexagonBall : MonoBehaviour
{
    [Header("物理参数")]
    [Tooltip("质量")]
    public float mass = 1f;

    [Tooltip("线性阻力")]
    public float linearDrag = 0.5f;

    [Tooltip("角阻力")]
    public float angularDrag = 2f;

    [Tooltip("重力缩放")]
    public float gravityScale = 1f;

    [Header("边界检测")]
    [Tooltip("安全区域 X 轴范围（超出此范围判定失败）")]
    public float safeZoneX = 6f;

    [Tooltip("危险倾斜角度（度数）")]
    public float dangerAngle = 45f;

    [Tooltip("危险速度阈值")]
    public float dangerVelocity = 10f;

    [Header("视觉设置")]
    [Tooltip("六边形颜色")]
    public Color hexagonColor = new Color(1f, 0.8f, 0.2f); // 金色

    // 组件引用
    private Rigidbody2D rb;
    private PolygonCollider2D polygonCollider;
    private SpriteRenderer spriteRenderer;

    // 状态
    private bool isGameOver = false;
    private float currentAngle = 0f;

    // 事件
    public static event Action OnBallFell;  // 球掉落事件
    public static event Action<float> OnBallTilted;  // 球倾斜事件（传递角度）
    public static event Action OnBallStable;  // 球稳定事件

    void Awake()
    {
        // 获取组件
        rb = GetComponent<Rigidbody2D>();
        polygonCollider = GetComponent<PolygonCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // 配置物理参数
        ConfigurePhysics();
    }

    void Start()
    {
        // 设置层级
        gameObject.layer = LayerMask.NameToLayer("HexagonBall");

        // 设置颜色
        if (spriteRenderer != null)
        {
            spriteRenderer.color = hexagonColor;
        }
    }

    void Update()
    {
        if (isGameOver) return;

        // 检测失败条件
        CheckGameOverConditions();

        // 更新倾斜角度
        UpdateTiltAngle();
    }

    /// <summary>
    /// 配置物理参数
    /// </summary>
    void ConfigurePhysics()
    {
        rb.mass = mass;
        rb.drag = linearDrag;
        rb.angularDrag = angularDrag;
        rb.gravityScale = gravityScale;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    /// <summary>
    /// 检测游戏结束条件
    /// </summary>
    void CheckGameOverConditions()
    {
        // 条件 1: 球的 X 坐标超出安全区域
        if (Mathf.Abs(transform.position.x) > safeZoneX)
        {
            TriggerGameOver("球掉出边界");
            return;
        }

        // 条件 2: 球倾斜角度过大
        if (Mathf.Abs(currentAngle) > dangerAngle)
        {
            TriggerGameOver($"球倾斜角度过大: {currentAngle:F1}°");
            return;
        }

        // 条件 3: 球速度过快且位置危险
        if (rb.velocity.magnitude > dangerVelocity && Mathf.Abs(transform.position.x) > safeZoneX * 0.7f)
        {
            TriggerGameOver($"球飞出速度过快: {rb.velocity.magnitude:F1}");
            return;
        }
    }

    /// <summary>
    /// 更新倾斜角度
    /// </summary>
    void UpdateTiltAngle()
    {
        // 获取当前旋转角度（归一化到 -180 到 180）
        currentAngle = transform.eulerAngles.z;
        if (currentAngle > 180f) currentAngle -= 360f;

        // 触发倾斜事件
        OnBallTilted?.Invoke(currentAngle);

        // 检查是否稳定
        if (IsStable())
        {
            OnBallStable?.Invoke();
        }
    }

    /// <summary>
    /// 触发游戏结束
    /// </summary>
    void TriggerGameOver(string reason)
    {
        if (isGameOver) return;

        isGameOver = true;
        Debug.Log($"游戏结束: {reason}");

        OnBallFell?.Invoke();
    }

    /// <summary>
    /// 检查球是否稳定
    /// </summary>
    public bool IsStable()
    {
        if (rb == null) return false;

        // 速度很小且角度接近水平
        bool velocityStable = rb.velocity.magnitude < 0.1f;
        bool angularStable = Mathf.Abs(rb.angularVelocity) < 1f;
        bool angleStable = Mathf.Abs(currentAngle) < 5f;

        return velocityStable && angularStable && angleStable;
    }

    /// <summary>
    /// 获取当前倾斜角度
    /// </summary>
    public float GetTiltAngle()
    {
        return currentAngle;
    }

    /// <summary>
    /// 获取当前高度（Y坐标）
    /// </summary>
    public float GetHeight()
    {
        return transform.position.y;
    }

    /// <summary>
    /// 增加稳定性（技能效果）
    /// </summary>
    public void IncreaseStability(float duration)
    {
        StartCoroutine(StabilityBoostCoroutine(duration));
    }

    /// <summary>
    /// 稳定性增强协程
    /// </summary>
    System.Collections.IEnumerator StabilityBoostCoroutine(float duration)
    {
        float originalDrag = angularDrag;
        angularDrag *= 3f; // 临时增加角阻力

        yield return new WaitForSeconds(duration);

        angularDrag = originalDrag;
    }

    /// <summary>
    /// 重置球的状态
    /// </summary>
    public void ResetBall()
    {
        isGameOver = false;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        transform.rotation = Quaternion.identity;
    }

    // void OnCollisionEnter2D(Collision2D collision)
    // {
    //     // 碰撞到方块或其他物体时的处理
    //     Debug.Log($"球碰撞到: {collision.gameObject.name}");
    // }

    void OnDrawGizmos()
    {
        // 在编辑器中绘制安全区域
        Gizmos.color = Color.green;
        Vector3 leftBound = new Vector3(-safeZoneX, transform.position.y, 0);
        Vector3 rightBound = new Vector3(safeZoneX, transform.position.y, 0);

        Gizmos.DrawLine(leftBound + Vector3.up * 10, leftBound - Vector3.up * 10);
        Gizmos.DrawLine(rightBound + Vector3.up * 10, rightBound - Vector3.up * 10);
    }
}
