using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 方块形状类型
/// </summary>
public enum BlockShapeType
{
    Single,      // 单格 1x1: █
    Square,      // 四格正方形 2x2: ██
    L3,          // 三格L形: █
                 //          ██
    L4,          // 四格L形: █
                 //          █
                 //          ██
    L5,          // 五格L形: █
                 //          █
                 //          █
                 //          ██
    Line4        // 四格I形: ████
}

/// <summary>
/// 方块形状数据
/// </summary>
[System.Serializable]
public class BlockShapeData
{
    public BlockShapeType shapeType;
    public Vector2Int[] positions; // 相对于左下角(0,0)的格子位置
    public Color color;
    public string shapeName;

    public BlockShapeData(BlockShapeType type, Vector2Int[] pos, Color col, string name)
    {
        shapeType = type;
        positions = pos;
        color = col;
        shapeName = name;
    }
}

/// <summary>
/// 方块形状管理器
/// </summary>
public static class BlockShapeManager
{
    private static Dictionary<BlockShapeType, BlockShapeData> shapeDatabase;

    static BlockShapeManager()
    {
        InitializeShapeDatabase();
    }

    /// <summary>
    /// 初始化形状数据库
    /// </summary>
    static void InitializeShapeDatabase()
    {
        shapeDatabase = new Dictionary<BlockShapeType, BlockShapeData>();

        // 单格 █
        shapeDatabase[BlockShapeType.Single] = new BlockShapeData(
            BlockShapeType.Single,
            new Vector2Int[] { new Vector2Int(0, 0) },
            new Color(0.9f, 0.9f, 0.9f), // 灰色
            "单格"
        );

        // 四格正方形 2x2
        // ██
        // ██
        shapeDatabase[BlockShapeType.Square] = new BlockShapeData(
            BlockShapeType.Square,
            new Vector2Int[] {
                new Vector2Int(0, 0), new Vector2Int(1, 0),
                new Vector2Int(0, 1), new Vector2Int(1, 1)
            },
            new Color(1f, 0.9f, 0.2f), // 黄色
            "四格正方形"
        );

        // 三格L形
        // █
        // ██
        shapeDatabase[BlockShapeType.L3] = new BlockShapeData(
            BlockShapeType.L3,
            new Vector2Int[] {
                new Vector2Int(0, 0), new Vector2Int(1, 0),
                new Vector2Int(0, 1)
            },
            new Color(1f, 0.5f, 0.2f), // 橙色
            "三格L形"
        );

        // 四格L形
        // █
        // █
        // ██
        shapeDatabase[BlockShapeType.L4] = new BlockShapeData(
            BlockShapeType.L4,
            new Vector2Int[] {
                new Vector2Int(0, 0), new Vector2Int(1, 0),
                new Vector2Int(0, 1),
                new Vector2Int(0, 2)
            },
            new Color(0.2f, 0.8f, 1f), // 蓝色
            "四格L形"
        );

        // 五格L形
        // █
        // █
        // █
        // ██
        shapeDatabase[BlockShapeType.L5] = new BlockShapeData(
            BlockShapeType.L5,
            new Vector2Int[] {
                new Vector2Int(0, 0), new Vector2Int(1, 0),
                new Vector2Int(0, 1),
                new Vector2Int(0, 2),
                new Vector2Int(0, 3)
            },
            new Color(0.6f, 0.2f, 1f), // 紫色
            "五格L形"
        );

        // 四格I形 ████
        shapeDatabase[BlockShapeType.Line4] = new BlockShapeData(
            BlockShapeType.Line4,
            new Vector2Int[] {
                new Vector2Int(0, 0), new Vector2Int(1, 0),
                new Vector2Int(2, 0), new Vector2Int(3, 0)
            },
            new Color(0.2f, 1f, 0.6f), // 青绿色
            "四格I形"
        );
    }

    /// <summary>
    /// 获取形状数据
    /// </summary>
    public static BlockShapeData GetShapeData(BlockShapeType type)
    {
        if (shapeDatabase.ContainsKey(type))
        {
            return shapeDatabase[type];
        }
        return shapeDatabase[BlockShapeType.Single];
    }

    /// <summary>
    /// 获取所有形状类型
    /// </summary>
    public static BlockShapeType[] GetAllShapeTypes()
    {
        return new BlockShapeType[]
        {
            BlockShapeType.Single,
            BlockShapeType.Square,
            BlockShapeType.L3,
            BlockShapeType.L4,
            BlockShapeType.L5,
            BlockShapeType.Line4
        };
    }

    /// <summary>
    /// 获取随机形状类型
    /// </summary>
    public static BlockShapeType GetRandomShapeType()
    {
        var types = GetAllShapeTypes();
        return types[Random.Range(0, types.Length)];
    }

    /// <summary>
    /// 获取形状的宽度
    /// </summary>
    public static int GetShapeWidth(BlockShapeType type)
    {
        var data = GetShapeData(type);
        int maxX = 0;
        foreach (var pos in data.positions)
        {
            maxX = Mathf.Max(maxX, pos.x);
        }
        return maxX + 1;
    }

    /// <summary>
    /// 获取形状的高度
    /// </summary>
    public static int GetShapeHeight(BlockShapeType type)
    {
        var data = GetShapeData(type);
        int maxY = 0;
        foreach (var pos in data.positions)
        {
            maxY = Mathf.Max(maxY, pos.y);
        }
        return maxY + 1;
    }
}
