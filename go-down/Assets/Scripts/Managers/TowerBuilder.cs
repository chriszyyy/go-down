using UnityEngine;

/// <summary>
/// 塔构建器 - 使用预制体拼装塔
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

        // 简单的智能填充算法
        for (int layer = 0; layer < towerLayers; layer++)
        {
            FillLayer(layer);
        }

        Debug.Log($"塔构建完成: {towerLayers} 层, 共 {allBlocks.Count} 个方块");
    }

    /// <summary>
    /// 填充一层
    /// </summary>
    void FillLayer(int layerIndex)
    {
        var layerBlocks = new System.Collections.Generic.List<GameObject>();
        float layerY = startHeight + layerIndex * layerSpacing;

        int currentX = 0;
        while (currentX < layerWidth)
        {
            GameObject blockPrefab = SelectRandomPrefab(layerWidth - currentX);
            if (blockPrefab == null)
            {
                Debug.LogWarning($"第 {layerIndex} 层位置 {currentX} 无法放置方块");
                break;
            }

            // 计算放置位置
            float worldX = (currentX - layerWidth / 2f) * 1f + 0.5f;
            Vector3 position = new Vector3(worldX, layerY, 0);

            // 实例化方块
            GameObject block = Instantiate(blockPrefab, position, Quaternion.identity, transform);
            block.name = $"Block_L{layerIndex}_X{currentX}";

            // 获取方块宽度（假设方块有TowerBlock组件）
            TowerBlock towerBlock = block.GetComponent<TowerBlock>();
            if (towerBlock != null)
            {
                towerBlock.Freeze(); // 初始为静态
            }

            layerBlocks.Add(block);
            allBlocks.Add(block);

            // 根据预制体类型决定占用宽度
            int blockWidth = GetBlockWidth(blockPrefab);
            currentX += blockWidth;
        }

        if (layerBlocks.Count > 0)
        {
            blocksByLayer[layerIndex] = layerBlocks;
        }
    }

    /// <summary>
    /// 选择随机预制体（根据剩余空间）
    /// </summary>
    GameObject SelectRandomPrefab(int remainingWidth)
    {
        var availablePrefabs = new System.Collections.Generic.List<GameObject>();

        if (remainingWidth >= 4 && lineBlockPrefab != null) availablePrefabs.Add(lineBlockPrefab);
        if (remainingWidth >= 2 && squareBlockPrefab != null) availablePrefabs.Add(squareBlockPrefab);
        if (remainingWidth >= 2 && l3BlockPrefab != null) availablePrefabs.Add(l3BlockPrefab);
        if (remainingWidth >= 2 && l4BlockPrefab != null) availablePrefabs.Add(l4BlockPrefab);
        if (remainingWidth >= 2 && l5BlockPrefab != null) availablePrefabs.Add(l5BlockPrefab);
        if (singleBlockPrefab != null) availablePrefabs.Add(singleBlockPrefab);

        if (availablePrefabs.Count == 0) return null;

        return availablePrefabs[Random.Range(0, availablePrefabs.Count)];
    }

    /// <summary>
    /// 获取方块宽度（格子数）
    /// </summary>
    int GetBlockWidth(GameObject prefab)
    {
        if (prefab == lineBlockPrefab) return 4;
        if (prefab == squareBlockPrefab) return 2;
        if (prefab == l3BlockPrefab) return 2;
        if (prefab == l4BlockPrefab) return 2;
        if (prefab == l5BlockPrefab) return 2;
        return 1; // 单格
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
