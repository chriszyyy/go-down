using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 固定塔管理器 - 生成固定布局的方块塔用于测试核心功能
/// </summary>
public class FixedTowerManager : MonoBehaviour
{
    [Header("塔配置")]
    [Tooltip("塔的层数")]
    public int towerLayers = 8;

    [Tooltip("每层的方块数")]
    public int blocksPerLayer = 8;

    [Tooltip("方块大小")]
    public float blockSize = 1f;

    [Tooltip("层间距")]
    public float layerSpacing = 1f;

    [Tooltip("起始高度")]
    public float startHeight = -3f;

    [Header("六边形球配置")]
    [Tooltip("是否自动生成六边形球")]
    public bool spawnHexagonBall = true;

    [Tooltip("球相对于顶部的高度偏移")]
    public float ballHeightOffset = 1.5f;

    // 存储所有方块
    private List<GameObject> allBlocks = new List<GameObject>();
    private Dictionary<int, List<GameObject>> blocksByLayer = new Dictionary<int, List<GameObject>>();
    private GameObject hexagonBall;

    void Start()
    {
        // 订阅方块消除事件
        Block.OnBlockDestroyed += HandleBlockDestroyed;

        // 生成固定塔
        GenerateFixedTower();

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
    /// 生成固定布局的塔
    /// </summary>
    public void GenerateFixedTower()
    {
        ClearTower();

        // 从底部开始，逐层生成
        for (int layer = 0; layer < towerLayers; layer++)
        {
            GenerateLayer(layer);
        }

        Debug.Log($"生成固定塔完成: {towerLayers} 层, 每层 {blocksPerLayer} 个方块, 共 {allBlocks.Count} 个方块");
    }

    /// <summary>
    /// 生成一层方块
    /// </summary>
    void GenerateLayer(int layerIndex)
    {
        List<GameObject> layerBlocks = new List<GameObject>();
        float layerY = startHeight + layerIndex * layerSpacing;

        // 计算起始X坐标（居中）
        float startX = -(blocksPerLayer * blockSize) / 2f + blockSize / 2f;

        for (int i = 0; i < blocksPerLayer; i++)
        {
            float blockX = startX + i * blockSize;
            Vector2 position = new Vector2(blockX, layerY);

            // 创建方块
            GameObject block = BlockFactory.CreateBlock(position, transform);
            block.name = $"Block_L{layerIndex}_X{i}";

            // 获取 Block 组件并设置为静态（不受重力影响）
            Block blockComponent = block.GetComponent<Block>();
            if (blockComponent != null)
            {
                blockComponent.Freeze();
            }

            layerBlocks.Add(block);
            allBlocks.Add(block);
        }

        blocksByLayer[layerIndex] = layerBlocks;
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
    /// 处理方块被消除事件
    /// </summary>
    void HandleBlockDestroyed(Block destroyedBlock)
    {
        Debug.Log($"方块被消除: {destroyedBlock.name}");

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
                        Block block = blockObj.GetComponent<Block>();
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
    /// 重置塔
    /// </summary>
    public void ResetTower()
    {
        ClearTower();

        if (hexagonBall != null)
        {
            Destroy(hexagonBall);
        }

        GenerateFixedTower();

        if (spawnHexagonBall)
        {
            SpawnHexagonBall();
        }
    }

    /// <summary>
    /// 获取统计信息
    /// </summary>
    public void PrintStatistics()
    {
        int totalBlocks = 0;
        for (int layer = 0; layer < towerLayers; layer++)
        {
            if (blocksByLayer.ContainsKey(layer))
            {
                int layerCount = blocksByLayer[layer].Count;
                totalBlocks += layerCount;
                Debug.Log($"第 {layer} 层剩余方块: {layerCount}");
            }
        }
        Debug.Log($"塔剩余总方块数: {totalBlocks}");
    }

    void OnDrawGizmos()
    {
        // 在编辑器中绘制塔的边界
        if (!Application.isPlaying)
        {
            Gizmos.color = Color.cyan;

            float width = blocksPerLayer * blockSize;
            float height = towerLayers * layerSpacing;
            Vector3 center = new Vector3(0, startHeight + height / 2f, 0);
            Gizmos.DrawWireCube(center, new Vector3(width, height, 0.1f));
        }
    }
}
