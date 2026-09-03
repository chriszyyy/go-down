using UnityEngine;

/// <summary>
/// 无尽塔区块横向随机游走的纯计算逻辑。
/// </summary>
public static class TowerSegmentShiftMath
{
    public static int CalculateAllowedMaxOffset(
        int shiftedSegmentCount,
        int initialMaxOffset,
        int segmentsPerOffsetIncrease,
        int maxAbsoluteOffset)
    {
        int maximum = Mathf.Max(1, maxAbsoluteOffset);
        int initial = Mathf.Clamp(initialMaxOffset, 1, maximum);
        int increase = Mathf.Max(0, shiftedSegmentCount) / Mathf.Max(1, segmentsPerOffsetIncrease);
        return Mathf.Min(maximum, initial + increase);
    }

    public static float CalculateProgressiveChance(
        int segmentIndex,
        int startSegment,
        int fullChanceSegment,
        float maximumChance)
    {
        if (segmentIndex < Mathf.Max(0, startSegment)) return 0f;

        int start = Mathf.Max(0, startSegment);
        int full = Mathf.Max(start + 1, fullChanceSegment);
        float progress = Mathf.InverseLerp(start, full, segmentIndex);
        return Mathf.Clamp01(maximumChance) * progress;
    }

    public static void CalculateDirectionRunBounds(
        int shiftedSegmentCount,
        int minimum,
        int maximum,
        int growthStartSegment,
        int segmentsPerIncrease,
        int maxAdditionalLength,
        out int resolvedMinimum,
        out int resolvedMaximum)
    {
        int baseMinimum = Mathf.Max(1, minimum);
        int baseMaximum = Mathf.Max(baseMinimum, maximum);
        int depthPastStart = Mathf.Max(0, shiftedSegmentCount - Mathf.Max(0, growthStartSegment));
        int growth = shiftedSegmentCount < Mathf.Max(0, growthStartSegment)
            ? 0
            : 1 + depthPastStart / Mathf.Max(1, segmentsPerIncrease);
        growth = Mathf.Min(Mathf.Max(0, maxAdditionalLength), growth);
        resolvedMinimum = baseMinimum + growth;
        resolvedMaximum = baseMaximum + growth;
    }

    public static int ResolveDirectionAtLimit(int currentOffset, int direction, int allowedMaxOffset)
    {
        int normalizedDirection = direction < 0 ? -1 : 1;
        int limit = Mathf.Max(1, allowedMaxOffset);
        if (currentOffset >= limit && normalizedDirection > 0) return -1;
        if (currentOffset <= -limit && normalizedDirection < 0) return 1;
        return normalizedDirection;
    }

    public static int GetNextOffset(int currentOffset, int direction, int allowedMaxOffset)
    {
        int resolvedDirection;
        int appliedStep;
        return GetNextOffset(currentOffset, direction, 1, allowedMaxOffset, out resolvedDirection, out appliedStep);
    }

    public static int GetNextOffset(
        int currentOffset,
        int direction,
        int requestedStep,
        int allowedMaxOffset,
        out int resolvedDirection,
        out int appliedStep)
    {
        int limit = Mathf.Max(1, allowedMaxOffset);
        resolvedDirection = ResolveDirectionAtLimit(currentOffset, direction, limit);
        int room = resolvedDirection > 0 ? limit - currentOffset : currentOffset + limit;

        if (room <= 0)
        {
            resolvedDirection = -resolvedDirection;
            room = resolvedDirection > 0 ? limit - currentOffset : currentOffset + limit;
        }

        appliedStep = Mathf.Min(Mathf.Clamp(requestedStep, 1, 2), Mathf.Max(1, room));
        return currentOffset + resolvedDirection * appliedStep;
    }

    public static int[] GetBridgeColumns(int previousOffset, int nextOffset, int layerWidth)
    {
        if (previousOffset == nextOffset) return new int[0];

        int width = Mathf.Max(1, layerWidth);
        int minColumn = Mathf.Min(previousOffset, nextOffset);
        int maxColumn = Mathf.Max(previousOffset + width - 1, nextOffset + width - 1);
        int[] columns = new int[maxColumn - minColumn + 1];
        for (int i = 0; i < columns.Length; i++)
            columns[i] = minColumn + i;

        return columns;
    }

    public static int[] GetBridgeChunkWidths(int totalWidth, bool hasLine4, bool hasLine3)
    {
        var widths = new System.Collections.Generic.List<int>();
        int remaining = Mathf.Max(0, totalWidth);

        while (remaining > 0)
        {
            if (hasLine4 && remaining >= 4 && !(remaining == 6 && hasLine3))
            {
                widths.Add(4);
                remaining -= 4;
            }
            else if (hasLine3 && remaining >= 3)
            {
                widths.Add(3);
                remaining -= 3;
            }
            else
            {
                widths.Add(1);
                remaining--;
            }
        }

        return widths.ToArray();
    }
}


/// <summary>
/// 成批反弹块的深度概率、数量、排除规则与空间聚集选择。
/// </summary>
public static class TowerBouncyBlockRules
{
    public static float CalculateChance(
        int segmentIndex,
        int startSegment,
        int fullChanceSegment,
        float maximumChance)
    {
        return TowerSegmentShiftMath.CalculateProgressiveChance(
            segmentIndex, startSegment, fullChanceSegment, maximumChance);
    }

    public static int CalculateBatchSize(
        int eligibleCount,
        int minimumBatchSize,
        int maximumBatchSize,
        float randomValue)
    {
        int eligible = Mathf.Max(0, eligibleCount);
        if (eligible == 0) return 0;

        int minimum = Mathf.Clamp(minimumBatchSize, 1, eligible);
        int maximum = Mathf.Clamp(maximumBatchSize, minimum, eligible);
        int range = maximum - minimum + 1;
        int offset = Mathf.Min(range - 1, Mathf.FloorToInt(Mathf.Clamp01(randomValue) * range));
        return minimum + offset;
    }

    public static bool IsEligible(bool isStructuralSupport, bool isTrap, bool isRainbow)
    {
        return !isStructuralSupport && !isTrap && !isRainbow;
    }

    public static int[] SelectClusterIndices(int[] layerKeys, int requestedCount, int tieBreaker)
    {
        if (layerKeys == null || layerKeys.Length == 0 || requestedCount <= 0)
            return new int[0];

        int count = Mathf.Min(requestedCount, layerKeys.Length);
        int[] sorted = new int[layerKeys.Length];
        for (int i = 0; i < sorted.Length; i++) sorted[i] = i;
        System.Array.Sort(sorted, (a, b) =>
        {
            int layerComparison = layerKeys[a].CompareTo(layerKeys[b]);
            return layerComparison != 0 ? layerComparison : a.CompareTo(b);
        });

        int minimumSpan = int.MaxValue;
        var bestStarts = new System.Collections.Generic.List<int>();
        for (int start = 0; start <= sorted.Length - count; start++)
        {
            int span = layerKeys[sorted[start + count - 1]] - layerKeys[sorted[start]];
            if (span < minimumSpan)
            {
                minimumSpan = span;
                bestStarts.Clear();
            }
            if (span == minimumSpan) bestStarts.Add(start);
        }

        int selectedStart = bestStarts[Mathf.Abs(tieBreaker) % bestStarts.Count];
        int[] result = new int[count];
        System.Array.Copy(sorted, selectedStart, result, 0, count);
        return result;
    }
}


