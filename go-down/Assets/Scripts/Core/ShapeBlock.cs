using UnityEngine;
using System;

/// <summary>
/// 一体化形状方块 - 管理整个俄罗斯方块形状作为单一物体
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class ShapeBlock : MonoBehaviour
{
    [Header("形状配置")]
    public TetrisShapeType shapeType;

    [Header("物理状态")]
    [Tooltip("形状是否为静态（不受重力影响）")]
    public bool isStatic = true;

    [Header("动画配置")]
    [Tooltip("消失动画持续时间")]
    public float disappearDuration = 0.3f;

    [Tooltip("消失时的缩放目标")]
    public float disappearScale = 0.1f;

    // 组件引用
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Collider2D shapeCollider;

    // 状态
    private bool isDestroying = false;
    private float destroyTimer = 0f;
    private Vector3 originalScale;
    private Color originalColor;

    // 静态事件
    public static event Action<ShapeBlock> OnShapeDestroyed;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        shapeCollider = GetComponent<Collider2D>();

        originalScale = transform.localScale;
        originalColor = spriteRenderer.color;
    }

    void Start()
    {
        // 初始设置为静态
        if (isStatic)
        {
            Freeze();
        }
    }

    void Update()
    {
        if (isDestroying)
        {
            UpdateDestroyAnimation();
        }
    }

    /// <summary>
    /// 鼠标点击检测
    /// </summary>
    void OnMouseDown()
    {
        Debug.Log($"点击检测到: {shapeType} at {transform.position}");

        if (!isDestroying)
        {
            DestroyShape();
        }
    }

    /// <summary>
    /// 鼠标进入检测（调试用）
    /// </summary>
    void OnMouseEnter()
    {
        if (!isDestroying && spriteRenderer != null)
        {
            // 鼠标悬停时高亮
            spriteRenderer.color = new Color(originalColor.r * 1.2f, originalColor.g * 1.2f, originalColor.b * 1.2f);
        }
    }

    /// <summary>
    /// 鼠标离开检测（调试用）
    /// </summary>
    void OnMouseExit()
    {
        if (!isDestroying && spriteRenderer != null)
        {
            // 恢复原色
            spriteRenderer.color = originalColor;
        }
    }

    /// <summary>
    /// 消除形状
    /// </summary>
    public void DestroyShape()
    {
        if (isDestroying) return;

        isDestroying = true;
        destroyTimer = 0f;

        // 禁用碰撞器
        if (shapeCollider != null)
        {
            shapeCollider.enabled = false;
        }

        // 触发事件
        OnShapeDestroyed?.Invoke(this);

        Debug.Log($"形状被消除: {shapeType} at {transform.position}");
    }

    /// <summary>
    /// 更新消失动画
    /// </summary>
    void UpdateDestroyAnimation()
    {
        destroyTimer += Time.deltaTime;
        float progress = destroyTimer / disappearDuration;

        if (progress >= 1f)
        {
            // 动画结束，销毁对象
            Destroy(gameObject);
            return;
        }

        // 缩小动画
        float scale = Mathf.Lerp(1f, disappearScale, progress);
        transform.localScale = originalScale * scale;

        // 淡出动画
        Color color = originalColor;
        color.a = Mathf.Lerp(1f, 0f, progress);
        spriteRenderer.color = color;
    }

    /// <summary>
    /// 将形状转为动态（受重力影响）
    /// </summary>
    public void MakeDynamic()
    {
        if (isDestroying) return;

        isStatic = false;

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.WakeUp();
        }

        Debug.Log($"形状转为动态: {shapeType} at {transform.position}");
    }

    /// <summary>
    /// 冻结形状（不受重力影响）
    /// </summary>
    public void Freeze()
    {
        isStatic = true;

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    /// <summary>
    /// 检查形状是否稳定（不再移动）
    /// </summary>
    public bool IsStable()
    {
        if (isStatic) return true;
        if (rb == null) return false;

        return rb.velocity.magnitude < 0.1f && Mathf.Abs(rb.angularVelocity) < 1f;
    }

    void OnDestroy()
    {
        // 清理事件订阅
        OnShapeDestroyed = null;
    }
}
