using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 俄罗斯方块形状定义
/// </summary>
public enum TetrisShapeType
{
    I,      // 一字形 ████
    O,      // 正方形 ██
    T,      // T字形  ▀█▀
    L,      // L字形  █▀▀
    J,      // J字形  ▀▀█
    S,      // S字形  ▀██
    Z,      // Z字形  ██▀
    Single  // 单个方块 █
}

/// <summary>
/// 俄罗斯方块形状数据
/// </summary>
[System.Serializable]
public class TetrisShape
{
    public TetrisShapeType shapeType;
    public Vector2Int[] blockPositions;  // 相对于形状中心的方块位置
    public Color shapeColor;

    public TetrisShape(TetrisShapeType type, Vector2Int[] positions, Color color)
    {
        shapeType = type;
        blockPositions = positions;
        shapeColor = color;
    }
}

/// <summary>
/// 形状组 - 由多个方块组成的一个整体
/// </summary>
public class ShapeGroup : MonoBehaviour
{
    public TetrisShapeType shapeType;
    public List<Block> blocks = new List<Block>();

    private bool isDestroyed = false;

    /// <summary>
    /// 消除整个形状组
    /// </summary>
    public void DestroyShape()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        // 消除所有方块
        foreach (Block block in blocks)
        {
            if (block != null)
            {
                block.DestroyBlock();
            }
        }
    }

    /// <summary>
    /// 将形状组的所有方块设为动态
    /// </summary>
    public void MakeDynamic()
    {
        foreach (Block block in blocks)
        {
            if (block != null)
            {
                block.MakeDynamic();
            }
        }
    }

    /// <summary>
    /// 将形状组的所有方块冻结
    /// </summary>
    public void Freeze()
    {
        foreach (Block block in blocks)
        {
            if (block != null)
            {
                block.Freeze();
            }
        }
    }

    /// <summary>
    /// 检查形状组是否稳定
    /// </summary>
    public bool IsStable()
    {
        foreach (Block block in blocks)
        {
            if (block != null && !block.IsStable())
            {
                return false;
            }
        }
        return true;
    }
}

/// <summary>
/// 俄罗斯方块形状工厂
/// </summary>
public static class TetrisShapeFactory
{
    // 定义所有形状
    private static readonly Dictionary<TetrisShapeType, TetrisShape> shapeDefinitions = new Dictionary<TetrisShapeType, TetrisShape>()
    {
        // I 形状 (一字形)
        {
            TetrisShapeType.I,
            new TetrisShape(TetrisShapeType.I, new Vector2Int[] {
                new Vector2Int(0, 0),
                new Vector2Int(1, 0),
                new Vector2Int(2, 0),
                new Vector2Int(3, 0)
            }, new Color(0.0f, 0.9f, 0.9f)) // 青色
        },
        
        // O 形状 (正方形)
        {
            TetrisShapeType.O,
            new TetrisShape(TetrisShapeType.O, new Vector2Int[] {
                new Vector2Int(0, 0),
                new Vector2Int(1, 0),
                new Vector2Int(0, 1),
                new Vector2Int(1, 1)
            }, new Color(0.9f, 0.9f, 0.0f)) // 黄色
        },
        
        // T 形状
        {
            TetrisShapeType.T,
            new TetrisShape(TetrisShapeType.T, new Vector2Int[] {
                new Vector2Int(0, 0),
                new Vector2Int(1, 0),
                new Vector2Int(2, 0),
                new Vector2Int(1, 1)
            }, new Color(0.6f, 0.0f, 0.9f)) // 紫色
        },
        
        // L 形状
        {
            TetrisShapeType.L,
            new TetrisShape(TetrisShapeType.L, new Vector2Int[] {
                new Vector2Int(0, 0),
                new Vector2Int(0, 1),
                new Vector2Int(0, 2),
                new Vector2Int(1, 0)
            }, new Color(0.9f, 0.6f, 0.0f)) // 橙色
        },
        
        // J 形状
        {
            TetrisShapeType.J,
            new TetrisShape(TetrisShapeType.J, new Vector2Int[] {
                new Vector2Int(1, 0),
                new Vector2Int(1, 1),
                new Vector2Int(1, 2),
                new Vector2Int(0, 0)
            }, new Color(0.0f, 0.0f, 0.9f)) // 蓝色
        },
        
        // S 形状
        {
            TetrisShapeType.S,
            new TetrisShape(TetrisShapeType.S, new Vector2Int[] {
                new Vector2Int(1, 0),
                new Vector2Int(2, 0),
                new Vector2Int(0, 1),
                new Vector2Int(1, 1)
            }, new Color(0.0f, 0.9f, 0.0f)) // 绿色
        },
        
        // Z 形状
        {
            TetrisShapeType.Z,
            new TetrisShape(TetrisShapeType.Z, new Vector2Int[] {
                new Vector2Int(0, 0),
                new Vector2Int(1, 0),
                new Vector2Int(1, 1),
                new Vector2Int(2, 1)
            }, new Color(0.9f, 0.0f, 0.0f)) // 红色
        },
        
        // Single (单个方块)
        {
            TetrisShapeType.Single,
            new TetrisShape(TetrisShapeType.Single, new Vector2Int[] {
                new Vector2Int(0, 0)
            }, new Color(0.8f, 0.8f, 0.8f)) // 灰色
        }
    };