/// <summary>
/// 塔构建器 - 使用预制体拼装塔（基于网格的随机填充算法）
/// </summary>
public class TowerBuilder : MonoBehaviour
{
    [Header("特殊方块")]
    [Range(0f, 1f)]
    [Tooltip("生成彩色特殊方块的概率（每生成一个方块抽一次）。例如 0.02 = 2%")]
    [SerializeField] private float specialBlockChance = 0.02f;

    [Header("陷阱方块")]
    [Range(0f, 1f)]
    [Tooltip("每生成一批塔时，生成陷阱方块的概率。命中后随机选取 trapBlockCount 个方块设为陷阱")]
    [SerializeField] private float trapBlockChance = 0.5f;

    [Tooltip("概率命中时，本批随机生成的陷阱方块数量")]
    [SerializeField] private int trapBlockCount = 2;
    [Header("3D 立体方块")]
    [Tooltip("是否给方块附加 3D 立体视觉（BlockPrismVisual）。彩虹特殊方块除外，仍保持流动效果")]
    [SerializeField] private bool enable3DBlockVisual = true;
    [Header("塔配置")]
    [Tooltip("塔的层数")]
    public int towerLayers = 8;

    [Tooltip("每层的宽度（单位：方块）")]
    public int layerWidth = 8;

    [Tooltip("起始高度")]
    public float startHeight = -3f;

    [Header("无尽塔配置")]
    [Tooltip("每次向下续接生成的段高度（单位：层，1层=1单位）")]
    public int segmentHeightLayers = 40;

    [Tooltip("当摄像机距离当前已生成底部小于该值时，提前生成下一段（单位：层/单位）")]
    public int generateAheadLayers = 20;

    [Tooltip("底部永远保持Kinematic的地基层厚度（单位：层/单位）")]
    public int foundationThicknessLayers = 5;

    [Tooltip("当方块高于摄像机太多时销毁（单位：层/单位），用于控制对象数量")]
    public int destroyAboveCameraLayers = 80;

    [Header("塔段横向随机游走")]
    [Tooltip("是否让续接塔段以单格步长左右随机游走。缺少单格预制体时自动保持原位")]
    [SerializeField] private bool enableSegmentHorizontalWalk = true;

    [Tooltip("开局后保持居中的续接段数量，给玩家留出适应期")]
    [Min(0)]
    [SerializeField] private int segmentsBeforeHorizontalWalk = 1;

    [Tooltip("每轮连续朝同一方向移动的最少塔段数")]
    [Min(1)]
    [SerializeField] private int minSegmentsPerDirectionRun = 2;

    [Tooltip("每轮连续朝同一方向移动的最多塔段数；每轮会在最少与最多之间随机取值")]
    [Min(1)]
    [SerializeField] private int maxSegmentsPerDirectionRun = 4;

    [Tooltip("随机游走初期允许的最大绝对横向偏移（格）")]
    [Min(1)]
    [SerializeField] private int initialMaxAbsoluteOffset = 2;

    [Tooltip("每生成多少个游走塔段，将允许的最大绝对偏移增加一格")]
    [Min(1)]
    [SerializeField] private int segmentsPerOffsetIncrease = 4;

    [Tooltip("塔段相对初始中心允许达到的最大绝对横向偏移（格）；到达后强制反向")]
    [Min(1)]
    [SerializeField] private int maxAbsoluteHorizontalOffset = 3;

    [Tooltip("新区段顶部沿用上一区段位置的连接层数，用于承托单格错位接缝")]
    [Min(1)]
    [SerializeField] private int segmentConnectionLayers = 2;


    [Tooltip("从第几个续接区段开始允许出现单次 2 格横移；此前始终为 1 格")]
    [Min(0)]
    [SerializeField] private int doubleStepStartSegment = 4;

    [Tooltip("到达该续接区段时，2 格横移概率增长到上限")]
    [Min(1)]
    [SerializeField] private int doubleStepFullChanceSegment = 14;

    [Tooltip("中后期单次 2 格横移的最高概率")]
    [Range(0f, 1f)]
    [SerializeField] private float maxDoubleStepChance = 0.18f;

    [Tooltip("从第几个游走区段开始增长同方向轮次长度")]
    [Min(0)]
    [SerializeField] private int directionRunGrowthStartSegment = 4;

    [Tooltip("轮次增长开始后，每经过多少个游走区段增加 1 段同向长度")]
    [Min(1)]
    [SerializeField] private int segmentsPerDirectionRunIncrease = 4;

    [Tooltip("同方向轮次相对初始范围最多额外增加的段数")]
    [Min(0)]
    [SerializeField] private int maxAdditionalDirectionRunLength = 3;

    [Header("成批反弹块")]
    [Tooltip("用于反弹块 Collider2D 的专用弹性物理材质")]
    [SerializeField] private PhysicsMaterial2D bouncyBlockPhysicsMaterial;

    [Tooltip("从第几个续接区段开始允许出现反弹批次；此前概率为 0")]
    [Min(0)]
    [SerializeField] private int bouncyBatchStartSegment = 3;

    [Tooltip("到达该续接区段时，反弹批次概率增长到上限")]
    [Min(1)]
    [SerializeField] private int bouncyBatchFullChanceSegment = 14;

    [Tooltip("后期每个区段出现一个反弹批次的最高概率")]
    [Range(0f, 1f)]
    [SerializeField] private float maxBouncyBatchChance = 0.28f;

    [Tooltip("概率命中时，一个反弹批次的最少方块数；候选不足时使用全部候选")]
    [Min(1)]
    [SerializeField] private int minBouncyBlocksPerBatch = 4;

    [Tooltip("概率命中时，一个反弹批次的最多方块数")]
    [Min(1)]
    [SerializeField] private int maxBouncyBlocksPerBatch = 7;

    [Header("新段稳定化")]
    [Tooltip("生成新段后，先冻结一小段时间以让接缝稳定，再按相机窗口激活")]
    public bool stabilizeNewSegment = true;

    [Tooltip("新段稳定化时长（秒）")]
    public float stabilizeDuration = 0.25f;

    [Header("初始激活")]
    [Tooltip("是否需要点击按钮后才开始激活/解冻逻辑")]
    public bool requireManualStartActivation = false;

    [Tooltip("开局延迟激活（秒），避免生成后立即解冻导致爆炸")]
    public float initialActivationDelay = 0.2f;

    [Tooltip("每次激活最多解冻多少个方块（分批解冻，降低约束爆炸风险）")]
    public int activationBatchSize = 24;

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

    [Tooltip("I型（3格）预制体")]
    public GameObject line3BlockPrefab;

    [Tooltip("L2型（3格L）预制体")]
    public GameObject l2BlockPrefab;

    [Header("失败边界预制体")]
    [Tooltip("左边界触发器预制体（可选）。用于球碰到左右边界判定失败")]
    public GameObject leftBoundaryPrefab;

    [Tooltip("右边界触发器预制体（可选）。用于球碰到左右边界判定失败")]
    public GameObject rightBoundaryPrefab;

    [Tooltip("左右边界各自向外额外留出的格子数（单位=格/世界单位）。总宽度 = layerWidth + 2*extra")]
    public float boundaryExtraCellsEachSide = 2f;

    [Tooltip("边界是否跟随主摄像机的Y位置（避免球从边界下方绕开）")]
    public bool boundaryFollowCameraY = true;

    [Tooltip("边界跟随摄像机时的Y偏移")]
    public float boundaryFollowYOffset = 0f;

    [Tooltip("塔段换道时左右边界横向跟随的平滑时间（秒）")]
    [Min(0.01f)]
    public float boundaryHorizontalSmoothTime = 0.35f;

