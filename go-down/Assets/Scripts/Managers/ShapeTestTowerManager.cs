using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 形状测试管理器 - 测试不同形状的方块组合
/// </summary>
public class ShapeTestTowerManager : MonoBehaviour
{
    [Header("塔配置")]
    [Tooltip("塔的层数")]
    public int towerLayers = 8;

    [Tooltip("塔的宽度（格子数）")]
    public int towerWidth = 8;

    [Tooltip("方块大小")]
    public float blockSize = 1f;

    [Tooltip("层间距")]
    public float layerSpacing = 1f;

    [Tooltip("起始高度")]
    public float startHeight = -3f;

    [Header("形状配置")]
    [Tooltip("使用随机形状")]
    public bool useRandomShapes = true;

    [Header("六边形球配置")]
    [Tooltip("是否自动生成六边形球")]
    public bool spawnHexagonBall = true;

    [Tooltip("球相对于顶部的高度偏移")]
    public float ballHeightOffset = 1.5f;

    // 存储所有形状
    private List<GameObject> allShapes = new List<GameObject>();
    private Dictionary<int, List<GameObject>> shapesByLayer = new Dictionary<int, List<GameObject>>();
    private GameObject hexagonBall;

    // 塔的占用情况
    private HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>();

    void Start()
    {
        // 订阅方块消除事件
        Block.OnBlockDestroyed += HandleBlockDestroyed;

        // 生成形状塔
        GenerateShapeTower();

        // 生成六边形球
        if (spawnHexagonBall)
        {
            SpawnHexagonBall();
        }
    }

    void OnDestroy()
    {
        Block.OnBlockDestroyed -= HandleBlockDestroyed;
    }

    /// <summary>
    /// 生成形状塔
    /// </summary>
    public void GenerateShapeTower()
    {
        ClearTower();
        occupiedCells.Clear();

        // 从底部开始，逐层填充
        for (int layer = 0; layer < towerLayers; layer++)
        {
            FillLayer(layer);
        }

        Debug.Log($"生成形状塔完成: {towerLayers} 层, 共 {allShapes.Count} 个形状, 占用格子: {occupiedCells.Count}");
    }

    /// <summary>
    /// 填充一层
    /// </summary>
    void FillLayer(int layerIndex)
    {
        List<GameObject> layerShapes = new List<GameObject>();
        float layerY = startHeight + layerIndex * layerSpacing;

        // 从左到右扫描这一层，找到空位并填充
        for (int x = 0; x < towerWidth;)
        {
            // 检查当前位置是否已被占用
            Vector2Int cellPos = new Vector2Int(x, layerIndex);
            if (occupiedCells.Contains(cellPos))
            {
                x++;
                continue;
            }

            // 尝试放置形状
            GameObject placedShape = TryPlaceShape(x, layerIndex, layerY);

            if (placedShape != null)
            {
                layerShapes.Add(placedShape);
                allShapes.Add(placedShape);

                // 跳过已占用的格子
                BlockShapeGroup group = placedShape.GetComponent<BlockShapeGroup>();
                if (group != null)
                {
                    int shapeWidth = BlockShapeManager.GetShapeWidth(group.shapeType);
                    x += shapeWidth;
                }
                else
                {
                    x++;
                }
            }
            else
            {
                x++;
            }
        }

        if (layerShapes.Count > 0)
        {
            shapesByLayer[layerIndex] = layerShapes;
        }
    }

    /// <summary>
    /// 尝试在指定位置放置形状
    /// </summary>
    GameObject TryPlaceShape(int startX, int startY, float worldY)
    {
        // 计算剩余空间
        int remainingWidth = towerWidth - startX;

        // 获取可以尝试的形状列表
        List<BlockShapeType> shapesToTry = GetShapePriority(remainingWidth);

        foreach (BlockShapeType shapeType in shapesToTry)
        {
            // 检查形状是否能放入
            if (CanPlaceShape(shapeType, startX, startY))
            {
                // 放置形状
                return PlaceShape(shapeType, startX, startY, worldY);
            }
        }

        return null;
    }

    /// <summary>
    /// 获取形状放置优先级
    /// </summary>
    List<BlockShapeType> GetShapePriority(int remainingWidth)
    {
        List<BlockShapeType> priority = new List<BlockShapeType>();

        if (useRandomShapes)
        {
            // 根据剩余宽度选择合适的形状
            var allShapes = new List<BlockShapeType>();

            if (remainingWidth >= 4) allShapes.Add(BlockShapeType.Line4);
            if (remainingWidth >= 2)
            {
                allShapes.Add(BlockShapeType.Square);
                allShapes.Add(BlockShapeType.L3);
                allShapes.Add(BlockShapeType.L4);
                allShapes.Add(BlockShapeType.L5);
            }
            allShapes.Add(BlockShapeType.Single);

            // 随机打乱
            for (int i = 0; i < allShapes.Count; i++)
            {
                int randomIndex = Random.Range(i, allShapes.Count);
                var temp = allShapes[i];
                allShapes[i] = allShapes[randomIndex];
                allShapes[randomIndex] = temp;
            }

            priority = allShapes;
        }
        else
        {
            priority.Add(BlockShapeType.Single);
        }

        return priority;
    }

