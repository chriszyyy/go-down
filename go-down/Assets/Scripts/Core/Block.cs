using UnityEngine;
using System;

/// <summary>
/// 方块脚本 - 可点击消除的方块
/// 附加到每个方块 GameObject 上
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
public class Block : MonoBehaviour
{
    [Header("方块设置")]
    [Tooltip("方块类型/颜色")]
    public BlockType blockType = BlockType.Normal;

    [Tooltip("方块是否可以被点击")]
    public bool isClickable = true;

    [Header("消除动画设置")]
    [Tooltip("消除动画持续时间")]
    public float destroyDuration = 0.3f;

    [Tooltip("消除时的缩放效果")]
    public bool useScaleAnimation = true;

    [Tooltip("消除时的淡出效果")]
    public bool useFadeAnimation = true;

    // 组件引用
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;
    private Rigidbody2D rb;

    // 状态
    private bool isDestroying = false;
    private Vector3 originalScale;
    private Color originalColor;

    // 事件 - 当方块被消除时触发
    public static event Action<Block> OnBlockDestroyed;

    void Awake()
    {
        // 获取组件引用
        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();
        rb = GetComponent<Rigidbody2D>();

        // 保存原始值
        originalScale = transform.localScale;
        originalColor = spriteRenderer.color;

        // 初始化 Rigidbody2D 为静态（不受重力影响）
        rb.bodyType = RigidbodyType2D.Static;
    }

    void Start()
    {
        // 确保方块在正确的层级
        if (gameObject.layer != LayerMask.NameToLayer("Block"))
        {
            gameObject.layer = LayerMask.NameToLayer("Block");
        }
    }

    /// <summary>
    /// 鼠标点击检测
    /// </summary>
    void OnMouseDown()
    {
        if (isClickable && !isDestroying)
        {
            DestroyBlock();
        }
    }

    /// <summary>
    /// 消除方块
    /// </summary>
    public void DestroyBlock()
    {
        if (isDestroying) return;

        isDestroying = true;
        isClickable = false;

        // 触发事件通知其他系统
        OnBlockDestroyed?.Invoke(this);

        // 禁用碰撞体，让上层物体可以穿过
        boxCollider.enabled = false;

        // 播放消除动画
        StartCoroutine(DestroyAnimation());
    }

    /// <summary>
    /// 消除动画协程
    /// </summary>
    private System.Collections.IEnumerator DestroyAnimation()
    {
        float elapsed = 0f;

        while (elapsed < destroyDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / destroyDuration;

            // 缩放动画
            if (useScaleAnimation)
            {
                transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, progress);
            }

            // 淡出动画
            if (useFadeAnimation)
            {
                Color color = originalColor;
                color.a = Mathf.Lerp(1f, 0f, progress);
                spriteRenderer.color = color;
            }

            yield return null;
        }

        // 动画完成后销毁对象
        Destroy(gameObject);
    }

    /// <summary>
    /// 将方块从静态变为动态（受重力影响）
    /// 用于当下方方块消除后，让上层方块下落
    /// </summary>
    public void MakeDynamic()
    {
        if (rb != null && rb.bodyType == RigidbodyType2D.Static)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
        }
    }

    /// <summary>
    /// 将方块冻结（停止物理模拟）
    /// 用于方块落地后稳定
    /// </summary>
    public void Freeze()
    {
        if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
        {
            rb.bodyType = RigidbodyType2D.Static;
        }
    }

    /// <summary>
    /// 检查方块是否已经稳定（静止）
    /// </summary>
    public bool IsStable()
    {
        if (rb == null) return true;

        // 如果是静态物体，认为是稳定的
        if (rb.bodyType == RigidbodyType2D.Static) return true;

        // 检查速度是否足够小
        return rb.velocity.magnitude < 0.01f && Mathf.Abs(rb.angularVelocity) < 0.1f;
    }

    /// <summary>
    /// 设置方块颜色
    /// </summary>
    public void SetColor(Color color)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
            originalColor = color;
        }
    }

    /// <summary>
    /// 获取方块的层级（Y坐标）
    /// </summary>
    public int GetLayer()
    {
        return Mathf.RoundToInt(transform.position.y);
    }
}

/// <summary>
/// 方块类型枚举
/// </summary>
public enum BlockType
{
    Normal,      // 普通方块
    Strong,      // 坚固方块（需要多次点击）
    Fragile,     // 易碎方块（自动消除）
    Special      // 特殊方块（带特效）
}