    [Tooltip("左右边界横向跟随的最大速度（单位/秒）")]
    [Min(0.1f)]
    public float maxBoundaryHorizontalSpeed = 5f;

    [Header("六边形球配置")]
    [Tooltip("六边形球预制体")]
    public GameObject hexagonBallPrefab;

    [Tooltip("是否自动生成六边形球")]
    public bool spawnHexagonBall = true;

    [Tooltip("球相对于顶部的高度偏移")]
    public float ballHeightOffset = 1.5f;

    [Tooltip("六边形球缩放倍率（在预制体原始缩放基础上相乘）。1=不变，建议 1.1~1.25 之间微调。")]
    public float hexagonBallScaleMultiplier = 1.15f;

    [Header("分层激活配置")]
    [Tooltip("额外向下激活的缓冲距离（单位）")]
    public float activationExtraBelow = 2f;

    [Tooltip("额外向上激活的缓冲距离（单位）")]
    public float activationExtraAbove = 1f;

    [Tooltip("激活检测频率（秒）")]
    public float activationCheckInterval = 0.5f;

    [Header("塔尖更新")]
    [Tooltip("塔尖高度定时更新频率（秒）")]
    public float topYUpdateInterval = 0.6f;

    // 运行时数据
    private GameObject hexagonBall;
    private GameObject leftBoundary;
    private GameObject rightBoundary;
    private float leftBoundaryXVelocity;
    private float rightBoundaryXVelocity;

    // 网格占用矩阵（仅用于初始段的放置计算）[层][列] = 是否被占用
    private bool[,] gridOccupied;

    // 当前塔的最高层
    private float currentTowerTopY;

    // 运行时：当前已生成的底部Y（更小=更靠下）
    private float currentGeneratedMinY;

    // 运行时：地基分界线（<=该高度永远冻结为Kinematic）
    private float foundationY;

    // 运行时：避免重复触发生成
    private float lastGeneratedMinY;
    private float lastSegmentCheckLogTime;

    private float stabilizeUntilTime;

    // 分层激活
    private float lastActivationCheckTime;
    private float lastTopYUpdateTime;
    private Camera mainCamera;

    private bool initialActivationPending;

    private bool activationEnabled;

    private int generatedSegmentCount;
    private int shiftedSegmentCount;
    private int currentSegmentXOffset;
    private int currentWalkDirection;
    private int segmentsRemainingInDirectionRun;

    private struct SegmentOffsetZone
    {
        public float minY;
        public float maxY;
        public int offset;
    }

    private readonly System.Collections.Generic.List<SegmentOffsetZone> segmentOffsetZones =
        new System.Collections.Generic.List<SegmentOffsetZone>();
    private readonly System.Collections.Generic.HashSet<int> bouncyBatchGeneratedSegments =
        new System.Collections.Generic.HashSet<int>();

    void Start()
    {
        // 订阅方块消除事件
        TowerBlock.OnBlockDestroyed += HandleBlockDestroyed;

        // 获取主摄像机
        mainCamera = Camera.main;

        // 构建初始塔
        BuildTower();

        // 更新边界
        UpdateTowerTopY();
        UpdateGeneratedMinY();
        UpdateFoundationY();

        // 初始段也做一次稳定化：先冻结、延迟激活，避免开局瞬间爆炸
        if (stabilizeNewSegment)
        {
            FreezeBlocksInYRange(startHeight - 1f, startHeight + towerLayers + 2f);
            stabilizeUntilTime = Time.time + Mathf.Max(0.01f, Mathf.Max(initialActivationDelay, stabilizeDuration));
        }
        else
        {
            stabilizeUntilTime = Time.time + Mathf.Max(0.01f, initialActivationDelay);
        }

        activationEnabled = !requireManualStartActivation;
        initialActivationPending = activationEnabled;

        // 立即将摄像机移动到塔顶位置，避免初始激活错误的方块
        if (mainCamera != null)
        {
            float centerX = layerWidth / 2f;
            mainCamera.transform.position = new Vector3(centerX, currentTowerTopY - 3f, mainCamera.transform.position.z);
        }

        // 注意：激活会在 Update 中延迟进行（initialActivationDelay）

        // 生成六边形球
        if (spawnHexagonBall && hexagonBallPrefab != null)
        {
            SpawnHexagonBall();
        }
    }

    void Update()
    {
        // 边界应始终跟随相机（即使尚未开始激活逻辑），避免从下方绕过。
        UpdateBoundariesFollow();

        if (!activationEnabled)
        {
            return;
        }

        // 接近底部时续接生成下一段
        TryGenerateNextSegment();

        // 清理远离相机的上方方块（性能）
        CleanupBlocksAboveCamera();

        // 定时更新塔尖高度，确保相机跟随在物理下落时也能刷新
        if (topYUpdateInterval > 0f && Time.time - lastTopYUpdateTime >= topYUpdateInterval)
        {
            lastTopYUpdateTime = Time.time;
            UpdateTowerTopY();
        }

        // 定期检查并激活进入范围的方块
        if (Time.time < stabilizeUntilTime)
        {
            // 稳定化期间保持冻结，避免接缝瞬间爆炸
            return;
        }

        if (initialActivationPending)
        {
            initialActivationPending = false;
            BeginInitialActivation();
        }

        if (Time.time - lastActivationCheckTime >= activationCheckInterval)
        {
            lastActivationCheckTime = Time.time;
            ActivateBlocksInRange();
        }
    }

    void BeginInitialActivation()
    {
        // 初始激活前：把“方块最低占用格的左下角”对齐到整数格。
        // 关键点：一些形状的 transform.position 并不是形状左下角，直接 Round(position) 会造成整体偏移与挤压。
        TowerBlock[] snapBlocks = GetComponentsInChildren<TowerBlock>();
        for (int i = 0; i < snapBlocks.Length; i++)
        {
            TowerBlock b = snapBlocks[i];
            if (b == null) continue;

            float rotZ = b.transform.eulerAngles.z;
            Vector2Int bottomLeft = b.GetBottomLeftCorner(rotZ);

            Vector3 p = b.transform.position;

            // PlaceBlock 规则：worldX = pivotCol - bottomLeft.x
            // 反推 pivotCol = worldX + bottomLeft.x，并保证 pivotCol 落在整数格
            float pivotX = p.x + bottomLeft.x;
            float pivotY = p.y + bottomLeft.y;

            float snappedPivotX = Mathf.Round(pivotX);
            float snappedPivotY = Mathf.Round(pivotY);

            float snappedWorldX = snappedPivotX - bottomLeft.x;
            float snappedWorldY = snappedPivotY - bottomLeft.y;

            if (!Mathf.Approximately(p.x, snappedWorldX) || !Mathf.Approximately(p.y, snappedWorldY))
            {
                b.transform.position = new Vector3(snappedWorldX, snappedWorldY, p.z);
            }
        }

        Physics2D.SyncTransforms();
        // 立刻触发一次激活（仍受 activationBatchSize 约束）
        ActivateBlocksInRange();
        lastActivationCheckTime = Time.time;
    }

    public void StartActivation()
    {
        activationEnabled = true;
        initialActivationPending = true;

        if (stabilizeNewSegment)
        {
            stabilizeUntilTime = Time.time + Mathf.Max(0.01f, Mathf.Max(initialActivationDelay, stabilizeDuration));
        }
        else
        {
            stabilizeUntilTime = Time.time + Mathf.Max(0.01f, initialActivationDelay);
        }
    }