    /// <summary>
    /// 检查形状是否能放置
    /// </summary>
    bool CanPlaceShape(BlockShapeType shapeType, int startX, int startY)
    {
        var shapeData = BlockShapeManager.GetShapeData(shapeType);

        foreach (Vector2Int relPos in shapeData.positions)
        {
            int cellX = startX + relPos.x;
            int cellY = startY + relPos.y;

            // 检查边界
            if (cellX < 0 || cellX >= towerWidth || cellY < 0 || cellY >= towerLayers)
            {
                return false;
            }

            // 检查占用
            if (occupiedCells.Contains(new Vector2Int(cellX, cellY)))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 放置形状
    /// </summary>
    GameObject PlaceShape(BlockShapeType shapeType, int startX, int startY, float worldY)
    {
        // 计算世界坐标位置（左下角对齐）
        float worldX = (startX - towerWidth / 2f) * blockSize + blockSize / 2f;
        Vector2 worldPosition = new Vector2(worldX, worldY);

        // 创建形状
        GameObject shape = BlockShapeFactory.CreateShape(shapeType, worldPosition, blockSize, transform);
        shape.name = $"Shape_L{startY}_{shapeType}_X{startX}";

        // 标记占用的格子
        var shapeData = BlockShapeManager.GetShapeData(shapeType);
        foreach (Vector2Int relPos in shapeData.positions)
        {
            int cellX = startX + relPos.x;
            int cellY = startY + relPos.y;
            occupiedCells.Add(new Vector2Int(cellX, cellY));
        }

        return shape;
    }

    /// <summary>
    /// 清空塔
    /// </summary>
    public void ClearTower()
    {
        foreach (GameObject shape in allShapes)
        {
            if (shape != null)
            {
                Destroy(shape);
            }
        }

        allShapes.Clear();
        shapesByLayer.Clear();
    }

    /// <summary>
    /// 生成六边形球
    /// </summary>
    void SpawnHexagonBall()
    {
        float topY = startHeight + towerLayers * layerSpacing;
        Vector2 ballPosition = new Vector2(0, topY + ballHeightOffset);

        hexagonBall = HexagonBallFactory.CreateHexagonBall(ballPosition, transform);
        hexagonBall.name = "HexagonBall";

        Debug.Log($"生成六边形球 at Y: {ballPosition.y}");
    }

    /// <summary>
    /// 重置塔
    /// </summary>
    public void ResetTower()
    {
        ClearTower();

        if (hexagonBall != null)
        {
            Destroy(hexagonBall);
        }

        GenerateShapeTower();

        if (spawnHexagonBall)
        {
            SpawnHexagonBall();
        }
    }

    /// <summary>
    /// 处理方块被消除事件
    /// </summary>
    void HandleBlockDestroyed(Block destroyedBlock)
    {
        // 找到被点击方块所属的形状组
        BlockShapeGroup parentShape = destroyedBlock.GetComponentInParent<BlockShapeGroup>();

        if (parentShape != null)
        {
            Debug.Log($"形状被点击: {parentShape.shapeType}");

            // 找出被消除形状的层级
            int destroyedLayer = GetShapeLayer(parentShape.gameObject);

            // 消除整个形状组
            parentShape.DestroyShapeGroup();

            // 激活上层所有形状的物理
            ActivateShapesAboveLayer(destroyedLayer);
        }
    }

    /// <summary>
    /// 获取形状所在的层级
    /// </summary>
    int GetShapeLayer(GameObject shape)
    {
        foreach (var kvp in shapesByLayer)
        {
            if (kvp.Value.Contains(shape))
            {
                return kvp.Key;
            }
        }
        return -1;
    }

    /// <summary>
    /// 激活指定层级以上所有形状的物理
    /// </summary>
    void ActivateShapesAboveLayer(int layerIndex)
    {
        for (int layer = layerIndex + 1; layer < towerLayers; layer++)
        {
            if (shapesByLayer.ContainsKey(layer))
            {
                foreach (GameObject shapeObj in shapesByLayer[layer])
                {
                    if (shapeObj != null)
                    {
                        BlockShapeGroup shape = shapeObj.GetComponent<BlockShapeGroup>();
                        if (shape != null)
                        {
                            shape.MakeDynamic();
                        }
                    }
                }
            }
        }

        Debug.Log($"激活 {layerIndex} 层以上的形状物理");
    }

    void OnDrawGizmos()
    {
        // 在编辑器中绘制塔的边界
        if (!Application.isPlaying)
        {
            Gizmos.color = Color.yellow;

            float width = towerWidth * blockSize;
            float height = towerLayers * layerSpacing;
            Vector3 center = new Vector3(0, startHeight + height / 2f, 0);
            Gizmos.DrawWireCube(center, new Vector3(width, height, 0.1f));
        }
    }
}
