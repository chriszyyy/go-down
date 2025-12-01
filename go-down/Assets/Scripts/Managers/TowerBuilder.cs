using UnityEngine;

/// <summary>
/// 塔构建器 - 使用预制体拼装塔（基于网格的随机填充算法）
/// </summary>
public class TowerBuilder : MonoBehaviour
{
    [Header("塔配置")]
    [Tooltip("塔的层数")]
    public int towerLayers = 8;

    [Tooltip("每层的宽度（单位：方块）")]
    public int layerWidth = 8;

    [Tooltip("层间距")]
    public float layerSpacing = 1f;

    [Tooltip("起始高度")]
    public float startHeight = -3f;

    [Header("方块预制体")]
    [Tooltip("单格方块预制体")]
    public GameObject singleBlockPrefab;

    [Tooltip("正方形（2x2）预制体")]
    public GameObject squareBlockPrefab;

    [Tooltip("L3型预制体")]
    public GameObject l3BlockPrefab;

    [Tooltip("L4型预制体")]
    public GameObject l4BlockPrefab;

    [Tooltip("L5型预制体")]
    public GameObject l5BlockPrefab;

    [Tooltip("I型（4格）预制体")]
    public GameObject lineBlockPrefab;

    [Header("六边形球配置")]
    [Tooltip("六边形球预制体")]
    public GameObject hexagonBallPrefab;

    [Tooltip("是否自动生成六边形球")]
    public bool spawnHexagonBall = true;

    [Tooltip("球相对于顶部的高度偏移")]
    public float ballHeightOffset = 1.5f;

    // 运行时数据
    private GameObject hexagonBall;
    private System.Collections.Generic.List<GameObject> allBlocks = new System.Collections.Generic.List<GameObject>();
    private System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<GameObject>> blocksByLayer =
        new System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<GameObject>>();

    // 网格占用矩阵 [层][列] = 是否被占用
    private bool[,] gridOccupied;

    void Start()
    {
        // 订阅方块消除事件
        TowerBlock.OnBlockDestroyed += HandleBlockDestroyed;
        TowerBlock.OnBlockScored += HandleBlockScored;

        // 构建塔
        BuildTower();

        // 生成六边形球
        if (spawnHexagonBall && hexagonBallPrefab != null)
        {
            SpawnHexagonBall();
        }
    }

    void OnDestroy()
    {
        TowerBlock.OnBlockDestroyed -= HandleBlockDestroyed;
        TowerBlock.OnBlockScored -= HandleBlockScored;
    }

    /// <summary>
    /// 构建塔
    /// </summary>
    public void BuildTower()
    {
        ClearTower();

        // 初始化网格占用矩阵
        gridOccupied = new bool[towerLayers, layerWidth];

        // DEBUG: 只生成第一层（底层）进行测试
        Debug.Log("=== 调试模式：只生成第一层 ===");
        FillLayerWithGrid(0);

        // TODO: 恢复完整塔生成
        // for (int layer = 0; layer < towerLayers; layer++)
        // {
        //     FillLayerWithGrid(layer);
        // }

        Debug.Log($"调试塔构建完成: 1 层, 共 {allBlocks.Count} 个方块");
    }

    /// <summary>
    /// 基于网格填充一层（随机放置算法）
    /// </summary>
    void FillLayerWithGrid(int layerIndex)
    {
        var layerBlocks = new System.Collections.Generic.List<GameObject>();

        Debug.Log($"=== 第 {layerIndex} 层开始填充 ===");

        // DEBUG: 测试所有4个L3旋转，放置在固定位置便于观察
        Debug.Log("调试模式：放置4个L3方块在固定位置，显示所有旋转");

        // 在底层放置4个L3方块，每个间隔2格
        // 位置: 0, 2, 4, 6 (留出足够空间避免重叠)
        var testConfigs = new[]
        {
            new { col = 0, rotation = 0f, name = "0度" },
            new { col = 2, rotation = 90f, name = "90度" },
            new { col = 4, rotation = 180f, name = "180度" },
            new { col = 6, rotation = 270f, name = "270度" }
        };

        foreach (var config in testConfigs)
        {
            if (config.col >= layerWidth) continue;

            if (l3BlockPrefab != null)
            {
                // 强制放置，不检查占用（测试用）
                GameObject placedBlock = PlaceBlock(l3BlockPrefab, config.rotation, layerIndex, config.col, layerBlocks);
                Debug.Log($"  放置L3方块 at 列{config.col}, 旋转={config.name}");
            }
        }

        if (layerBlocks.Count > 0)
        {
            blocksByLayer[layerIndex] = layerBlocks;
        }

        Debug.Log($"=== 第 {layerIndex} 层完成，共 {layerBlocks.Count} 个方块 ===\n");
    }

