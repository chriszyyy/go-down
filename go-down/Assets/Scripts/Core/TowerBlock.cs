using UnityEngine;
using System;
using UnityEngine.EventSystems;

/// <summary>
/// 塔方块基类 - 所有可消除方块的基类
/// 用于 Prefab 预制体
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class TowerBlock : MonoBehaviour
{
    [Header("方块配置")]
    [Tooltip("方块类型名称")]
    public string blockTypeName = "Block";

    [Tooltip("方块得分")]
    public int scoreValue = 10;

    [Tooltip("得分倍率（例如特殊方块=10倍）。最终得分 = 计算得分 * 倍率")]
    public int scoreMultiplier = 1;

    [Header("物理状态")]
    [Tooltip("是否为静态（不受重力影响）")]
    public bool isStatic = true;

    [Tooltip("是否为可破坏的结构支撑；常规激活不会将其转为动态刚体")]
    [SerializeField] private bool isStructuralSupport;

    [Header("动画配置")]
    [Tooltip("消失动画持续时间")]
    public float disappearDuration = 0.15f;

    [Tooltip("消失时的缩放目标")]
    public float disappearScale = 0.1f;

    // 组件引用
    protected Rigidbody2D rb;
    protected SpriteRenderer spriteRenderer;
    protected Collider2D blockCollider;

    // 状态
    protected bool isDestroying = false;
    protected float destroyTimer = 0f;
    protected Vector3 originalScale;
    protected Color originalColor;

    /// <summary>是否正在消除中（已被点击/触发消除）。</summary>
    public bool IsDestroying => isDestroying;

    /// <summary>是否为保持 Kinematic、但仍可点击消除的结构支撑。</summary>
    public bool IsStructuralSupport => isStructuralSupport;

    // 静态事件
    public static event Action<TowerBlock> OnBlockDestroyed;
    public static event Action<TowerBlock, int> OnBlockScored; // 方块，得分

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        blockCollider = GetComponent<Collider2D>();

        // 默认设置为Kinematic（静态，不受重力影响）
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
        }
        isStatic = true;

        originalScale = transform.localScale;
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    public void OverrideOriginalColor(Color color)
    {
        originalColor = color;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }
    }

    protected virtual void Start()
    {
    }

    protected virtual void Update()
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
        if (isDestroying) return;

        // If a UI panel (e.g., GameOver modal) is on top, don't let physics clicks through.
        if (IsPointerOverUI()) return;

        // During GameOver we pause timeScale; block clicking so score won't keep increasing.
        if (Time.timeScale == 0f) return;

        DestroyBlock();
    }

    private static bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;

        // Touch
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            return EventSystem.current.IsPointerOverGameObject(t.fingerId);
        }

        // Mouse
        return EventSystem.current.IsPointerOverGameObject();
    }

    /// <summary>
    /// 消除方块
    /// </summary>
    public virtual void DestroyBlock()
    {
        if (isDestroying) return;

        isDestroying = true;
        destroyTimer = 0f;

        // 禁用碰撞器
        if (blockCollider != null)
        {
            blockCollider.enabled = false;
        }

        // Immediate visual feedback: get out of the way instantly so next target is easier to click.
        transform.localScale = originalScale * 0.82f;
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = Mathf.Min(c.a, 0.55f);
            spriteRenderer.color = c;
        }

        // 触发得分事件（倍率在 ScoreManager 中统一处理，避免破坏 scoreValue<=0 的占格兜底逻辑）
        OnBlockScored?.Invoke(this, scoreValue);

        // 触发消除事件
        OnBlockDestroyed?.Invoke(this);

        // Debug.Log($"方块被消除: {blockTypeName} at {transform.position}, 得分: {scoreValue}");
    }

    /// <summary>
    /// 更新消失动画
    /// </summary>
    protected virtual void UpdateDestroyAnimation()
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
        if (spriteRenderer != null)
        {
            Color color = originalColor;
            color.a = Mathf.Lerp(1f, 0f, progress);
            spriteRenderer.color = color;
        }
    }

    /// <summary>
    /// 标记为可破坏的固定结构支撑。
    /// </summary>
    public void ConfigureStructuralSupport()
    {
        isStructuralSupport = true;
        Freeze();
    }

    /// <summary>
    /// 将普通方块转为动态（受重力影响）。结构支撑保持 Kinematic。
    /// </summary>
    public virtual void MakeDynamic()
    {
        if (isDestroying || isStructuralSupport) return;

        isStatic = false;

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.WakeUp();
        }
    }

    /// <summary>
    /// 冻结方块（不受重力影响）
    /// </summary>
    public virtual void Freeze()
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
    /// 检查方块是否稳定（不再移动）
    /// </summary>
    public virtual bool IsStable()
    {
        if (isStatic) return true;
        if (rb == null) return false;

        return rb.velocity.magnitude < 0.1f && Mathf.Abs(rb.angularVelocity) < 1f;
    }

    /// <summary>
    /// 获取方块实际占用的格子（相对坐标）
    /// 返回格子坐标列表，例如L3方块: [(0,0), (1,0), (0,1), (0,2)]
    /// </summary>
    public virtual System.Collections.Generic.List<(int x, int y)> GetOccupiedCells(float rotationAngle)
    {
        var cells = new System.Collections.Generic.List<(int x, int y)>
        {
            // 根据方块类型和旋转角度返回实际占用格子
            // 基类默认：单格方块
            (0, 0)
        };

        return cells;
    }

    public virtual Vector2Int GetBottomLeftCorner(float rotationAngle)
    {
        float normalizedAngle = rotationAngle % 360f;
        if (normalizedAngle < 0) normalizedAngle += 360f;

        if (Mathf.Approximately(normalizedAngle, 0f))
        {
            return new Vector2Int(0, 0);
        }
        else if (Mathf.Approximately(normalizedAngle, 90f))
        {
            return new Vector2Int(-1, 0);
        }
        else if (Mathf.Approximately(normalizedAngle, 180f))
        {
            return new Vector2Int(-1, -1);
        }
        else if (Mathf.Approximately(normalizedAngle, 270f))
        {
            return new Vector2Int(0, -1);
        }

        return Vector2Int.zero;
    }

    protected virtual void OnDestroy()
    {
        // 清理时解除事件订阅
    }
}
