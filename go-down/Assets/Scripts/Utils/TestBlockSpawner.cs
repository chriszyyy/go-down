using UnityEngine;

/// <summary>
/// 测试方块生成器 - 用于快速测试方块功能
/// </summary>
public class TestBlockSpawner : MonoBehaviour
{
    [Header("方块测试设置")]
    [Tooltip("自动生成方块的数量")]
    public int blockCount = 5;

    [Tooltip("方块之间的间距")]
    public float spacing = 1.2f;

    [Tooltip("生成位置的 Y 坐标")]
    public float spawnY = 5f;

    [Header("六边形球测试")]
    [Tooltip("是否生成六边形球")]
    public bool spawnHexagonBall = true;

    [Tooltip("球的生成高度（相对于方块顶部）")]
    public float ballHeightOffset = 2f;

    void Start()
    {
        SpawnTestBlocks();

        if (spawnHexagonBall)
        {
            SpawnHexagonBall();
        }
    }

    /// <summary>
    /// 生成测试方块
    /// </summary>
    void SpawnTestBlocks()
    {
        // 计算起始位置，让方块居中
        float startX = -(blockCount - 1) * spacing / 2f;

        for (int i = 0; i < blockCount; i++)
        {
            float x = startX + i * spacing;
            Vector2 position = new Vector2(x, spawnY);

            // 使用 BlockFactory 创建方块
            GameObject block = BlockFactory.CreateBlock(position, transform);
            block.name = $"TestBlock_{i}";

            Debug.Log($"创建方块: {block.name} at position {position}");
        }
    }

    /// <summary>
    /// 生成六边形球
    /// </summary>
    void SpawnHexagonBall()
    {
        // 在方块上方生成球
        Vector2 ballPosition = new Vector2(0, spawnY + ballHeightOffset);
        GameObject ball = HexagonBallFactory.CreateHexagonBall(ballPosition, transform);

        Debug.Log($"创建六边形球 at position {ballPosition}");

        // 订阅球的事件
        HexagonBall hexBall = ball.GetComponent<HexagonBall>();
        if (hexBall != null)
        {
            HexagonBall.OnBallFell += HandleBallFell;
            HexagonBall.OnBallTilted += HandleBallTilted;
            HexagonBall.OnBallStable += HandleBallStable;
        }
    }

    void HandleBallFell()
    {
        Debug.Log("❌ 游戏结束 - 球掉落了！");
    }

    void HandleBallTilted(float angle)
    {
        // 只在角度较大时输出，避免刷屏
        if (Mathf.Abs(angle) > 20f)
        {
            Debug.LogWarning($"⚠️ 球倾斜角度: {angle:F1}°");
        }
    }

    void HandleBallStable()
    {
        Debug.Log("✅ 球已稳定");
    }

    void OnDestroy()
    {
        // 取消事件订阅
        HexagonBall.OnBallFell -= HandleBallFell;
        HexagonBall.OnBallTilted -= HandleBallTilted;
        HexagonBall.OnBallStable -= HandleBallStable;
    }
}