    /// <summary>
    /// 尝试在指定位置放置方块
    /// </summary>
    GameObject TryPlaceBlockAt(int layer, int col, System.Collections.Generic.List<GameObject> layerBlocks)
    {
        var allPrefabs = GetAllAvailablePrefabs();
        var availablePrefabs = new System.Collections.Generic.List<GameObject>(allPrefabs);

        // 打乱Prefab顺序（随机选择）
        ShuffleList(availablePrefabs);

        foreach (GameObject prefab in availablePrefabs)
        {
            // 尝试所有旋转角度（随机顺序）
            var rotations = new System.Collections.Generic.List<float> { 0f, 90f, 180f, 270f };
            ShuffleList(rotations);

            foreach (float rotation in rotations)
            {
                // 检查是否能放置
                if (CanPlaceBlock(prefab, rotation, layer, col))
                {
                    // 放置方块
                    GameObject block = PlaceBlock(prefab, rotation, layer, col, layerBlocks);
                    return block;
                }
            }
        }

        return null; // 无法放置
    }

    /// <summary>
    /// 检查方块是否可以放置在指定位置
    /// </summary>
    bool CanPlaceBlock(GameObject prefab, float rotation, int layer, int col)
    {
        TowerBlock block = prefab.GetComponent<TowerBlock>();
        if (block == null)
        {
            return false;
        }

        // 直接获取实际占用的格子
        var occupiedCells = block.GetOccupiedCells(rotation);

        // 检查所有占用格子是否都在边界内且未被占用
        foreach (var (dx, dy) in occupiedCells)
        {
            int checkCol = col + dx;
            int checkLayer = layer + dy;

            // 检查边界
            if (checkCol < 0 || checkCol >= layerWidth || checkLayer < 0 || checkLayer >= towerLayers)
            {
                return false; // 超出边界
            }

            // 检查占用状态
            if (gridOccupied[checkLayer, checkCol])
            {
                return false; // 已被占用
            }
        }

        return true; // 可以放置
    }

    /// <summary>
    /// 放置方块并标记网格占用
    /// </summary>
    GameObject PlaceBlock(GameObject prefab, float rotation, int layer, int col, System.Collections.Generic.List<GameObject> layerBlocks)
    {
        TowerBlock blockComponent = prefab.GetComponent<TowerBlock>();

        // 获取实际占用的格子
        var occupiedCells = blockComponent.GetOccupiedCells(rotation);

        // 计算占用区域的边界盒（用于位置计算）
        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;

        foreach (var (dx, dy) in occupiedCells)
        {
            minX = Mathf.Min(minX, dx);
            maxX = Mathf.Max(maxX, dx);
            minY = Mathf.Min(minY, dy);
            maxY = Mathf.Max(maxY, dy);
        }

        // 计算边界盒尺寸
        int boundingWidth = maxX - minX + 1;
        int boundingHeight = maxY - minY + 1;

        // GRID-BASED COORDINATE SYSTEM (简化版)
        // 使用底部左侧作为塔的原点，每个格子为1x1单位
        float towerOriginX = -layerWidth / 2f; // 塔的左边缘
        float towerOriginY = startHeight; // 塔的底部

        // 计算格子的底-左位置 (格子[0,0]的左下角)
        float gridX = towerOriginX + col; // 格子的左边缘
        float gridY = towerOriginY + layer; // 格子的底边缘

        // 由于Unity使用中心pivot，需要计算方块的实际几何中心
        // 考虑到占用格子可能不从(0,0)开始（如旋转后的形状）
        float actualCenterX = gridX + (minX + maxX) * 0.5f + 0.5f; // 实际几何中心X
        float actualCenterY = gridY + (minY + maxY) * 0.5f + 0.5f; // 实际几何中心Y

        Vector3 position = new Vector3(actualCenterX, actualCenterY, 0);

        // DEBUG: 详细坐标信息
        Debug.Log($"    DEBUG坐标计算:");
        Debug.Log($"      塔原点: ({towerOriginX:F2}, {towerOriginY:F2})");
        Debug.Log($"      格子[{layer},{col}]底左: ({gridX:F2}, {gridY:F2})");
        Debug.Log($"      占用格子: {string.Join(", ", occupiedCells)}");
        Debug.Log($"      占用格子范围: X[{minX}~{maxX}], Y[{minY}~{maxY}]");
        Debug.Log($"      方块中心: ({actualCenterX:F2}, {actualCenterY:F2})");
        Debug.Log($"      旋转角度: {rotation}°");

        // 恢复正常的grid定位
        GameObject block = Instantiate(prefab, position, Quaternion.Euler(0, 0, rotation), transform);
        block.name = $"Block_L{layer}_C{col}_{blockComponent.blockTypeName}_R{rotation}";

        TowerBlock towerBlock = block.GetComponent<TowerBlock>();
        if (towerBlock != null)
        {
            towerBlock.Freeze();
        }

        layerBlocks.Add(block);
        allBlocks.Add(block);

        // 标记实际占用的格子
        foreach (var (dx, dy) in occupiedCells)
        {
            gridOccupied[layer + dy, col + dx] = true;
        }

        Debug.Log($"  放置 {blockComponent.blockTypeName} at 格子[L{layer},C{col}], 世界坐标({actualCenterX:F2},{actualCenterY:F2}), 旋转={rotation}°, 占用{occupiedCells.Count}格");

        return block;
    }