    /// <summary>
    /// 创建一个形状组
    /// </summary>
    public static GameObject CreateShape(TetrisShapeType shapeType, Vector2 centerPosition, Transform parent = null)
    {
        if (!shapeDefinitions.ContainsKey(shapeType))
        {
            Debug.LogError($"未定义的形状类型: {shapeType}");
            return null;
        }

        TetrisShape shapeDef = shapeDefinitions[shapeType];

        // 创建形状组父对象
        GameObject shapeGroup = new GameObject($"Shape_{shapeType}");
        shapeGroup.transform.position = centerPosition;

        if (parent != null)
        {
            shapeGroup.transform.SetParent(parent);
        }

        // 添加 ShapeGroup 组件
        ShapeGroup group = shapeGroup.AddComponent<ShapeGroup>();
        group.shapeType = shapeType;

        // 创建所有方块
        foreach (Vector2Int blockPos in shapeDef.blockPositions)
        {
            Vector2 worldPos = centerPosition + new Vector2(blockPos.x, blockPos.y);
            GameObject block = BlockFactory.CreateBlock(worldPos, shapeGroup.transform);

            // 设置方块颜色
            Block blockScript = block.GetComponent<Block>();
            if (blockScript != null)
            {
                blockScript.SetColor(shapeDef.shapeColor);
                group.blocks.Add(blockScript);
            }
        }

        return shapeGroup;
    }

    /// <summary>
    /// 获取形状的方块位置数组
    /// </summary>
    public static Vector2Int[] GetShapePositions(TetrisShapeType shapeType)
    {
        if (!shapeDefinitions.ContainsKey(shapeType))
            return new Vector2Int[] { Vector2Int.zero };

        return shapeDefinitions[shapeType].blockPositions;
    }

    /// <summary>
    /// 获取随机形状类型
    /// </summary>
    public static TetrisShapeType GetRandomShapeType()
    {
        var shapeTypes = System.Enum.GetValues(typeof(TetrisShapeType));
        return (TetrisShapeType)shapeTypes.GetValue(Random.Range(0, shapeTypes.Length));
    }

    /// <summary>
    /// 获取形状的宽度（方块数）
    /// </summary>
    public static int GetShapeWidth(TetrisShapeType shapeType)
    {
        if (!shapeDefinitions.ContainsKey(shapeType))
            return 1;

        var positions = shapeDefinitions[shapeType].blockPositions;
        int minX = int.MaxValue;
        int maxX = int.MinValue;

        foreach (var pos in positions)
        {
            if (pos.x < minX) minX = pos.x;
            if (pos.x > maxX) maxX = pos.x;
        }

        return maxX - minX + 1;
    }

    /// <summary>
    /// 获取形状的高度（方块数）
    /// </summary>
    public static int GetShapeHeight(TetrisShapeType shapeType)
    {
        if (!shapeDefinitions.ContainsKey(shapeType))
            return 1;

        var positions = shapeDefinitions[shapeType].blockPositions;
        int minY = int.MaxValue;
        int maxY = int.MinValue;

        foreach (var pos in positions)
        {
            if (pos.y < minY) minY = pos.y;
            if (pos.y > maxY) maxY = pos.y;
        }

        return maxY - minY + 1;
    }
}