    public float GetCurrentGeneratedMinY()
    {
        return currentGeneratedMinY;
    }

    public int GetCurrentSegmentXOffset()
    {
        return currentSegmentXOffset;
    }

    private void RegisterSegmentOffsetZone(float minY, float maxY, int offset)
    {
        segmentOffsetZones.Add(new SegmentOffsetZone
        {
            minY = minY,
            maxY = maxY,
            offset = offset
        });
    }

    public int GetSegmentXOffsetAtY(float worldY)
    {
        for (int i = segmentOffsetZones.Count - 1; i >= 0; i--)
        {
            SegmentOffsetZone zone = segmentOffsetZones[i];
            if (worldY >= zone.minY && worldY < zone.maxY)
                return zone.offset;
        }

        return worldY < currentGeneratedMinY ? currentSegmentXOffset : 0;
    }

    public float GetTowerCenterXAtY(float worldY)
    {
        return layerWidth / 2f + GetSegmentXOffsetAtY(worldY);
    }


    void OnDestroy()
    {
        TowerBlock.OnBlockDestroyed -= HandleBlockDestroyed;
    }

    /// <summary>
    /// 构建塔
    /// </summary>
    public void BuildTower()
    {
        ClearTower();

        generatedSegmentCount = 0;
        shiftedSegmentCount = 0;
        currentSegmentXOffset = 0;
        currentWalkDirection = 0;
        segmentsRemainingInDirectionRun = 0;
        segmentOffsetZones.Clear();
        bouncyBatchGeneratedSegments.Clear();
        RegisterSegmentOffsetZone(startHeight, float.PositiveInfinity, 0);

        EnsureBoundaries();

        // 初始化网格占用矩阵（仅用于初始段的放置计算）
        gridOccupied = new bool[towerLayers, layerWidth];

        // 生成初始段：从 startHeight 开始向上 towerLayers 层
        for (int layer = 0; layer < towerLayers; layer++)
        {
            FillLayerWithGrid(layer, startHeight, gridOccupied, towerLayers);
        }

        TrySpawnTrapInBatch(startHeight, startHeight + towerLayers);
        TrySpawnBouncyBatch(startHeight, startHeight + towerLayers, 0);


        currentGeneratedMinY = startHeight;
        lastGeneratedMinY = currentGeneratedMinY;
        UpdateTowerTopY();
    }

    /// <summary>
    /// 基于网格填充一层（随机放置算法）
    /// </summary>
    void FillLayerWithGrid(int layerIndex, float baseY, bool[,] occupied, int layerLimit, int worldXOffset = 0)
    {
        // Debug.Log($"=== 第 {layerIndex} 层开始填充 ===");

        int currentCol = 0;
        int attempts = 0;
        int maxAttempts = 100; // 防止无限循环
        int blockCount = 0;

        while (currentCol < layerWidth && attempts < maxAttempts)
        {
            attempts++;

            // 尝试在当前列放置方块
            GameObject placedBlock = TryPlaceBlockAt(
                layerIndex, currentCol, baseY, occupied, layerLimit, worldXOffset);

            if (placedBlock != null)
            {
                blockCount++;
                // Debug.Log($"  成功放置方块 at 列{currentCol}");
                // 移动到下一个未占用的列
                currentCol = FindNextEmptyColumn(layerIndex, currentCol, occupied);
            }
            else
            {
                // 当前列放不下，尝试下一列
                currentCol++;
            }

            // 如果找不到空列，结束
            if (currentCol >= layerWidth)
            {
                // Debug.Log("  已填满或无法继续放置");
                break;
            }
        }

        // Debug.Log($"=== 第 {layerIndex} 层完成，共 {blockCount} 个方块 ===\n");
    }

    /// <summary>
    /// 找到当前列之后的第一个空列
    /// </summary>
    int FindNextEmptyColumn(int layer, int startCol, bool[,] occupied)
    {
        for (int col = startCol; col < layerWidth; col++)
        {
            if (occupied == null || !occupied[layer, col])
            {
                return col;
            }
        }
        return layerWidth; // 没有空列
    }

    /// <summary>
    /// 尝试在指定位置放置方块
    /// </summary>
    GameObject TryPlaceBlockAt(
        int layer,
        int col,
        float baseY,
        bool[,] occupied,
        int layerLimit,
        int worldXOffset)
    {
        // 获取所有可用的prefab
        var availablePrefabs = GetAllAvailablePrefabs();
        if (availablePrefabs.Count == 0) return null;

        // 随机打乱prefab顺序
        ShuffleList(availablePrefabs);

        // 尝试所有旋转角度（随机顺序）
        var rotations = new System.Collections.Generic.List<float> { 0f, 90f, 180f, 270f };

        // 尝试每个prefab
        foreach (var prefab in availablePrefabs)
        {
            ShuffleList(rotations);

            foreach (float rotation in rotations)
            {
                // 检查是否能放置
                if (CanPlaceBlock(prefab, rotation, layer, col, occupied, layerLimit))
                {
                    // 放置方块
                    GameObject block = PlaceBlock(
                        prefab, rotation, layer, col, baseY, occupied, layerLimit, worldXOffset);
                    return block;
                }
            }
        }

        return null; // 无法放置
    }

    /// <summary>
    /// 检查方块是否可以放置在指定位置
    /// </summary>
    bool CanPlaceBlock(GameObject prefab, float rotation, int layer, int col, bool[,] occupied, int layerLimit)
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
            if (checkCol < 0 || checkCol >= layerWidth || checkLayer < 0 || checkLayer >= layerLimit)
            {
                // Debug.Log($"    边界检查失败: 格子({checkCol},{checkLayer}) 超出范围 [0-{layerWidth - 1}, 0-{towerLayers - 1}]");
                return false;
            }

