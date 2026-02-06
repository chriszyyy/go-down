using UnityEngine;

/// <summary>
/// 塔构建器 - 使用预制体拼装塔（基于网格的随机填充算法）
/// </summary>
public class TowerBuilder : MonoBehaviour
{
    private const float SpecialBlockChance = 0.018f;
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

    [Header("六边形球配置")]
    [Tooltip("六边形球预制体")]
    public GameObject hexagonBallPrefab;

    [Tooltip("是否自动生成六边形球")]
    public bool spawnHexagonBall = true;

    [Tooltip("球相对于顶部的高度偏移")]
    public float ballHeightOffset = 1.5f;

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

        EnsureBoundaries();

        // 初始化网格占用矩阵（仅用于初始段的放置计算）
        gridOccupied = new bool[towerLayers, layerWidth];

        // 生成初始段：从 startHeight 开始向上 towerLayers 层
        for (int layer = 0; layer < towerLayers; layer++)
        {
            FillLayerWithGrid(layer, startHeight, gridOccupied, towerLayers);
        }

        currentGeneratedMinY = startHeight;
        lastGeneratedMinY = currentGeneratedMinY;

        // 更新塔尖高度
        UpdateTowerTopY();
    }

    /// <summary>
    /// 基于网格填充一层（随机放置算法）
    /// </summary>
    void FillLayerWithGrid(int layerIndex, float baseY, bool[,] occupied, int layerLimit)
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
            GameObject placedBlock = TryPlaceBlockAt(layerIndex, currentCol, baseY, occupied, layerLimit);

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
    GameObject TryPlaceBlockAt(int layer, int col, float baseY, bool[,] occupied, int layerLimit)
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
                    GameObject block = PlaceBlock(prefab, rotation, layer, col, baseY, occupied, layerLimit);
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
    GameObject PlaceBlock(GameObject prefab, float rotation, int layer, int col, float baseY, bool[,] occupied, int layerLimit)
    {
        TowerBlock blockComponent = prefab.GetComponent<TowerBlock>();

        // 取出对应pivot点，放置在网格位置(col, layer)
        Vector2Int bottomLeftCorner = blockComponent.GetBottomLeftCorner(rotation);
        float worldX = col - bottomLeftCorner.x;
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

        if (UnityEngine.Random.value >= SpecialBlockChance) return;

        // Attach animated rainbow gradient + glow (without a compile-time dependency on Visuals asmdef).
        if (block.GetComponent("RainbowGlowVisual") == null)
        {
            var t = System.Type.GetType("RainbowGlowVisual, GoDown.Visuals");
            if (t != null)
            {
                block.AddComponent(t);
            }
            else
            {
                Debug.LogWarning("TowerBuilder: 未找到 RainbowGlowVisual 类型（GoDown.Visuals）。将跳过特殊方块发光效果。");
            }
        }

        // Ensure any palette-based styling won't tint the rainbow shader.
        Color white = Color.white;
        white.a = 1f;

        Component style = block.GetComponent("BlockVisualStyle");
        if (style != null)
        {
            style.SendMessage("ApplyStyleAndLock", white, SendMessageOptions.DontRequireReceiver);
        }

        TowerBlock tb = block.GetComponent<TowerBlock>();
        if (tb != null)
        {
            tb.OverrideOriginalColor(white);
        }
    }

    // 从世界中的现有方块采样“最底部若干层”的格子占用，用于新段顶部的无缝接合约束
    bool[,] BuildSeamConstraintForNewSegment(float newSegmentStartY, int seamLayers, int newSegmentHeightLayers)
    {
        // seam约束矩阵尺寸与新段一致：true=该格子禁止放置（已被上一段的最底部占用）
        bool[,] seamOccupied = new bool[newSegmentHeightLayers, layerWidth];
        if (seamLayers <= 0) return seamOccupied;

        // 新段的“顶部 seam 区域”在 newSegment 内对应的层区间： [newH - seamLayers, newH)
        int seamStartLayerInNew = Mathf.Max(0, newSegmentHeightLayers - seamLayers);

        foreach (Transform child in transform)
        {
            if (child == null) continue;
            if (child.gameObject == hexagonBall) continue;

            TowerBlock block = child.GetComponent<TowerBlock>();
            if (block == null) continue;

            // 只采样“当前已生成内容最底部附近”的方块，避免把很远的上方也计入
            // 这里用 worldY 与 newSegmentStartY 的关系做过滤：只关心即将接合的那一带
            float worldY = child.position.y;
            if (worldY < newSegmentStartY + newSegmentHeightLayers - seamLayers - 2f) continue;
            if (worldY > newSegmentStartY + newSegmentHeightLayers + 2f) continue;

            // 将方块pivot对齐到格子：col = round(x), layer = round(y - newSegmentStartY)
            int approxCol = Mathf.RoundToInt(child.position.x);
            int approxLayer = Mathf.RoundToInt(child.position.y - newSegmentStartY);

            // 用方块自身占用格子来标记 seam（近似：使用transform当前旋转）
            float rotZ = child.eulerAngles.z;
            var occupiedCells = block.GetOccupiedCells(rotZ);
            var bottomLeft = block.GetBottomLeftCorner(rotZ);

            // 我们的 PlaceBlock 使用 col - bottomLeftCorner.x 作为 worldX
            // 反推 pivot col：col = round(worldX) + bottomLeftCorner.x
            int pivotCol = Mathf.RoundToInt(child.position.x) + bottomLeft.x;
            int pivotLayer = Mathf.RoundToInt(child.position.y - newSegmentStartY) + bottomLeft.y;

            foreach (var (dx, dy) in occupiedCells)
            {
                int c = pivotCol + dx;
                int l = pivotLayer + dy;

                if (c < 0 || c >= layerWidth) continue;
                if (l < seamStartLayerInNew || l >= newSegmentHeightLayers) continue;

                seamOccupied[l, c] = true;
            }
        }

        return seamOccupied;
    }

    void TryGenerateNextSegment()
    {
        if (mainCamera == null) return;

        float cameraY = mainCamera.transform.position.y;
        float triggerY = currentGeneratedMinY + generateAheadLayers;

        // 低频日志：仅在接近触发阈值时输出，避免刷屏
        if (Time.time - lastSegmentCheckLogTime >= 1.0f)
        {
            float distanceToTrigger = cameraY - triggerY;
            if (distanceToTrigger <= Mathf.Max(2f, segmentHeightLayers * 0.25f))
            {
                lastSegmentCheckLogTime = Time.time;
                // Debug.Log($"段检查: cameraY={cameraY:F2}, currentGeneratedMinY={currentGeneratedMinY:F2}, triggerY={triggerY:F2}");
            }
        }

        // 摄像机接近当前已生成底部时，在更下面续接生成一段
        if (cameraY <= triggerY)
        {
            // 防止重复触发：只有当 currentGeneratedMinY 发生变化后才能再次生成
            if (!Mathf.Approximately(lastGeneratedMinY, currentGeneratedMinY))
                return;

            float newSegmentStartY = currentGeneratedMinY - segmentHeightLayers;
            // Debug.Log($"准备生成新段: fromMinY={currentGeneratedMinY:F2} -> newStartY={newSegmentStartY:F2}, cameraY={cameraY:F2}, triggerY={triggerY:F2}");
            BuildTowerSegment(newSegmentStartY, segmentHeightLayers);
            currentGeneratedMinY = newSegmentStartY;
            lastGeneratedMinY = currentGeneratedMinY;
            UpdateFoundationY();

            if (stabilizeNewSegment)
            {
                FreezeBlocksInYRange(newSegmentStartY - 1f, newSegmentStartY + segmentHeightLayers + 2f);
                stabilizeUntilTime = Time.time + Mathf.Max(0.01f, stabilizeDuration);
            }

            // Debug.Log($"生成新段完成: currentGeneratedMinY={currentGeneratedMinY:F2}, foundationY={foundationY:F2}");
        }
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

    void BuildTowerSegment(float segmentStartY, int heightLayers)
    {
        // 续接段：独立 occupancy + 顶部 seam 约束，保证与上一段无缝贴合
        // seamLayers 选 2 层通常足够覆盖复杂形状的接触边界
        int seamLayers = 2;
        bool[,] segmentOccupied = new bool[heightLayers, layerWidth];

        // 用上一段最底部的占用，预占新段顶部的 seam 区域，避免交界重叠
        bool[,] seamOccupied = BuildSeamConstraintForNewSegment(segmentStartY, seamLayers, heightLayers);
        for (int r = heightLayers - seamLayers; r < heightLayers; r++)
        {
            if (r < 0) continue;
            for (int c = 0; c < layerWidth; c++)
                segmentOccupied[r, c] = seamOccupied[r, c];
        }

        int placedBlocks = 0;

        for (int layer = 0; layer < heightLayers; layer++)
        {
            int before = transform.childCount;
            FillLayerWithGrid(layer, segmentStartY, segmentOccupied, heightLayers);
            int after = transform.childCount;
            if (after > before) placedBlocks += (after - before);
        }

        // Debug.Log($"段生成统计: startY={segmentStartY:F2}, layers={heightLayers}, placedApprox={placedBlocks}, seamLayers={seamLayers}");

        UpdateTowerTopY();
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
        // 重新计算塔尖高度
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
        // 先清理旧边界
        if (leftBoundary != null) Destroy(leftBoundary);
        if (rightBoundary != null) Destroy(rightBoundary);

        float xMin = -boundaryExtraCellsEachSide;
        float xMax = layerWidth + boundaryExtraCellsEachSide;

        if (leftBoundaryPrefab != null)
        {
            leftBoundary = Instantiate(leftBoundaryPrefab, new Vector3(xMin, 0f, 0f), Quaternion.identity);
            leftBoundary.name = "LeftBoundary";
        }

        if (rightBoundaryPrefab != null)
        {
            rightBoundary = Instantiate(rightBoundaryPrefab, new Vector3(xMax, 0f, 0f), Quaternion.identity);
            rightBoundary.name = "RightBoundary";
        }

        UpdateBoundariesFollow(force: true);
    }

    private void UpdateBoundariesFollow(bool force = false)
    {
        if (!boundaryFollowCameraY) return;
        if (mainCamera == null) return;

        float targetY = mainCamera.transform.position.y + boundaryFollowYOffset;

        if (leftBoundary != null)
        {
            Vector3 p = leftBoundary.transform.position;
            if (force || !Mathf.Approximately(p.y, targetY))
                leftBoundary.transform.position = new Vector3(p.x, targetY, p.z);
        }

        if (rightBoundary != null)
        {
            Vector3 p = rightBoundary.transform.position;
            if (force || !Mathf.Approximately(p.y, targetY))
                rightBoundary.transform.position = new Vector3(p.x, targetY, p.z);
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
