using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 方块管理器 - 管理方块塔的生成、消除和物理状态
/// </summary>
public class BlockManager : MonoBehaviour
{
    [Header("方块塔配置")]
    [Tooltip("方块塔的层数")]
    public int towerLayers = 10;

    [Tooltip("每层的方块数量（宽度）")]
    public int blocksPerLayer = 5; [Tooltip("方块大小")]
    public float blockSize = 1f;

    [Tooltip("方块间距")]
    public float blockSpacing = 0.05f;

    [Tooltip("层间距")]
    public float layerHeight = 1f;

    [Tooltip("起始高度")]
    public float startHeight = 0f;

    [Header("六边形球配置")]
    [Tooltip("是否自动生成六边形球")]
    public bool spawnHexagonBall = true;

    [Tooltip("球相对于顶部的高度偏移")]
    public float ballHeightOffset = 1.5f;

    // 方块存储
    private List<GameObject> allBlocks = new List<GameObject>();
    private Dictionary<int, List<GameObject>> blocksByLayer = new Dictionary<int, List<GameObject>>();
    private GameObject hexagonBall;

    void Start()
    {
        // 订阅方块消除事件
        Block.OnBlockDestroyed += HandleBlockDestroyed;

        // 生成方块塔
        GenerateTower();

        // 生成六边形球
        if (spawnHexagonBall)
        {
            SpawnHexagonBall();
        }
    }

    void OnDestroy()
    {
        // 取消订阅
        Block.OnBlockDestroyed -= HandleBlockDestroyed;
    }

    /// <summary>
    /// 生成方块塔
    /// </summary>
    public void GenerateTower()
    {
        ClearTower();

        for (int layer = 0; layer < towerLayers; layer++)
        {
            GenerateLayer(layer);
        }

        Debug.Log($"生成方块塔完成: {towerLayers} 层, 共 {allBlocks.Count} 个方块");
    }

    /// <summary>
    /// 生成单层方块
    /// </summary>
    void GenerateLayer(int layerIndex)
    {
        // 每层的方块数量都相同（垂直塔形）
        int blocksInLayer = blocksPerLayer;

        // 计算层高度
        float y = startHeight + layerIndex * layerHeight;

        // 计算总宽度
        float totalWidth = blocksInLayer * blockSize + (blocksInLayer - 1) * blockSpacing;
        float startX = -totalWidth / 2f + blockSize / 2f;

        List<GameObject> layerBlocks = new List<GameObject>();

        for (int i = 0; i < blocksInLayer; i++)
        {
            float x = startX + i * (blockSize + blockSpacing);
            Vector2 position = new Vector2(x, y);

            // 创建方块
            GameObject block = BlockFactory.CreateBlock(position, transform);
            block.name = $"Block_L{layerIndex}_#{i}";

            // 根据层数设置不同颜色（循环使用颜色）
            Block blockScript = block.GetComponent<Block>();
            if (blockScript != null)
            {
                blockScript.SetColor(BlockFactory.GetBlockColor(layerIndex % 6));
            }

            allBlocks.Add(block);
            layerBlocks.Add(block);
        }

        blocksByLayer[layerIndex] = layerBlocks;
    }    /// <summary>
         /// 清空方块塔
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
        // 计算球的位置（在塔顶上方）
        float topY = startHeight + towerLayers * layerHeight;
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
        GameObject blockObj = destroyedBlock.gameObject;

        Debug.Log($"方块被消除: {blockObj.name}");

        // 从列表中移除
        allBlocks.Remove(blockObj);

        // 找出被消除方块的层级
        int destroyedLayer = GetBlockLayer(blockObj);

        // 激活上层所有方块的物理（让它们下落）
        ActivateBlocksAboveLayer(destroyedLayer);
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
    /// 获取当前剩余方块数量
    /// </summary>
    public int GetRemainingBlockCount()
    {
        return allBlocks.Count;
    }

    /// <summary>
    /// 获取六边形球对象
    /// </summary>
    public GameObject GetHexagonBall()
    {
        return hexagonBall;
    }

    /// <summary>
    /// 重置方块塔
    /// </summary>
    public void ResetTower()
    {
        ClearTower();

        if (hexagonBall != null)
        {
            Destroy(hexagonBall);
        }

        GenerateTower();

        if (spawnHexagonBall)
        {
            SpawnHexagonBall();
        }
    }

    void OnDrawGizmos()
    {
        // 在编辑器中绘制方块塔的预览
        if (!Application.isPlaying)
        {
            Gizmos.color = Color.cyan;

            for (int layer = 0; layer < towerLayers; layer++)
            {
                int blocksInLayer = blocksPerLayer;

                float y = startHeight + layer * layerHeight;
                float totalWidth = blocksInLayer * blockSize + (blocksInLayer - 1) * blockSpacing;
                float startX = -totalWidth / 2f;

                for (int i = 0; i < blocksInLayer; i++)
                {
                    float x = startX + i * (blockSize + blockSpacing) + blockSize / 2f;
                    Vector3 position = new Vector3(x, y, 0);
                    Gizmos.DrawWireCube(position, new Vector3(blockSize, blockSize, 0.1f));
                }
            }
        }
    }
}
