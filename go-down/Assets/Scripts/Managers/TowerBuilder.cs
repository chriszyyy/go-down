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

    // 网格占用矩阵 [层][列] = 是否被占用
    private bool[,] gridOccupied;

    void Start()
    {
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
        // FillLayerWithGrid(0);

        // TODO: 恢复完整塔生成
        for (int layer = 0; layer < towerLayers; layer++)
        {
            FillLayerWithGrid(layer);
        }

        Debug.Log($"调试塔构建完成: 1 层");
    }

    /// <summary>
    /// 基于网格填充一层（随机放置算法）
    /// </summary>
    void FillLayerWithGrid(int layerIndex)
    {
        Debug.Log($"=== 第 {layerIndex} 层开始填充 ===");

        // DEBUG: 只使用L3方块，从左到右随机填充，直到放不下为止
        Debug.Log("调试模式：使用L3方块随机填充第一层");

        int currentCol = 0;
        int attempts = 0;
        int maxAttempts = 100; // 防止无限循环
        int blockCount = 0;

        while (currentCol < layerWidth && attempts < maxAttempts)
        {
            attempts++;

            // 尝试在当前列放置L3方块
            GameObject placedBlock = TryPlaceBlockAt(layerIndex, currentCol);

            if (placedBlock != null)
            {
                blockCount++;
                Debug.Log($"  成功放置方块 at 列{currentCol}");
                // 移动到下一个未占用的列
                currentCol = FindNextEmptyColumn(layerIndex, currentCol);
            }
            else
            {
                // 当前列放不下，尝试下一列
                currentCol++;
            }

            // 如果找不到空列，结束
            if (currentCol >= layerWidth)
            {
                Debug.Log("  已填满或无法继续放置");
                break;
            }
        }

        Debug.Log($"=== 第 {layerIndex} 层完成，共 {blockCount} 个方块 ===\n");
    }

    /// <summary>
    /// 找到当前列之后的第一个空列
    /// </summary>
    int FindNextEmptyColumn(int layer, int startCol)
    {
        for (int col = startCol; col < layerWidth; col++)
        {
            if (!gridOccupied[layer, col])
            {
                return col;
            }
        }
        return layerWidth; // 没有空列
    }

    /// <summary>
    /// 尝试在指定位置放置方块
    /// </summary>
    GameObject TryPlaceBlockAt(int layer, int col)
    {
        // DEBUG: 只使用单个方块
        if (singleBlockPrefab == null) return null;

        // 尝试所有旋转角度（随机顺序）
        var rotations = new System.Collections.Generic.List<float> { 0f, 90f, 180f, 270f };
        ShuffleList(rotations);

        foreach (float rotation in rotations)
        {
            // 检查是否能放置
            if (CanPlaceBlock(singleBlockPrefab, rotation, layer, col))
            {
                // 放置方块
                GameObject block = PlaceBlock(singleBlockPrefab, rotation, layer, col);
                return block;
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

        // 获取旋转后占用的格子（相对于pivot点）
        var occupiedCells = block.GetOccupiedCells(rotation);

        // 检查所有占用格子是否都在边界内且未被占用
        foreach (var (dx, dy) in occupiedCells)
        {
            int checkCol = col + dx;
            int checkLayer = layer + dy;

            // 检查边界
            if (checkCol < 0 || checkCol >= layerWidth || checkLayer < 0 || checkLayer >= towerLayers)
            {
                Debug.Log($"    边界检查失败: 格子({checkCol},{checkLayer}) 超出范围 [0-{layerWidth - 1}, 0-{towerLayers - 1}]");
                return false;
            }

            // 检查占用状态
            if (gridOccupied[checkLayer, checkCol])
            {
                Debug.Log($"    占用检查失败: 格子({checkCol},{checkLayer}) 已被占用");
                return false;
            }
        }

        Debug.Log($"    可以放置: pivot({col},{layer}), 占用格子: {string.Join(", ", occupiedCells)}");
        return true;
    }

    /// <summary>
    /// 放置方块（pivot点直接使用col,layer坐标，并标记占用）
    /// </summary>
    GameObject PlaceBlock(GameObject prefab, float rotation, int layer, int col)
    {
        TowerBlock blockComponent = prefab.GetComponent<TowerBlock>();

        // 取出对应pivot点，放置在网格位置(col, layer)
        Vector2Int bottomLeftCorner = blockComponent.GetBottomLeftCorner(rotation);
        float worldX = col - bottomLeftCorner.x;
        float worldY = layer - bottomLeftCorner.y;

        Vector3 position = new Vector3(worldX, worldY, 0);

        // 获取占用格子
        var occupiedCells = blockComponent.GetOccupiedCells(rotation);

        // DEBUG: 坐标信息
        Debug.Log($"  放置 {blockComponent.blockTypeName} at 网格[{col},{layer}]");
        Debug.Log($"    Pivot点世界坐标: ({worldX:F2},{worldY:F2}), 旋转={rotation}°");
        Debug.Log($"    占用格子(相对pivot): {string.Join(", ", occupiedCells)}");

        // 创建方块（应用旋转）
        Quaternion blockRotation = Quaternion.Euler(0, 0, rotation);
        GameObject block = Instantiate(prefab, position, blockRotation, transform);
        block.name = $"Block_L{layer}_C{col}_{blockComponent.blockTypeName}_R{rotation}";

        // 标记网格占用
        foreach (var (dx, dy) in occupiedCells)
        {
            int occupyCol = col + dx;
            int occupyLayer = layer + dy;
            gridOccupied[occupyLayer, occupyCol] = true;
            Debug.Log($"    标记格子({occupyCol},{occupyLayer})为已占用");
        }

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
    /// 清空塔
    /// </summary>
    public void ClearTower()
    {
        // 销毁所有子对象
        foreach (Transform child in transform)
        {
            if (child.gameObject != hexagonBall)
            {
                Destroy(child.gameObject);
            }
        }
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
        // 在编辑器中绘制塔的边界（从0,0开始向右画）
        if (!Application.isPlaying)
        {
            Gizmos.color = Color.cyan;

            float width = layerWidth * 1f;
            float height = towerLayers * 1f;
            // 中心点应该在网格的中心：(layerWidth/2, towerLayers/2)
            Vector3 center = new Vector3(width / 2f, height / 2f, 0);
            Gizmos.DrawWireCube(center, new Vector3(width, height, 0.1f));
        }
    }
}
