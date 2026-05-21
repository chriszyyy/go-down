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

    [Header("挑战性调节")]
    [Tooltip("最终重力倍率（>1 更难控制，<1 更稳）")]
    public float gravityMultiplier = 1.15f;

    [Tooltip("最终线性阻力倍率（<1 更滑，更难控）")]
    public float linearDragMultiplier = 0.65f;

    [Tooltip("最终角阻力倍率（<1 更容易旋转，更难控）")]
    public float angularDragMultiplier = 0.8f;

    [Header("边界检测")]
    [Tooltip("安全区域 X 轴范围（仅用于 Gizmos 可视化；胜负请用左右边界触发器判定）")]
    public float safeZoneX = 6f;

    [Header("视觉设置")]
    [Tooltip("六边形颜色")]
    public Color hexagonColor = new Color(1f, 0.8f, 0.2f); // 金色

    // 组件引用
    private Rigidbody2D rb;
    private PolygonCollider2D polygonCollider;
    private SpriteRenderer spriteRenderer;

    // 状态
    private bool isGameOver = false;

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
    }

    /// <summary>
    /// 配置物理参数
    /// </summary>
    void ConfigurePhysics()
    {
        rb.mass = mass;
        rb.drag = Mathf.Max(0f, linearDrag * Mathf.Max(0f, linearDragMultiplier));
        rb.angularDrag = Mathf.Max(0f, angularDrag * Mathf.Max(0f, angularDragMultiplier));
        rb.gravityScale = Mathf.Max(0f, gravityScale * Mathf.Max(0f, gravityMultiplier));
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    // 游戏结束现在由左右边界触发器负责（GameOverBoundary），球本身不再做失败判定。

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
