using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 形状组 - 将多个方块组合成一个整体
/// </summary>
public class BlockShapeGroup : MonoBehaviour
{
    [Header("形状信息")]
    public BlockShapeType shapeType;
    public Color shapeColor;

    [Header("物理状态")]
    public bool isStatic = true;

    // 包含的所有方块
    private List<Block> blocks = new List<Block>();
    private Rigidbody2D rb;
    private CompositeCollider2D compositeCollider;

    void Awake()
    {
        // 收集所有子方块
        blocks.AddRange(GetComponentsInChildren<Block>());
        rb = GetComponent<Rigidbody2D>();
        compositeCollider = GetComponent<CompositeCollider2D>();
    }

    void Start()
    {
        // 初始设置为静态
        if (isStatic)
        {
            Freeze();
        }

        // 订阅方块消除事件
        foreach (var block in blocks)
        {
            Block.OnBlockDestroyed += HandleBlockDestroyed;
        }
    }

    void OnDestroy()
    {
        Block.OnBlockDestroyed -= HandleBlockDestroyed;
    }

    /// <summary>
    /// 处理方块被消除
    /// </summary>
    void HandleBlockDestroyed(Block destroyedBlock)
    {
        // 如果是本组的方块，消除整个形状组
        if (blocks.Contains(destroyedBlock))
        {
            DestroyShapeGroup();
        }
    }

    /// <summary>
    /// 消除整个形状组
    /// </summary>
    public void DestroyShapeGroup()
    {
        Debug.Log($"消除形状组: {shapeType}");

        // 消除所有方块
        foreach (var block in blocks)
        {
            if (block != null && block.gameObject != null)
            {
                block.DestroyBlock();
            }
        }

        // 延迟销毁父对象
        Destroy(gameObject, 0.5f);
    }

    /// <summary>
    /// 将形状组转为动态（受重力影响）
    /// </summary>
    public void MakeDynamic()
    {
        isStatic = false;

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.WakeUp();
        }

        Debug.Log($"形状组转为动态: {shapeType}");
    }

    /// <summary>
    /// 冻结形状组（不受重力影响）
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
    /// 检查形状组是否稳定
    /// </summary>
    public bool IsStable()
    {
        if (isStatic) return true;

        if (rb != null)
        {
            return rb.velocity.magnitude < 0.1f && Mathf.Abs(rb.angularVelocity) < 1f;
        }

        return true;
    }
}