    /// <summary>
    /// 打乱列表顺序（Fisher-Yates洗牌算法）
    /// </summary>
    void ShuffleList<T>(System.Collections.Generic.List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    /// <summary>
    /// 获取所有可用的Prefab列表
    /// </summary>
    System.Collections.Generic.List<GameObject> GetAllAvailablePrefabs()
    {
        var prefabs = new System.Collections.Generic.List<GameObject>();
        if (singleBlockPrefab != null) prefabs.Add(singleBlockPrefab);
        if (squareBlockPrefab != null) prefabs.Add(squareBlockPrefab);
        if (l3BlockPrefab != null) prefabs.Add(l3BlockPrefab);
        if (l4BlockPrefab != null) prefabs.Add(l4BlockPrefab);
        if (l5BlockPrefab != null) prefabs.Add(l5BlockPrefab);
        if (lineBlockPrefab != null) prefabs.Add(lineBlockPrefab);
        return prefabs;
    }

    /// <summary>
    /// 生成六边形球
    /// </summary>
    void SpawnHexagonBall()
    {
        float topY = startHeight + towerLayers * layerSpacing;
        Vector3 ballPosition = new Vector3(0, topY + ballHeightOffset, 0);

        hexagonBall = Instantiate(hexagonBallPrefab, ballPosition, Quaternion.identity, transform);
        hexagonBall.name = "HexagonBall";

        Debug.Log($"生成六边形球 at Y: {ballPosition.y}");
    }

    /// <summary>
    /// 处理方块被消除事件
    /// </summary>
    void HandleBlockDestroyed(TowerBlock destroyedBlock)
    {
        Debug.Log($"方块被消除: {destroyedBlock.blockTypeName}");

        // 找出被消除方块的层级
        int destroyedLayer = GetBlockLayer(destroyedBlock.gameObject);

        if (destroyedLayer >= 0)
        {
            // 从该层移除这个方块
            if (blocksByLayer.ContainsKey(destroyedLayer))
            {
                blocksByLayer[destroyedLayer].Remove(destroyedBlock.gameObject);
            }

            // 激活上层所有方块的物理
            ActivateBlocksAboveLayer(destroyedLayer);
        }
    }

    /// <summary>
    /// 处理方块得分事件
    /// </summary>
    void HandleBlockScored(TowerBlock block, int score)
    {
        Debug.Log($"得分: {score} (方块: {block.blockTypeName})");
        // TODO: 更新得分系统
    }

    /// <summary>
    /// 获取方块所在的层级
    /// </summary>
    int GetBlockLayer(GameObject block)
    {
        foreach (var kvp in blocksByLayer)
        {
            if (kvp.Value.Contains(block))
            {
                return kvp.Key;
            }
        }
        return -1;
    }

    /// <summary>
    /// 激活指定层级以上所有方块的物理
    /// </summary>
    void ActivateBlocksAboveLayer(int layerIndex)
    {
        for (int layer = layerIndex + 1; layer < towerLayers; layer++)
        {
            if (blocksByLayer.ContainsKey(layer))
            {
                foreach (GameObject blockObj in blocksByLayer[layer])
                {
                    if (blockObj != null)
                    {
                        TowerBlock block = blockObj.GetComponent<TowerBlock>();
                        if (block != null)
                        {
                            block.MakeDynamic();
                        }
                    }
                }
            }
        }

        Debug.Log($"激活 {layerIndex} 层以上的方块物理");
    }

    /// <summary>
    /// 清空塔
    /// </summary>
    public void ClearTower()
    {
        foreach (GameObject block in allBlocks)
        {
            if (block != null)
            {
                Destroy(block);
            }
        }

        allBlocks.Clear();
        blocksByLayer.Clear();
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

        BuildTower();

        if (spawnHexagonBall && hexagonBallPrefab != null)
        {
            SpawnHexagonBall();
        }
    }

    void OnDrawGizmos()
    {
        // 在编辑器中绘制塔的边界
        if (!Application.isPlaying)
        {
            Gizmos.color = Color.cyan;

            float width = layerWidth * 1f;
            float height = towerLayers * layerSpacing;
            Vector3 center = new Vector3(0, startHeight + height / 2f, 0);
            Gizmos.DrawWireCube(center, new Vector3(width, height, 0.1f));
        }
    }
}