            // 检查占用状态（段内占用）
            if (occupied != null && occupied[checkLayer, checkCol])
            {
                // Debug.Log($"    占用检查失败: 格子({checkCol},{checkLayer}) 已被占用");
                return false;
            }
        }

        // Debug.Log($"    可以放置: pivot({col},{layer}), 占用格子: {string.Join(", ", occupiedCells)}");
        return true;
    }

    /// <summary>
    /// 放置方块（pivot点直接使用col,layer坐标，并标记占用）
    /// </summary>
    GameObject PlaceBlock(
        GameObject prefab,
        float rotation,
        int layer,
        int col,
        float baseY,
        bool[,] occupied,
        int layerLimit,
        int worldXOffset)
    {
        TowerBlock blockComponent = prefab.GetComponent<TowerBlock>();

        // 取出对应pivot点，放置在网格位置(col, layer)
        Vector2Int bottomLeftCorner = blockComponent.GetBottomLeftCorner(rotation);
        float worldX = worldXOffset + col - bottomLeftCorner.x;
        float worldY = baseY + layer - bottomLeftCorner.y;

        Vector3 position = new Vector3(worldX, worldY, 0);

        // 获取占用格子
        var occupiedCells = blockComponent.GetOccupiedCells(rotation);

        // DEBUG: 坐标信息（需要时再打开，避免刷屏影响性能/时序）
        // Debug.Log($"  放置 {blockComponent.blockTypeName} pivotWorld=({worldX:F2},{worldY:F2}) rot={rotation}°");

        // 创建方块（应用旋转）
        Quaternion blockRotation = Quaternion.Euler(0, 0, rotation);
        GameObject block = Instantiate(prefab, position, blockRotation, transform);
        block.name = $"Block_L{layer}_C{col}_{blockComponent.blockTypeName}_R{rotation}";

        TryApplySpecialBlockVisual(block);
        TryApply3DBlockVisual(block);

        // 标记段内占用
        if (occupied != null)
        {
            foreach (var (dx, dy) in occupiedCells)
            {
                int occupyCol = col + dx;
                int occupyLayer = layer + dy;
                if (occupyLayer >= 0 && occupyLayer < layerLimit && occupyCol >= 0 && occupyCol < layerWidth)
                    occupied[occupyLayer, occupyCol] = true;
            }
        }

        return block;
    }

    void TryApplySpecialBlockVisual(GameObject block)
    {
        if (block == null) return;

        if (UnityEngine.Random.value >= specialBlockChance) return;

        // Attach animated rainbow gradient + glow (without a compile-time dependency on Visuals asmdef).
        if (block.GetComponent("RainbowGlowVisual") == null)
        {
            var t = System.Type.GetType("RainbowGlowVisual, GoDown.Visuals");
            if (t != null)
            {
                var comp = block.AddComponent(t);

                // Keep the normal inset highlight sprite enabled as a light outline.
                // (RainbowGlowVisual disables it by default; we want it on for special blocks.)
                var field = t.GetField("disableInsetHighlight");
                if (field != null)
                {
                    field.SetValue(comp, false);
                }
            }
            else
            {
                Debug.LogWarning("TowerBuilder: 未找到 RainbowGlowVisual 类型（GoDown.Visuals）。将跳过特殊方块发光效果。");
            }
        }

        // Use the same palette as normal blocks for the inset highlight (outline), but keep the rainbow fill.
        // We generate a deterministic palette color (ApplyRandomStyle) then lock it, so special blocks remain stable.
        Component style = block.GetComponent("BlockVisualStyle");
        if (style != null)
        {
            style.SendMessage("ApplyRandomStyle", SendMessageOptions.DontRequireReceiver);

            SpriteRenderer sr = block.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                style.SendMessage("ApplyStyleAndLock", sr.color, SendMessageOptions.DontRequireReceiver);
            }
        }

        // Special block: 10x score when destroyed.
        TowerBlock tb = block.GetComponent<TowerBlock>();
        if (tb != null)
        {
            tb.scoreMultiplier = 10;
        }

        // Special block: award coins when HexagonBall collides (once per block).
        RainbowCoinReward coinReward = block.GetComponent<RainbowCoinReward>();
        if (coinReward == null)
        {
            coinReward = block.AddComponent<RainbowCoinReward>();
        }

        coinReward.coinsPerActivation = 5;
        coinReward.hexagonBallTag = "HexagonBall";
    }

    /// <summary>
    /// 给方块附加 3D 立体视觉。彩虹特殊方块（已带 RainbowGlowVisual）跳过，保持流动效果（方案 A）。
    /// </summary>
    void TryApply3DBlockVisual(GameObject block)
    {
        if (!enable3DBlockVisual || block == null) return;

        // 彩虹方块保持原 flat 流动效果，不做 3D
        if (block.GetComponent("RainbowGlowVisual") != null) return;

        if (block.GetComponent<BlockPrismVisual>() == null)
        {
            block.AddComponent<BlockPrismVisual>();
        }
    }

    /// <summary>
    /// 在刚生成的一批塔中按概率植入一个陷阱方块：
    /// 命中后，在该批 [batchMinY, batchMaxY] 范围内的所有方块中（彩虹特殊方块除外）随机选一个设为陷阱。
    /// </summary>
    void TrySpawnTrapInBatch(float batchMinY, float batchMaxY)
    {
        if (UnityEngine.Random.value >= trapBlockChance) return;

        var candidates = new System.Collections.Generic.List<TowerBlock>();
        foreach (Transform child in transform)
        {
            if (child == null) continue;
            if (child.gameObject == hexagonBall) continue;

            float y = child.position.y;
            if (y < batchMinY - 0.5f || y > batchMaxY + 0.5f) continue;

            TowerBlock tb = child.GetComponent<TowerBlock>();
            if (tb == null || tb.IsStructuralSupport) continue;

            // 排除彩虹特殊方块（带 RainbowGlowVisual 的不做陷阱）
            if (child.GetComponent("RainbowGlowVisual") != null) continue;

            // 已是陷阱则跳过
            if (tb.GetComponent<TrapBlock>() != null) continue;

            candidates.Add(tb);
        }

        if (candidates.Count == 0) return;

        // 随机选取 trapBlockCount 个不重复的方块设为陷阱
        int want = Mathf.Clamp(trapBlockCount, 1, candidates.Count);
        for (int i = 0; i < want; i++)
        {
            int idx = UnityEngine.Random.Range(0, candidates.Count);
            TowerBlock chosen = candidates[idx];
            candidates.RemoveAt(idx);
            ApplyTrapBlock(chosen);
        }
    }

    /// <summary>
    /// 把指定方块设为陷阱：剥离可能的彩色特殊效果、附加 TrapBlock（陷阱外观 + 连带消除 + 隐身）。
    /// </summary>
    void ApplyTrapBlock(TowerBlock tb)
    {
        if (tb == null) return;
        GameObject block = tb.gameObject;

        // 若该格恰好被选为彩色特殊块，剥离其特殊效果，避免与陷阱叠加
        var rainbow = block.GetComponent("RainbowGlowVisual");
        if (rainbow != null) Destroy(rainbow);
        var coinReward = block.GetComponent<RainbowCoinReward>();
        if (coinReward != null) Destroy(coinReward);
        tb.scoreMultiplier = 1;

        // 陷阱深色 + 隐身后要伪装成的“正常方块”颜色（从普通调色板随机取）
        Color trapColor = new Color(0.07f, 0.07f, 0.11f, 1f);
        Color normalTarget = s_normalBlockPalette[UnityEngine.Random.Range(0, s_normalBlockPalette.Length)];

        // 锁定 BlockVisualStyle，避免它在 Start 时再随机上色覆盖陷阱色
        Component style = block.GetComponent("BlockVisualStyle");
        if (style != null)
        {
            style.SendMessage("ApplyStyleAndLock", trapColor, SendMessageOptions.DontRequireReceiver);
        }
        tb.OverrideOriginalColor(trapColor);

        var trap = block.GetComponent<TrapBlock>();
        if (trap == null) trap = block.AddComponent<TrapBlock>();
        trap.Configure(trapColor, normalTarget);
    }

    /// <summary>
    /// 每个区段只抽取一次批次概率；命中后将 Y 层跨度最小的一组普通方块配置为反弹块。
    /// 陷阱先配置，反弹块再筛选，确保两种属性最终不重叠。
    /// </summary>
    void TrySpawnBouncyBatch(float batchMinY, float batchMaxY, int segmentIndex)
    {
        if (bouncyBlockPhysicsMaterial == null || bouncyBatchGeneratedSegments.Contains(segmentIndex)) return;

        float chance = TowerBouncyBlockRules.CalculateChance(
            segmentIndex,
            bouncyBatchStartSegment,
            bouncyBatchFullChanceSegment,
            maxBouncyBatchChance);
        if (chance <= 0f || UnityEngine.Random.value >= chance) return;
        bouncyBatchGeneratedSegments.Add(segmentIndex);

        var candidates = new System.Collections.Generic.List<TowerBlock>();
        var layerKeys = new System.Collections.Generic.List<int>();
        foreach (Transform child in transform)
        {
            if (child == null || child.gameObject == hexagonBall) continue;

            float y = child.position.y;
            if (y < batchMinY - 0.5f || y > batchMaxY + 0.5f) continue;

            TowerBlock block = child.GetComponent<TowerBlock>();
            if (block == null || !IsBouncyEligible(block)) continue;

            candidates.Add(block);
            layerKeys.Add(Mathf.RoundToInt(y));
        }

        int batchSize = TowerBouncyBlockRules.CalculateBatchSize(
            candidates.Count,
            minBouncyBlocksPerBatch,
            maxBouncyBlocksPerBatch,
            UnityEngine.Random.value);
        if (batchSize <= 0) return;

        int[] selected = TowerBouncyBlockRules.SelectClusterIndices(
            layerKeys.ToArray(),
            batchSize,
            UnityEngine.Random.Range(0, int.MaxValue));
        int minLayer = int.MaxValue;
        int maxLayer = int.MinValue;
        int invalidOverlapCount = 0;
        for (int i = 0; i < selected.Length; i++)
        {
            TowerBlock block = candidates[selected[i]];
            ApplyBouncyBlock(block);
            int layer = layerKeys[selected[i]];
            minLayer = Mathf.Min(minLayer, layer);
            maxLayer = Mathf.Max(maxLayer, layer);
            if (!IsBouncyEligible(block)) invalidOverlapCount++;
        }

        Debug.Log($"[BouncyBatch] segment={segmentIndex} count={selected.Length} ySpan={maxLayer - minLayer} invalidOverlap={invalidOverlapCount}");
    }

    private static bool IsBouncyEligible(TowerBlock block)
    {
        if (block == null) return false;
        bool isTrap = block.GetComponent<TrapBlock>() != null;
        bool isRainbow = block.gameObject.GetComponent("RainbowGlowVisual") != null;
        return TowerBouncyBlockRules.IsEligible(block.IsStructuralSupport, isTrap, isRainbow);
    }

    void ApplyBouncyBlock(TowerBlock block)
    {
        if (bouncyBlockPhysicsMaterial == null || !IsBouncyEligible(block)) return;

        Collider2D blockCollider = block.GetComponent<Collider2D>();
        if (blockCollider == null) return;

        blockCollider.sharedMaterial = bouncyBlockPhysicsMaterial;
        block.blockTypeName = "Bouncy Block";
        block.gameObject.name = "Bouncy_" + block.gameObject.name;

        Color bouncyColor = new Color(0.45f, 1f, 0.08f, 1f);
        Component style = block.GetComponent("BlockVisualStyle");
        if (style != null)
            style.SendMessage("ApplyStyleAndLock", bouncyColor, SendMessageOptions.DontRequireReceiver);
        block.OverrideOriginalColor(bouncyColor);
    }

    // 普通方块调色板（与 BlockVisualStyle 默认 5 色一致），陷阱隐身时随机伪装成其中之一
    private static readonly Color[] s_normalBlockPalette = new[]
    {
        new Color(0.012f, 0.388f, 0.941f, 1f),
        new Color(0.063f, 0.698f, 0.349f, 1f),
        new Color(0.498f, 0.169f, 0.945f, 1f),
        new Color(0.992f, 0.706f, 0.051f, 1f),
        new Color(0.933f, 0.169f, 0.396f, 1f),
    };

    void TryGenerateNextSegment()
    {
        if (mainCamera == null) return;

        float cameraY = mainCamera.transform.position.y;
        float triggerY = currentGeneratedMinY + generateAheadLayers;

        if (Time.time - lastSegmentCheckLogTime >= 1.0f)
        {
            float distanceToTrigger = cameraY - triggerY;
            if (distanceToTrigger <= Mathf.Max(2f, segmentHeightLayers * 0.25f))
                lastSegmentCheckLogTime = Time.time;
        }

        if (cameraY > triggerY) return;
        if (!Mathf.Approximately(lastGeneratedMinY, currentGeneratedMinY)) return;

        int previousOffset = currentSegmentXOffset;
        int nextOffset = previousOffset;
        bool canWalk = enableSegmentHorizontalWalk
            && singleBlockPrefab != null
            && generatedSegmentCount >= Mathf.Max(0, segmentsBeforeHorizontalWalk);

        if (canWalk)
        {
            int allowedMaxOffset = TowerSegmentShiftMath.CalculateAllowedMaxOffset(
                shiftedSegmentCount,
                initialMaxAbsoluteOffset,
                segmentsPerOffsetIncrease,
                maxAbsoluteHorizontalOffset);

            if (currentWalkDirection == 0)
            {
                currentWalkDirection = UnityEngine.Random.value < 0.5f ? -1 : 1;
                StartNewDirectionRun();
            }

            int resolvedDirection = TowerSegmentShiftMath.ResolveDirectionAtLimit(
                previousOffset, currentWalkDirection, allowedMaxOffset);
            bool reachedLimit = resolvedDirection != currentWalkDirection;
            if (reachedLimit)
            {
                currentWalkDirection = resolvedDirection;
                StartNewDirectionRun();
            }
            else if (segmentsRemainingInDirectionRun <= 0)
            {
                currentWalkDirection = -currentWalkDirection;
                currentWalkDirection = TowerSegmentShiftMath.ResolveDirectionAtLimit(
                    previousOffset, currentWalkDirection, allowedMaxOffset);
                StartNewDirectionRun();
            }

            float doubleStepChance = TowerSegmentShiftMath.CalculateProgressiveChance(
                shiftedSegmentCount,
                doubleStepStartSegment,
                doubleStepFullChanceSegment,
                maxDoubleStepChance);
            int requestedStep = UnityEngine.Random.value < doubleStepChance ? 2 : 1;
            int appliedStep;
            nextOffset = TowerSegmentShiftMath.GetNextOffset(
                previousOffset,
                currentWalkDirection,
                requestedStep,
                allowedMaxOffset,
                out currentWalkDirection,
                out appliedStep);
            segmentsRemainingInDirectionRun--;
            shiftedSegmentCount++;
        }

        float newSegmentStartY = currentGeneratedMinY - segmentHeightLayers;
        BuildTowerSegment(
            newSegmentStartY,
            segmentHeightLayers,
            previousOffset,
            nextOffset,
            generatedSegmentCount + 1);

        currentSegmentXOffset = nextOffset;
        generatedSegmentCount++;
        currentGeneratedMinY = newSegmentStartY;
        lastGeneratedMinY = currentGeneratedMinY;
        UpdateFoundationY();

        Debug.Log($"[TowerShift] segment={generatedSegmentCount} offset={previousOffset}->{nextOffset} step={Mathf.Abs(nextOffset - previousOffset)} direction={currentWalkDirection} remaining={segmentsRemainingInDirectionRun}");

        if (stabilizeNewSegment)
        {
            FreezeBlocksInYRange(newSegmentStartY - 1f, newSegmentStartY + segmentHeightLayers + 2f);
            stabilizeUntilTime = Time.time + Mathf.Max(0.01f, stabilizeDuration);
        }
    }

    private void StartNewDirectionRun()
    {
        int minimum;
        int maximum;
        TowerSegmentShiftMath.CalculateDirectionRunBounds(
            shiftedSegmentCount,
            minSegmentsPerDirectionRun,
            maxSegmentsPerDirectionRun,
            directionRunGrowthStartSegment,
            segmentsPerDirectionRunIncrease,
            maxAdditionalDirectionRunLength,
            out minimum,
            out maximum);
        segmentsRemainingInDirectionRun = UnityEngine.Random.Range(minimum, maximum + 1);
    }


    void FreezeBlocksInYRange(float minY, float maxY)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            float y = child.position.y;
            if (y < minY || y > maxY) continue;

            TowerBlock block = child.GetComponent<TowerBlock>();
            if (block != null) block.Freeze();
        }
    }

    void BuildTowerSegment(
        float segmentStartY,
        int heightLayers,
        int previousOffset,
        int nextOffset,
        int segmentIndex)
    {
        if (heightLayers <= 0) return;

        bool needsBridge = previousOffset != nextOffset
            && singleBlockPrefab != null
            && heightLayers >= 3;
        int bridgeLayers = needsBridge ? 1 : 0;
        int maxConnectionLayers = heightLayers - bridgeLayers - 1;
        int connectionLayers = maxConnectionLayers > 0
            ? Mathf.Clamp(segmentConnectionLayers, 1, maxConnectionLayers)
            : 0;
        int bodyLayers = heightLayers - connectionLayers - bridgeLayers;

        if (bodyLayers > 0)
        {
            bool[,] bodyOccupied = new bool[bodyLayers, layerWidth];
            for (int layer = 0; layer < bodyLayers; layer++)
                FillLayerWithGrid(layer, segmentStartY, bodyOccupied, bodyLayers, nextOffset);

            RegisterSegmentOffsetZone(segmentStartY, segmentStartY + bodyLayers, nextOffset);
        }

        float bridgeStartY = segmentStartY + bodyLayers;
        if (needsBridge)
        {
            CreateSegmentBridge(previousOffset, nextOffset, bridgeStartY);
            RegisterSegmentOffsetZone(bridgeStartY, bridgeStartY + 1f, nextOffset);
        }

        if (connectionLayers > 0)
        {
            float connectionStartY = bridgeStartY + bridgeLayers;
            bool[,] connectionOccupied = new bool[connectionLayers, layerWidth];
            for (int layer = 0; layer < connectionLayers; layer++)
            {
                FillLayerWithGrid(
                    layer,
                    connectionStartY,
                    connectionOccupied,
                    connectionLayers,
                    previousOffset);
            }

            RegisterSegmentOffsetZone(connectionStartY, segmentStartY + heightLayers, previousOffset);
        }

        TrySpawnTrapInBatch(segmentStartY, segmentStartY + heightLayers);
        TrySpawnBouncyBatch(segmentStartY, segmentStartY + heightLayers, segmentIndex);

        UpdateTowerTopY();
    }

    void CreateSegmentBridge(int previousOffset, int nextOffset, float bridgeY)
    {
        int[] bridgeColumns = TowerSegmentShiftMath.GetBridgeColumns(
            previousOffset, nextOffset, layerWidth);
        int[] chunkWidths = TowerSegmentShiftMath.GetBridgeChunkWidths(
            bridgeColumns.Length,
            lineBlockPrefab != null,
            line3BlockPrefab != null);
        int bridgeX = bridgeColumns[0];

        for (int i = 0; i < chunkWidths.Length; i++)
        {
            int chunkWidth = chunkWidths[i];
            GameObject prefab = chunkWidth == 4
                ? lineBlockPrefab
                : chunkWidth == 3 ? line3BlockPrefab : singleBlockPrefab;
            GameObject bridgeObject = Instantiate(
                prefab,
                new Vector3(bridgeX, bridgeY, 0f),
                Quaternion.identity,
                transform);
            bridgeObject.name = $"StructuralBridge_X{bridgeX}_W{chunkWidth}_Y{bridgeY:F0}";

            TowerBlock bridgeBlock = bridgeObject.GetComponent<TowerBlock>();
            if (bridgeBlock != null)
            {
                bridgeBlock.blockTypeName = "Structural Bridge";
                bridgeBlock.ConfigureStructuralSupport();
                ApplyStructuralBridgeVisual(bridgeBlock);
            }

            TryApply3DBlockVisual(bridgeObject);
            bridgeX += chunkWidth;
        }
    }

    private static void ApplyStructuralBridgeVisual(TowerBlock bridgeBlock)
    {
        Color bridgeColor = new Color(0.08f, 0.72f, 0.82f, 1f);
        Component style = bridgeBlock.GetComponent("BlockVisualStyle");
        if (style != null)
            style.SendMessage("ApplyStyleAndLock", bridgeColor, SendMessageOptions.DontRequireReceiver);
        bridgeBlock.OverrideOriginalColor(bridgeColor);
    }


    void UpdateGeneratedMinY()
    {
        float minY = float.PositiveInfinity;
        foreach (Transform child in transform)
        {
            if (child.gameObject == hexagonBall) continue;
            if (child.position.y < minY) minY = child.position.y;
        }
        if (float.IsPositiveInfinity(minY))
        {
            currentGeneratedMinY = startHeight;
            return;
        }

        // 由于方块 pivot 可能在形状内部，这里用 position.y 作为近似下界
        currentGeneratedMinY = Mathf.Min(currentGeneratedMinY, minY);
    }

    void UpdateFoundationY()
    {
        foundationY = currentGeneratedMinY + foundationThicknessLayers;
    }

    void CleanupBlocksAboveCamera()
    {
        if (mainCamera == null) return;
        float cameraY = mainCamera.transform.position.y;
        float destroyAboveY = cameraY + destroyAboveCameraLayers;

        foreach (Transform child in transform)
        {
            if (child == null) continue;
            if (child.gameObject == hexagonBall) continue;
            if (child.position.y > destroyAboveY)
            {
                Destroy(child.gameObject);
            }
        }
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
        if (line3BlockPrefab != null) prefabs.Add(line3BlockPrefab);
        if (l2BlockPrefab != null) prefabs.Add(l2BlockPrefab);
        return prefabs;
    }

    /// <summary>
    /// 获取塔尖的Y坐标
    /// </summary>
    public float GetTowerTopY()
    {
        return currentTowerTopY;
    }

    /// <summary>
    /// 处理方块消除事件
    /// </summary>
    void HandleBlockDestroyed(TowerBlock block)
    {
        UpdateTowerTopY();
    }

    /// <summary>
    /// 更新塔尖高度
    /// </summary>
    void UpdateTowerTopY()
    {
        float maxY = float.NegativeInfinity;
        foreach (Transform child in transform)
        {
            if (child.gameObject == hexagonBall) continue;

            TowerBlock block = child.GetComponent<TowerBlock>();
            if (block != null)
            {
                float blockTopY = child.position.y + 1f; // 假设方块高度至少1单位
                if (blockTopY > maxY)
                {
                    maxY = blockTopY;
                }
            }
        }

        if (float.IsNegativeInfinity(maxY))
        {
            // 没有方块时回退到起始高度，避免相机/逻辑被 NaN 或极值影响
            currentTowerTopY = startHeight;
        }
        else
        {
            currentTowerTopY = maxY;
        }
    }

    /// <summary>
    /// 激活摄像机可视范围内及向下延伸区域的方块
    /// </summary>
    void ActivateBlocksInRange()
    {
        if (mainCamera == null) return;

        // 使用摄像机真实可视范围（正交相机）
        float camY = mainCamera.transform.position.y;
        float halfHeight = mainCamera.orthographicSize;
        float activationTop = camY + halfHeight + activationExtraAbove;
        float activationBottom = camY - halfHeight - activationExtraBelow;

        // 遍历所有方块（按高度从高到低排序，先激活上方，减少链式顶推）
        TowerBlock[] allBlocks = GetComponentsInChildren<TowerBlock>();
        System.Array.Sort(allBlocks, (a, b) =>
        {
            if (a == null && b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;
            return b.transform.position.y.CompareTo(a.transform.position.y);
        });

        int activatedCount = 0;
        int staticCount = 0;
        int dynamicCount = 0;

        int batchRemaining = activationBatchSize <= 0 ? int.MaxValue : activationBatchSize;

        foreach (TowerBlock block in allBlocks)
        {
            if (block == null) continue;

            float blockY = block.transform.position.y;

            // 结构梁是可点击消除的固定承重层，不能被常规激活或远距回收逻辑解冻。
            if (block.IsStructuralSupport)
            {
                if (!block.isStatic) block.Freeze();
                continue;
            }

            // 地基层：永远冻结，不允许变成 Dynamic（防止底部抖动/挤出）
            if (blockY <= foundationY)
            {
                if (!block.isStatic)
                {
                    block.Freeze();
                }
                continue;
            }

            if (block.isStatic)
            {
                staticCount++;

                // 只激活摄像机可视范围内的方块（上下各延伸activationDistanceBelow）
                if (blockY >= activationBottom && blockY <= activationTop)
                {
                    if (batchRemaining > 0)
                    {
                        block.MakeDynamic();
                        activatedCount++;
                        batchRemaining--;
                    }
                }
            }
            else
            {
                dynamicCount++;

                // 动态方块离开可视范围很远时，冻结回Kinematic减少抖动与求解成本
                if (blockY < activationBottom - 10f || blockY > activationTop + 10f)
                    block.Freeze();
            }
        }

        // if (activatedCount > 0)
        // {
        //     Debug.Log($"激活检查 - 摄像机Y={camY:F2}, 激活范围=[{activationBottom:F2}, {activationTop:F2}]");
        //     Debug.Log($"方块状态 - 静态:{staticCount}, 动态:{dynamicCount}, 本次激活:{activatedCount}");
        // }
    }

    /// <summary>
    /// 生成六边形球
    /// </summary>
    void SpawnHexagonBall()
    {
        UpdateTowerTopY();

        // 生成在塔尖上方中间
        float centerX = layerWidth / 2f;
        Vector3 ballPosition = new Vector3(centerX, currentTowerTopY + ballHeightOffset, 0);

        hexagonBall = Instantiate(hexagonBallPrefab, ballPosition, Quaternion.identity, transform);
        hexagonBall.name = "HexagonBall";

        // Make it slightly bigger for better readability on mobile.
        if (hexagonBallPrefab != null)
        {
            float m = Mathf.Clamp(hexagonBallScaleMultiplier, 0.25f, 5f);
            Vector3 baseScale = hexagonBallPrefab.transform.localScale;
            hexagonBall.transform.localScale = Vector3.Scale(baseScale, new Vector3(m, m, m));
        }

        // Debug.Log($"生成六边形球 at: ({ballPosition.x:F2}, {ballPosition.y:F2})");
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

        if (leftBoundary != null) Destroy(leftBoundary);
        if (rightBoundary != null) Destroy(rightBoundary);
    }

    private void EnsureBoundaries()
    {
        if (leftBoundary != null) Destroy(leftBoundary);
        if (rightBoundary != null) Destroy(rightBoundary);
        leftBoundaryXVelocity = 0f;
        rightBoundaryXVelocity = 0f;

        float xMin = -boundaryExtraCellsEachSide;
        float xMax = layerWidth + boundaryExtraCellsEachSide;

        if (leftBoundaryPrefab != null)
        {
            leftBoundary = Instantiate(leftBoundaryPrefab, new Vector3(xMin, 0f, 0f), Quaternion.identity);
            leftBoundary.name = "LeftBoundary";
            ConfigureBoundaryForHorizontalWalk(leftBoundary);
        }

        if (rightBoundaryPrefab != null)
        {
            rightBoundary = Instantiate(rightBoundaryPrefab, new Vector3(xMax, 0f, 0f), Quaternion.identity);
            rightBoundary.name = "RightBoundary";
            ConfigureBoundaryForHorizontalWalk(rightBoundary);
        }

        UpdateBoundariesFollow(force: true);
    }

    private void ConfigureBoundaryForHorizontalWalk(GameObject boundary)
    {
        if (boundary == null || !enableSegmentHorizontalWalk) return;

        // 边界预制体是跨越多个塔段的长触发器。横移后若继续清理普通方块，
        // 会把相邻旧区段误判为越界；这里只保留球的失败判定。
        Component boundaryLogic = boundary.GetComponent("GameOverBoundary");
        if (boundaryLogic == null) return;

        System.Reflection.FieldInfo cleanupField = boundaryLogic.GetType().GetField("autoCleanupNonBallBlocks");
        if (cleanupField != null) cleanupField.SetValue(boundaryLogic, false);
    }


    private void UpdateBoundariesFollow(bool force = false)
    {
        if (mainCamera == null) return;

        float regionY = hexagonBall != null
            ? hexagonBall.transform.position.y
            : mainCamera.transform.position.y;
        int regionOffset = GetSegmentXOffsetAtY(regionY);
        float targetY = boundaryFollowCameraY
            ? mainCamera.transform.position.y + boundaryFollowYOffset
            : 0f;
        float leftX = regionOffset - boundaryExtraCellsEachSide;
        float rightX = regionOffset + layerWidth + boundaryExtraCellsEachSide;

        if (leftBoundary != null)
        {
            Vector3 p = leftBoundary.transform.position;
            float y = boundaryFollowCameraY ? targetY : p.y;
            float x = force
                ? leftX
                : Mathf.SmoothDamp(
                    p.x,
                    leftX,
                    ref leftBoundaryXVelocity,
                    Mathf.Max(0.01f, boundaryHorizontalSmoothTime),
                    Mathf.Max(0.1f, maxBoundaryHorizontalSpeed),
                    Time.deltaTime);
            if (force || !Mathf.Approximately(p.x, x) || !Mathf.Approximately(p.y, y))
                leftBoundary.transform.position = new Vector3(x, y, p.z);
        }

        if (rightBoundary != null)
        {
            Vector3 p = rightBoundary.transform.position;
            float y = boundaryFollowCameraY ? targetY : p.y;
            float x = force
                ? rightX
                : Mathf.SmoothDamp(
                    p.x,
                    rightX,
                    ref rightBoundaryXVelocity,
                    Mathf.Max(0.01f, boundaryHorizontalSmoothTime),
                    Mathf.Max(0.1f, maxBoundaryHorizontalSpeed),
                    Time.deltaTime);
            if (force || !Mathf.Approximately(p.x, x) || !Mathf.Approximately(p.y, y))
                rightBoundary.transform.position = new Vector3(x, y, p.z);
        }
    }

    /// <summary>
    /// 重置塔
    /// </summary>
    public void ResetTower()
    {
        // Reset activation state.
        // On restart we always defer activation until external code calls StartActivation()
        // (e.g., after the camera is back to the tower top).
        activationEnabled = false;
        initialActivationPending = false;

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

        // 刚重建完塔，重新设置稳定化窗口（与 Start() 保持一致）
        if (stabilizeNewSegment)
        {
            FreezeBlocksInYRange(startHeight - 1f, startHeight + towerLayers + 2f);
            stabilizeUntilTime = Time.time + Mathf.Max(0.01f, Mathf.Max(initialActivationDelay, stabilizeDuration));
        }
        else
        {
            stabilizeUntilTime = Time.time + Mathf.Max(0.01f, initialActivationDelay);
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
