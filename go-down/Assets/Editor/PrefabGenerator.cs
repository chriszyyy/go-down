using UnityEngine;
using UnityEditor;
using System.IO;

public class PrefabGenerator : EditorWindow
{
    private const string BLOCK_PHYSICS_MATERIAL_PATH = "Assets/PhysicsMaterials/BlockPhysicsMaterial.physicsMaterial2D";
    private const float DEFAULT_BLOCK_FRICTION = 0.35f;
    private const float DEFAULT_BLOCK_BOUNCINESS = 0f;
    private const float DEFAULT_BLOCK_DRAG = 0.05f;
    private const float DEFAULT_BLOCK_ANGULAR_DRAG = 1.0f;

    private const string MAIN_PREFAB_PATH = "Assets/Prefabs";
    private const string BLOCKS_PREFAB_PATH = "Assets/Prefabs/Blocks";

    // 物理容差：让碰撞体略小于逻辑格子，降低初始生成/长期堆叠时的挤爆与抖动
    // 注意：这里的单位是“世界单位(=1格)”。该值越大，缝隙越明显；越小越容易挤爆。
    private const float COLLIDER_TOLERANCE = 0.015f;

    // 统一的格子单位：生成逻辑里默认 1 格 = 1 世界单位
    private const float GRID_UNIT = 1f;

    // Block visual style (base + inset highlight)
    private const int BLOCK_SPRITE_PPU = 64;
    private const int BLOCK_HIGHLIGHT_INSET_PX = 6; // inset border thickness, purely visual
    private const int BLOCK_CORNER_RADIUS_PX = 2;   // 外凸角圆角半径（像素），同时作用于 base 和 highlight
    private static readonly Color BLOCK_BASE_WHITE = Color.white;

    /// <summary>
    /// 判断在 [minX..maxX) × [minY..maxY) 矩形里，(x,y) 是否被 BB 角的圆角裁剪掉。
    /// 适用于 Square / Line / 单个矩形分量；圆角只裁剪四个 BB 角象限。
    /// </summary>
    private static bool IsClippedByCornerRadius(int x, int y, int minX, int minY, int maxX, int maxY, int radius)
    {
        if (radius <= 0) return false;

        int innerLeft = minX + radius;
        int innerRight = maxX - 1 - radius;
        int innerBottom = minY + radius;
        int innerTop = maxY - 1 - radius;
        int rSq = radius * radius;

        // TL
        if (x < innerLeft && y > innerTop)
        {
            int dx = innerLeft - x, dy = y - innerTop;
            if (dx * dx + dy * dy > rSq) return true;
        }
        // TR
        if (x > innerRight && y > innerTop)
        {
            int dx = x - innerRight, dy = y - innerTop;
            if (dx * dx + dy * dy > rSq) return true;
        }
        // BL
        if (x < innerLeft && y < innerBottom)
        {
            int dx = innerLeft - x, dy = innerBottom - y;
            if (dx * dx + dy * dy > rSq) return true;
        }
        // BR
        if (x > innerRight && y < innerBottom)
        {
            int dx = x - innerRight, dy = innerBottom - y;
            if (dx * dx + dy * dy > rSq) return true;
        }
        return false;
    }

    /// <summary>
    /// 多矩形组合形状（如 L）的圆角裁剪：仅当该 BB 角是 union 的真正外凸角时才裁剪。
    /// 判定方法：检查该 BB 角的两条**直邻边**（横向 + 纵向）外侧是否都在 union 外。
    /// 若任一直邻边在 union 内，说明这个 BB 角是被另一个 rect 接续的"接缝"或"凹角"，不裁剪。
    /// </summary>
    private static bool ShouldClipUnionCorner(int x, int y, int minX, int minY, int maxX, int maxY, int radius, System.Func<int, int, bool> inUnion)
    {
        if (radius <= 0) return false;

        int innerLeft = minX + radius;
        int innerRight = maxX - 1 - radius;
        int innerBottom = minY + radius;
        int innerTop = maxY - 1 - radius;
        int rSq = radius * radius;

        // TL：直邻 = 左侧 (minX-1, y) 和 上侧 (x, maxY)
        if (x < innerLeft && y > innerTop)
        {
            int dx = innerLeft - x, dy = y - innerTop;
            if (dx * dx + dy * dy > rSq && !inUnion(minX - 1, y) && !inUnion(x, maxY)) return true;
        }
        // TR：直邻 = 右侧 (maxX, y) 和 上侧 (x, maxY)
        if (x > innerRight && y > innerTop)
        {
            int dx = x - innerRight, dy = y - innerTop;
            if (dx * dx + dy * dy > rSq && !inUnion(maxX, y) && !inUnion(x, maxY)) return true;
        }
        // BL：直邻 = 左侧 (minX-1, y) 和 下侧 (x, minY-1)
        if (x < innerLeft && y < innerBottom)
        {
            int dx = innerLeft - x, dy = innerBottom - y;
            if (dx * dx + dy * dy > rSq && !inUnion(minX - 1, y) && !inUnion(x, minY - 1)) return true;
        }
        // BR：直邻 = 右侧 (maxX, y) 和 下侧 (x, minY-1)
        if (x > innerRight && y < innerBottom)
        {
            int dx = x - innerRight, dy = innerBottom - y;
            if (dx * dx + dy * dy > rSq && !inUnion(maxX, y) && !inUnion(x, minY - 1)) return true;
        }
        return false;
    }

    private static Vector2 GridCellCenterOffset(float widthInCells, float heightInCells)
    {
        return new Vector2(widthInCells * GRID_UNIT * 0.5f, heightInCells * GRID_UNIT * 0.5f);
    }

    [MenuItem("Tools/Generate Block Prefabs")]
    public static void ShowWindow()
    {
        GetWindow<PrefabGenerator>("Prefab Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("方块预制体生成器", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("生成所有方块Prefab", GUILayout.Height(40)))
        {
            GenerateAllPrefabs();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("生成六边形球Prefab", GUILayout.Height(40)))
        {
            CreateHexagonBallPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("完成", "六边形球Prefab已生成到 Assets/Prefabs/", "确定");
        }

        GUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "这会自动创建:\n" +
            "• 所有方块的Sprite\n" +
            "• 六边形球的Sprite\n" +
            "• 配置好的Prefab\n" +
            "• 保存到 Assets/Prefabs/",
            MessageType.Info);
    }

    private static void GenerateAllPrefabs()
    {
        // 创建必要的文件夹
        if (!AssetDatabase.IsValidFolder(MAIN_PREFAB_PATH))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder(BLOCKS_PREFAB_PATH))
            AssetDatabase.CreateFolder(MAIN_PREFAB_PATH, "Blocks");
        if (!AssetDatabase.IsValidFolder("Assets/Sprites"))
            AssetDatabase.CreateFolder("Assets", "Sprites");
        if (!AssetDatabase.IsValidFolder("Assets/PhysicsMaterials"))
            AssetDatabase.CreateFolder("Assets", "PhysicsMaterials");

        PhysicsMaterial2D blockPhysicsMaterial = GetOrCreateBlockPhysicsMaterial();

        // 生成各种方块
        CreateSingleBlockPrefab(blockPhysicsMaterial);
        CreateSquareBlockPrefab(blockPhysicsMaterial);
        CreateL3BlockPrefab(blockPhysicsMaterial);
        CreateL4BlockPrefab(blockPhysicsMaterial);
        CreateL5BlockPrefab(blockPhysicsMaterial);
        CreateLineBlockPrefab(blockPhysicsMaterial);
        CreateLine3BlockPrefab(blockPhysicsMaterial);
        CreateL2BlockPrefab(blockPhysicsMaterial);

        // 生成六边形球
        CreateHexagonBallPrefab();

        // 生成左右边界（用于碰撞触发 GameOver）
        CreateGameOverBoundariesPrefabs();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("✅ 所有Prefab生成完成！");
        EditorUtility.DisplayDialog("完成", "所有Prefab已生成到 Assets/Prefabs/", "确定");
    }

    private static void CreateGameOverBoundariesPrefabs()
    {
        CreateGameOverBoundaryPrefab("LeftBoundary", -1f, "Left Boundary Hit");
        CreateGameOverBoundaryPrefab("RightBoundary", 1f, "Right Boundary Hit");
    }

    // sideSign: -1 = left, +1 = right
    private static void CreateGameOverBoundaryPrefab(string name, float sideSign, string reason)
    {
        GameObject go = new GameObject(name);

        // A trigger collider; height is tall enough to catch the ball in most scenes.
        BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(1f, 200f);

        GameOverBoundary boundary = go.AddComponent<GameOverBoundary>();
        boundary.requiredTag = "HexagonBall";
        boundary.reason = reason;

        // Store side info on X via localPosition when you instantiate.
        // Prefab itself stays at origin.
        SavePrefab(go, $"{MAIN_PREFAB_PATH}/{name}.prefab");
        Object.DestroyImmediate(go);
    }

    // 1. 单格方块
    private static void CreateSingleBlockPrefab(PhysicsMaterial2D physicsMaterial)
    {
        GameObject go = new GameObject("SingleBlock");

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSquareSprite(64, 64, BLOCK_BASE_WHITE);
        sr.sortingOrder = 0;

        CreateHighlightChild(go, sr, CreateSquareHighlightSprite(64, 64, BLOCK_BASE_WHITE));

        BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(GRID_UNIT - COLLIDER_TOLERANCE, GRID_UNIT - COLLIDER_TOLERANCE);
        collider.offset = GridCellCenterOffset(1f, 1f);
        collider.sharedMaterial = physicsMaterial;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
        rb.interpolation = RigidbodyInterpolation2D.None;
        rb.sleepMode = RigidbodySleepMode2D.StartAsleep;
        rb.drag = DEFAULT_BLOCK_DRAG;
        rb.angularDrag = DEFAULT_BLOCK_ANGULAR_DRAG;

        TowerBlock block = go.AddComponent<TowerBlock>();
        block.blockTypeName = "Single";
        block.scoreValue = 10;
        block.isStatic = true;

        go.AddComponent<BlockVisualStyle>();

        go.layer = LayerMask.NameToLayer("Block");

        SavePrefab(go, "Assets/Prefabs/Blocks/SingleBlock.prefab");
        Object.DestroyImmediate(go);
    }

    // 2. 正方形方块 (2x2)
    private static void CreateSquareBlockPrefab(PhysicsMaterial2D physicsMaterial)
    {
        GameObject go = new GameObject("SquareBlock");

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSquareSprite(128, 128, BLOCK_BASE_WHITE);

        CreateHighlightChild(go, sr, CreateSquareHighlightSprite(128, 128, BLOCK_BASE_WHITE));

        BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(2f * GRID_UNIT - COLLIDER_TOLERANCE, 2f * GRID_UNIT - COLLIDER_TOLERANCE);
        collider.offset = GridCellCenterOffset(2f, 2f);
        collider.sharedMaterial = physicsMaterial;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.mass = 4f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
        rb.interpolation = RigidbodyInterpolation2D.None;
        rb.sleepMode = RigidbodySleepMode2D.StartAsleep;
        rb.drag = DEFAULT_BLOCK_DRAG;
        rb.angularDrag = DEFAULT_BLOCK_ANGULAR_DRAG;

        SquareBlock block = go.AddComponent<SquareBlock>();
        block.blockTypeName = "Square";
        block.scoreValue = 40;
        block.isStatic = true;

        go.AddComponent<BlockVisualStyle>();

        go.layer = LayerMask.NameToLayer("Block");

        SavePrefab(go, "Assets/Prefabs/Blocks/SquareBlock.prefab");
        Object.DestroyImmediate(go);
    }

    // 3. L3型方块
    private static void CreateL3BlockPrefab(PhysicsMaterial2D physicsMaterial)
    {
        GameObject go = new GameObject("L3Block");

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateLShapeSprite(3, BLOCK_BASE_WHITE);

        CreateHighlightChild(go, sr, CreateLShapeHighlightSprite(3, BLOCK_BASE_WHITE));

        PolygonCollider2D collider = go.AddComponent<PolygonCollider2D>();
        collider.points = CreateLShapeColliderPoints(3);
        collider.sharedMaterial = physicsMaterial;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.mass = 3f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
        rb.interpolation = RigidbodyInterpolation2D.None;
        rb.sleepMode = RigidbodySleepMode2D.StartAsleep;
        rb.drag = DEFAULT_BLOCK_DRAG;
        rb.angularDrag = DEFAULT_BLOCK_ANGULAR_DRAG;

        L3Block block = go.AddComponent<L3Block>();
        block.blockTypeName = "L3";
        block.scoreValue = 40;
        block.isStatic = true;

        go.AddComponent<BlockVisualStyle>();

        go.layer = LayerMask.NameToLayer("Block");

        SavePrefab(go, "Assets/Prefabs/Blocks/L3Block.prefab");
        Object.DestroyImmediate(go);
    }

    // 4. L4型方块
    private static void CreateL4BlockPrefab(PhysicsMaterial2D physicsMaterial)
    {
        GameObject go = new GameObject("L4Block");

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateLShapeSprite(4, BLOCK_BASE_WHITE);

        CreateHighlightChild(go, sr, CreateLShapeHighlightSprite(4, BLOCK_BASE_WHITE));

        PolygonCollider2D collider = go.AddComponent<PolygonCollider2D>();
        collider.points = CreateLShapeColliderPoints(4);
        collider.sharedMaterial = physicsMaterial;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.mass = 4f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
        rb.interpolation = RigidbodyInterpolation2D.None;
        rb.sleepMode = RigidbodySleepMode2D.StartAsleep;
        rb.drag = DEFAULT_BLOCK_DRAG;
        rb.angularDrag = DEFAULT_BLOCK_ANGULAR_DRAG;

        L4Block block = go.AddComponent<L4Block>();
        block.blockTypeName = "L4";
        block.scoreValue = 50;
        block.isStatic = true;

        go.AddComponent<BlockVisualStyle>();

        go.layer = LayerMask.NameToLayer("Block");

        SavePrefab(go, "Assets/Prefabs/Blocks/L4Block.prefab");
        Object.DestroyImmediate(go);
    }

    // 5. L5型方块 (等长L形 3x3)
    private static void CreateL5BlockPrefab(PhysicsMaterial2D physicsMaterial)
    {
        GameObject go = new GameObject("L5Block");

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateEqualLShapeSprite(BLOCK_BASE_WHITE);

        CreateHighlightChild(go, sr, CreateEqualLShapeHighlightSprite(BLOCK_BASE_WHITE));

        PolygonCollider2D collider = go.AddComponent<PolygonCollider2D>();
        collider.points = CreateEqualLShapeColliderPoints();
        collider.sharedMaterial = physicsMaterial;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.mass = 5f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
        rb.interpolation = RigidbodyInterpolation2D.None;
        rb.sleepMode = RigidbodySleepMode2D.StartAsleep;
        rb.drag = DEFAULT_BLOCK_DRAG;
        rb.angularDrag = DEFAULT_BLOCK_ANGULAR_DRAG;

        L5Block block = go.AddComponent<L5Block>();
        block.blockTypeName = "L5";
        block.scoreValue = 50;
        block.isStatic = true;

        go.AddComponent<BlockVisualStyle>();

        go.layer = LayerMask.NameToLayer("Block");

        SavePrefab(go, "Assets/Prefabs/Blocks/L5Block.prefab");
        Object.DestroyImmediate(go);
    }

    // 6. I型方块 (4格横条)
    private static void CreateLineBlockPrefab(PhysicsMaterial2D physicsMaterial)
    {
        GameObject go = new GameObject("LineBlock");

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateLineSprite(4, BLOCK_BASE_WHITE);

        CreateHighlightChild(go, sr, CreateLineHighlightSprite(4, BLOCK_BASE_WHITE));

        BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(4f * GRID_UNIT - COLLIDER_TOLERANCE, GRID_UNIT - COLLIDER_TOLERANCE);
        collider.offset = GridCellCenterOffset(4f, 1f);
        collider.sharedMaterial = physicsMaterial;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.mass = 4f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
        rb.interpolation = RigidbodyInterpolation2D.None;
        rb.sleepMode = RigidbodySleepMode2D.StartAsleep;
        rb.drag = DEFAULT_BLOCK_DRAG;
        rb.angularDrag = DEFAULT_BLOCK_ANGULAR_DRAG;

        LineBlock block = go.AddComponent<LineBlock>();
        block.blockTypeName = "Line";
        block.scoreValue = 40;
        block.isStatic = true;

        go.AddComponent<BlockVisualStyle>();

        go.layer = LayerMask.NameToLayer("Block");

        SavePrefab(go, "Assets/Prefabs/Blocks/LineBlock.prefab");
        Object.DestroyImmediate(go);
    }

    // 7. I型方块 (3格横条)
    private static void CreateLine3BlockPrefab(PhysicsMaterial2D physicsMaterial)
    {
        GameObject go = new GameObject("Line3Block");

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateLineSprite(3, BLOCK_BASE_WHITE);

        CreateHighlightChild(go, sr, CreateLineHighlightSprite(3, BLOCK_BASE_WHITE));

        BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(3f * GRID_UNIT - COLLIDER_TOLERANCE, GRID_UNIT - COLLIDER_TOLERANCE);
        collider.offset = GridCellCenterOffset(3f, 1f);
        collider.sharedMaterial = physicsMaterial;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.mass = 3f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
        rb.interpolation = RigidbodyInterpolation2D.None;
        rb.sleepMode = RigidbodySleepMode2D.StartAsleep;
        rb.drag = DEFAULT_BLOCK_DRAG;
        rb.angularDrag = DEFAULT_BLOCK_ANGULAR_DRAG;

        Line3Block block = go.AddComponent<Line3Block>();
        block.blockTypeName = "Line3";
        block.scoreValue = 30;
        block.isStatic = true;

        go.AddComponent<BlockVisualStyle>();

        go.layer = LayerMask.NameToLayer("Block");

        SavePrefab(go, "Assets/Prefabs/Blocks/Line3Block.prefab");
        Object.DestroyImmediate(go);
    }

    // 8. L2型方块 (3格L形)
    private static void CreateL2BlockPrefab(PhysicsMaterial2D physicsMaterial)
    {
        GameObject go = new GameObject("L2Block");

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateLShapeSprite(2, BLOCK_BASE_WHITE);

        CreateHighlightChild(go, sr, CreateLShapeHighlightSprite(2, BLOCK_BASE_WHITE));

        PolygonCollider2D collider = go.AddComponent<PolygonCollider2D>();
        collider.points = CreateLShapeColliderPoints(2);
        collider.sharedMaterial = physicsMaterial;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.mass = 3f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
        rb.interpolation = RigidbodyInterpolation2D.None;
        rb.sleepMode = RigidbodySleepMode2D.StartAsleep;
        rb.drag = DEFAULT_BLOCK_DRAG;
        rb.angularDrag = DEFAULT_BLOCK_ANGULAR_DRAG;

        L2Block block = go.AddComponent<L2Block>();
        block.blockTypeName = "L2";
        block.scoreValue = 30;
        block.isStatic = true;

        go.AddComponent<BlockVisualStyle>();

        go.layer = LayerMask.NameToLayer("Block");

        SavePrefab(go, "Assets/Prefabs/Blocks/L2Block.prefab");
        Object.DestroyImmediate(go);
    }

    // ========== 辅助方法 ==========

    // 创建L形碰撞器的点（逆时针）
    private static Vector2[] CreateLShapeColliderPoints(int blocks)
    {
        float gap = COLLIDER_TOLERANCE * 0.5f;

        // L形状（2格宽，blocks格高）在右上角区域：
        // 由于图形绘制在右上角，需要计算正确的偏移
        // 原始尺寸: 2*64 x blocks*64，纹理尺寸: 4*64 x 2*blocks*64
        // 图形中心偏移: (64, blocks*32) pixels = (1.0f, blocks*0.5f) units
        float offsetX = 1.0f * GRID_UNIT;
        float offsetY = (float)blocks * 0.5f * GRID_UNIT;

        float width = 2f * GRID_UNIT;
        float height = (float)blocks * GRID_UNIT;

        return new Vector2[]
        {
            new Vector2(-width/2f + gap + offsetX, -height/2f + gap + offsetY),                         // ①左下角
            new Vector2(width/2f - gap + offsetX, -height/2f + gap + offsetY),                          // ②右下角
            new Vector2(width/2f - gap + offsetX, -height/2f + GRID_UNIT - gap + offsetY),              // ③右下方块顶部
            new Vector2(-gap + offsetX, -height/2f + GRID_UNIT - gap + offsetY),                        // ④转折点
            new Vector2(-gap + offsetX, height/2f - gap + offsetY),                                     // ⑤左上
            new Vector2(-width/2f + gap + offsetX, height/2f - gap + offsetY),                          // ⑥左上角
        };
    }

    // 创建等长L形碰撞器的点（3x3的L形）
    private static Vector2[] CreateEqualLShapeColliderPoints()
    {
        float gap = COLLIDER_TOLERANCE * 0.5f;

        // 等长L形（3x3）在右上角区域：
        // 由于图形绘制在右上角，需要计算正确的偏移
        // 原始尺寸: 3*64 x 3*64，纹理尺寴: 6*64 x 6*64
        // 图形中心偏移: (96, 96) pixels = (1.5f, 1.5f) units
        float offsetX = 1.5f * GRID_UNIT;
        float offsetY = 1.5f * GRID_UNIT;

        float size = 3f * GRID_UNIT;
        float half = size / 2f;

        return new Vector2[]
        {
            new Vector2(-half + gap + offsetX, -half + gap + offsetY),                         // ①左下角
            new Vector2(half - gap + offsetX, -half + gap + offsetY),                          // ②右下角
            new Vector2(half - gap + offsetX, -half + GRID_UNIT - gap + offsetY),              // ③右下第一格顶部
            new Vector2(-half + GRID_UNIT - gap + offsetX, -half + GRID_UNIT - gap + offsetY), // ④转折点
            new Vector2(-half + GRID_UNIT - gap + offsetX, half - gap + offsetY),              // ⑤左上
            new Vector2(-half + gap + offsetX, half - gap + offsetY),                           // ⑥左上角
        };
    }

    // 创建正方形Sprite（图形绘制在右上角）
    private static Sprite CreateSquareSprite(int width, int height, Color color)
    {
        // 创建更大的纹理，把图形绘制在右上角
        int textureWidth = width * 2;  // 纹理宽度加倍
        int textureHeight = height * 2; // 纹理高度加倍

        Texture2D texture = new Texture2D(textureWidth, textureHeight);
        Color[] pixels = new Color[textureWidth * textureHeight];

        // 透明背景
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }

        // 在右上角区域绘制方块（从(width,height)到(textureWidth,textureHeight)）
        for (int y = height; y < textureHeight; y++)
        {
            for (int x = width; x < textureWidth; x++)
            {
                if (IsClippedByCornerRadius(x, y, width, height, textureWidth, textureHeight, BLOCK_CORNER_RADIUS_PX)) continue;
                pixels[y * textureWidth + x] = color;
            }
        }
        texture.SetPixels(pixels);
        texture.Apply();

        string path = $"Assets/Sprites/Square_{width}x{height}.png";
        byte[] pngData = texture.EncodeToPNG();
        File.WriteAllBytes(path, pngData);
        AssetDatabase.ImportAsset(path);

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.spritePixelsPerUnit = BLOCK_SPRITE_PPU;
            importer.textureType = TextureImporterType.Sprite;
            importer.filterMode = FilterMode.Point;
            importer.spriteImportMode = SpriteImportMode.Single;
            AssetDatabase.WriteImportSettingsIfDirty(path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // 创建L形Sprite（图形绘制在右上角）
    private static Sprite CreateLShapeSprite(int blocks, Color color)
    {
        int size = 64;
        int originalWidth = 2 * size;
        int originalHeight = blocks * size;

        // 创建更大的纹理，把L形绘制在右上角
        int textureWidth = originalWidth * 2;  // 纹理宽度加倍
        int textureHeight = originalHeight * 2; // 纹理高度加倍

        Texture2D texture = new Texture2D(textureWidth, textureHeight);
        Color[] pixels = new Color[textureWidth * textureHeight];

        // 透明背景
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }

        // 在右上角绘制L形：左边一列 + 底部一行。
        // 列的 BB：x ∈ [originalWidth, originalWidth+size), y ∈ [originalHeight, textureHeight)
        // 行的 BB：x ∈ [originalWidth, textureWidth),       y ∈ [originalHeight, originalHeight+size)
        // 圆角仅裁剪外凸角；凹角保留。
        int colMinX = originalWidth, colMaxX = originalWidth + size, colMinY = originalHeight, colMaxY = textureHeight;
        int rowMinX = originalWidth, rowMaxX = textureWidth, rowMinY = originalHeight, rowMaxY = originalHeight + size;

        System.Func<int, int, bool> inUnion = (x, y) =>
            IsInsideRect(x, y, colMinX, colMinY, colMaxX, colMaxY) ||
            IsInsideRect(x, y, rowMinX, rowMinY, rowMaxX, rowMaxY);

        // 左边一列
        for (int y = colMinY; y < colMaxY; y++)
        {
            for (int x = colMinX; x < colMaxX; x++)
            {
                if (ShouldClipUnionCorner(x, y, colMinX, colMinY, colMaxX, colMaxY, BLOCK_CORNER_RADIUS_PX, inUnion)) continue;
                pixels[y * textureWidth + x] = color;
            }
        }

        // 底部一行
        int bottomLayerStart = originalHeight;
        for (int y = rowMinY; y < rowMaxY; y++)
        {
            for (int x = rowMinX; x < rowMaxX; x++)
            {
                if (ShouldClipUnionCorner(x, y, rowMinX, rowMinY, rowMaxX, rowMaxY, BLOCK_CORNER_RADIUS_PX, inUnion)) continue;
                pixels[y * textureWidth + x] = color;
            }
        }
        texture.SetPixels(pixels);
        texture.Apply();

        string path = $"Assets/Sprites/L{blocks}Shape.png";
        byte[] pngData = texture.EncodeToPNG();
        File.WriteAllBytes(path, pngData);
        AssetDatabase.ImportAsset(path);

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.spritePixelsPerUnit = BLOCK_SPRITE_PPU;
            importer.textureType = TextureImporterType.Sprite;
            importer.filterMode = FilterMode.Point;
            importer.spriteImportMode = SpriteImportMode.Single;



            AssetDatabase.WriteImportSettingsIfDirty(path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // 创建等长L形Sprite（图形绘制在右上角）
    private static Sprite CreateEqualLShapeSprite(Color color)
    {
        int size = 64;
        int originalWidth = 3 * size;  // 3格宽
        int originalHeight = 3 * size; // 3格高

        // 创建更大的纹理，把等长L形绘制在右上角
        int textureWidth = originalWidth * 2;
        int textureHeight = originalHeight * 2;

        Texture2D texture = new Texture2D(textureWidth, textureHeight);
        Color[] pixels = new Color[textureWidth * textureHeight];

        // 透明背景
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }

        // 在右上角绘制正确等长L形：左边一列(3格高) + 底部水平一排(3格宽)
        // 圆角仅裁剪外凸角；凹角保留。
        int colMinX = originalWidth, colMaxX = originalWidth + size, colMinY = originalHeight, colMaxY = textureHeight;
        int rowMinX = originalWidth, rowMaxX = textureWidth, rowMinY = originalHeight, rowMaxY = originalHeight + size;

        System.Func<int, int, bool> inUnion = (x, y) =>
            IsInsideRect(x, y, colMinX, colMinY, colMaxX, colMaxY) ||
            IsInsideRect(x, y, rowMinX, rowMinY, rowMaxX, rowMaxY);

        // 左边一列
        for (int y = colMinY; y < colMaxY; y++)
        {
            for (int x = colMinX; x < colMaxX; x++)
            {
                if (ShouldClipUnionCorner(x, y, colMinX, colMinY, colMaxX, colMaxY, BLOCK_CORNER_RADIUS_PX, inUnion)) continue;
                pixels[y * textureWidth + x] = color;
            }
        }

        // 底部水平一排
        int bottomLayerStart = originalHeight;
        for (int y = rowMinY; y < rowMaxY; y++)
        {
            for (int x = rowMinX; x < rowMaxX; x++)
            {
                if (ShouldClipUnionCorner(x, y, rowMinX, rowMinY, rowMaxX, rowMaxY, BLOCK_CORNER_RADIUS_PX, inUnion)) continue;
                pixels[y * textureWidth + x] = color;
            }
        }
        texture.SetPixels(pixels);
        texture.Apply();

        string path = "Assets/Sprites/L5Shape_Equal.png";
        byte[] pngData = texture.EncodeToPNG();
        File.WriteAllBytes(path, pngData);
        AssetDatabase.ImportAsset(path);

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.spritePixelsPerUnit = BLOCK_SPRITE_PPU;
            importer.textureType = TextureImporterType.Sprite;
            importer.filterMode = FilterMode.Point;
            importer.spriteImportMode = SpriteImportMode.Single;



            AssetDatabase.WriteImportSettingsIfDirty(path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // 创建横条Sprite（图形绘制在右上角）
    private static Sprite CreateLineSprite(int blocks, Color color)
    {
        int size = 64;
        int originalWidth = blocks * size;
        int originalHeight = size;

        // 创建更大的纹理，把横条绘制在右上角
        int textureWidth = originalWidth * 2;
        int textureHeight = originalHeight * 2;

        Texture2D texture = new Texture2D(textureWidth, textureHeight);
        Color[] pixels = new Color[textureWidth * textureHeight];

        // 透明背景
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }

        // 在右上角绘制横条
        for (int y = originalHeight; y < textureHeight; y++)
        {
            for (int x = originalWidth; x < textureWidth; x++)
            {
                if (IsClippedByCornerRadius(x, y, originalWidth, originalHeight, textureWidth, textureHeight, BLOCK_CORNER_RADIUS_PX)) continue;
                pixels[y * textureWidth + x] = color;
            }
        }
        texture.SetPixels(pixels);
        texture.Apply();

        string path = $"Assets/Sprites/Line{blocks}.png";
        byte[] pngData = texture.EncodeToPNG();
        File.WriteAllBytes(path, pngData);
        AssetDatabase.ImportAsset(path);

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.spritePixelsPerUnit = BLOCK_SPRITE_PPU;
            importer.textureType = TextureImporterType.Sprite;
            importer.filterMode = FilterMode.Point;
            importer.spriteImportMode = SpriteImportMode.Single;



            AssetDatabase.WriteImportSettingsIfDirty(path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // 保存Prefab
    private static void SavePrefab(GameObject go, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Debug.Log($"✅ 已创建: {path}");
    }

    private static void CreateHighlightChild(GameObject parent, SpriteRenderer baseRenderer, Sprite highlightSprite)
    {
        if (parent == null || baseRenderer == null || highlightSprite == null) return;

        GameObject child = new GameObject("Highlight");
        child.transform.SetParent(parent.transform, false);

        SpriteRenderer sr = child.AddComponent<SpriteRenderer>();
        sr.sprite = highlightSprite;
        sr.sortingLayerID = baseRenderer.sortingLayerID;
        sr.sortingOrder = baseRenderer.sortingOrder + 1;
        sr.color = BLOCK_BASE_WHITE;

        // Keep highlight purely visual.
        child.layer = parent.layer;
    }

    private static bool IsInsideRect(int x, int y, int minX, int minY, int maxXExclusive, int maxYExclusive)
    {
        return x >= minX && x < maxXExclusive && y >= minY && y < maxYExclusive;
    }

    private static bool IsBorderPixel(int x, int y, int minX, int minY, int maxXExclusive, int maxYExclusive, int inset)
    {
        if (!IsInsideRect(x, y, minX, minY, maxXExclusive, maxYExclusive)) return false;

        bool inner = IsInsideRect(
            x,
            y,
            minX + inset,
            minY + inset,
            maxXExclusive - inset,
            maxYExclusive - inset);

        return !inner;
    }

    private static bool IsOutlinePixel(System.Func<int, int, bool> isFilled, int x, int y)
    {
        if (!isFilled(x, y)) return false;

        // 4-neighborhood outline (avoids diagonal seams)
        return !isFilled(x - 1, y)
            || !isFilled(x + 1, y)
            || !isFilled(x, y - 1)
            || !isFilled(x, y + 1);
    }

    private static Sprite CreateMaskInsetOutlineSprite(
        int textureWidth,
        int textureHeight,
        System.Func<int, int, bool> isFilled,
        int insetPx,
        Color color,
        string path)
    {
        Texture2D texture = new Texture2D(textureWidth, textureHeight);
        Color[] pixels = new Color[textureWidth * textureHeight];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

        // Precompute outline positions.
        bool[] outline = new bool[textureWidth * textureHeight];
        for (int y = 0; y < textureHeight; y++)
        {
            for (int x = 0; x < textureWidth; x++)
            {
                outline[y * textureWidth + x] = IsOutlinePixel(isFilled, x, y);
            }
        }

        int insetSq = insetPx * insetPx;
        for (int y = 0; y < textureHeight; y++)
        {
            for (int x = 0; x < textureWidth; x++)
            {
                if (!isFilled(x, y)) continue;

                // Find the nearest outline pixel within inset range.
                bool inBand = false;
                int minY = Mathf.Max(0, y - insetPx);
                int maxY = Mathf.Min(textureHeight - 1, y + insetPx);
                int minX = Mathf.Max(0, x - insetPx);
                int maxX = Mathf.Min(textureWidth - 1, x + insetPx);

                for (int yy = minY; yy <= maxY && !inBand; yy++)
                {
                    int dy = yy - y;
                    int dySq = dy * dy;
                    for (int xx = minX; xx <= maxX; xx++)
                    {
                        if (!outline[yy * textureWidth + xx]) continue;
                        int dx = xx - x;
                        if (dx * dx + dySq <= insetSq)
                        {
                            inBand = true;
                            break;
                        }
                    }
                }

                if (inBand)
                {
                    pixels[y * textureWidth + x] = color;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        byte[] pngData = texture.EncodeToPNG();
        File.WriteAllBytes(path, pngData);
        AssetDatabase.ImportAsset(path);

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.spritePixelsPerUnit = BLOCK_SPRITE_PPU;
            importer.textureType = TextureImporterType.Sprite;
            importer.filterMode = FilterMode.Point;
            importer.spriteImportMode = SpriteImportMode.Single;
            AssetDatabase.WriteImportSettingsIfDirty(path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Sprite CreateSquareHighlightSprite(int width, int height, Color color)
    {
        int textureWidth = width * 2;
        int textureHeight = height * 2;

        int minX = width;
        int minY = height;
        int maxX = textureWidth;
        int maxY = textureHeight;

        Texture2D texture = new Texture2D(textureWidth, textureHeight);
        Color[] pixels = new Color[textureWidth * textureHeight];

        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

        for (int y = minY; y < maxY; y++)
        {
            for (int x = minX; x < maxX; x++)
            {
                if (IsClippedByCornerRadius(x, y, minX, minY, maxX, maxY, BLOCK_CORNER_RADIUS_PX)) continue;
                if (IsBorderPixel(x, y, minX, minY, maxX, maxY, BLOCK_HIGHLIGHT_INSET_PX))
                {
                    pixels[y * textureWidth + x] = color;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        string path = $"Assets/Sprites/SquareHighlight_{width}x{height}.png";
        byte[] pngData = texture.EncodeToPNG();
        File.WriteAllBytes(path, pngData);
        AssetDatabase.ImportAsset(path);

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.spritePixelsPerUnit = BLOCK_SPRITE_PPU;
            importer.textureType = TextureImporterType.Sprite;
            importer.filterMode = FilterMode.Point;
            importer.spriteImportMode = SpriteImportMode.Single;
            AssetDatabase.WriteImportSettingsIfDirty(path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Sprite CreateLShapeHighlightSprite(int blocks, Color color)
    {
        int size = 64;
        int originalWidth = 2 * size;
        int originalHeight = blocks * size;

        int textureWidth = originalWidth * 2;
        int textureHeight = originalHeight * 2;

        // Two rectangles forming the L (in the top-right quadrant)
        // 1) Vertical bar: [originalWidth, originalWidth+size) x [originalHeight, textureHeight)
        int vMinX = originalWidth;
        int vMaxX = originalWidth + size;
        int vMinY = originalHeight;
        int vMaxY = textureHeight;

        // 2) Bottom bar: [originalWidth, textureWidth) x [originalHeight, originalHeight+size)
        int hMinX = originalWidth;
        int hMaxX = textureWidth;
        int hMinY = originalHeight;
        int hMaxY = originalHeight + size;

        System.Func<int, int, bool> inUnion = (x, y) =>
            IsInsideRect(x, y, vMinX, vMinY, vMaxX, vMaxY) ||
            IsInsideRect(x, y, hMinX, hMinY, hMaxX, hMaxY);

        System.Func<int, int, bool> isFilled = (x, y) =>
        {
            if (!inUnion(x, y)) return false;
            // 在列内 且 BB 角被 union 外象限裁剪 → 删除。凹角依靠 inUnion 判定被保留。
            if (IsInsideRect(x, y, vMinX, vMinY, vMaxX, vMaxY) &&
                ShouldClipUnionCorner(x, y, vMinX, vMinY, vMaxX, vMaxY, BLOCK_CORNER_RADIUS_PX, inUnion)) return false;
            if (IsInsideRect(x, y, hMinX, hMinY, hMaxX, hMaxY) &&
                ShouldClipUnionCorner(x, y, hMinX, hMinY, hMaxX, hMaxY, BLOCK_CORNER_RADIUS_PX, inUnion)) return false;
            return true;
        };

        string path = $"Assets/Sprites/L{blocks}Shape_Highlight.png";
        return CreateMaskInsetOutlineSprite(textureWidth, textureHeight, isFilled, BLOCK_HIGHLIGHT_INSET_PX, color, path);
    }

    private static Sprite CreateEqualLShapeHighlightSprite(Color color)
    {
        int size = 64;
        int originalWidth = 3 * size;
        int originalHeight = 3 * size;

        int textureWidth = originalWidth * 2;
        int textureHeight = originalHeight * 2;

        // Vertical bar: [originalWidth, originalWidth+size) x [originalHeight, textureHeight)
        int vMinX = originalWidth;
        int vMaxX = originalWidth + size;
        int vMinY = originalHeight;
        int vMaxY = textureHeight;

        // Bottom bar: [originalWidth, textureWidth) x [originalHeight, originalHeight+size)
        int hMinX = originalWidth;
        int hMaxX = textureWidth;
        int hMinY = originalHeight;
        int hMaxY = originalHeight + size;

        System.Func<int, int, bool> inUnion = (x, y) =>
            IsInsideRect(x, y, vMinX, vMinY, vMaxX, vMaxY) ||
            IsInsideRect(x, y, hMinX, hMinY, hMaxX, hMaxY);

        System.Func<int, int, bool> isFilled = (x, y) =>
        {
            if (!inUnion(x, y)) return false;
            if (IsInsideRect(x, y, vMinX, vMinY, vMaxX, vMaxY) &&
                ShouldClipUnionCorner(x, y, vMinX, vMinY, vMaxX, vMaxY, BLOCK_CORNER_RADIUS_PX, inUnion)) return false;
            if (IsInsideRect(x, y, hMinX, hMinY, hMaxX, hMaxY) &&
                ShouldClipUnionCorner(x, y, hMinX, hMinY, hMaxX, hMaxY, BLOCK_CORNER_RADIUS_PX, inUnion)) return false;
            return true;
        };

        string path = "Assets/Sprites/L5Shape_Equal_Highlight.png";
        return CreateMaskInsetOutlineSprite(textureWidth, textureHeight, isFilled, BLOCK_HIGHLIGHT_INSET_PX, color, path);
    }

    private static Sprite CreateLineHighlightSprite(int blocks, Color color)
    {
        int size = 64;
        int originalWidth = blocks * size;
        int originalHeight = size;

        int textureWidth = originalWidth * 2;
        int textureHeight = originalHeight * 2;

        int minX = originalWidth;
        int minY = originalHeight;
        int maxX = textureWidth;
        int maxY = textureHeight;

        Texture2D texture = new Texture2D(textureWidth, textureHeight);
        Color[] pixels = new Color[textureWidth * textureHeight];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

        for (int y = minY; y < maxY; y++)
        {
            for (int x = minX; x < maxX; x++)
            {
                if (IsClippedByCornerRadius(x, y, minX, minY, maxX, maxY, BLOCK_CORNER_RADIUS_PX)) continue;
                if (IsBorderPixel(x, y, minX, minY, maxX, maxY, BLOCK_HIGHLIGHT_INSET_PX))
                {
                    pixels[y * textureWidth + x] = color;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        string path = $"Assets/Sprites/Line{blocks}_Highlight.png";
        byte[] pngData = texture.EncodeToPNG();
        File.WriteAllBytes(path, pngData);
        AssetDatabase.ImportAsset(path);

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.spritePixelsPerUnit = BLOCK_SPRITE_PPU;
            importer.textureType = TextureImporterType.Sprite;
            importer.filterMode = FilterMode.Point;
            importer.spriteImportMode = SpriteImportMode.Single;
            AssetDatabase.WriteImportSettingsIfDirty(path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static PhysicsMaterial2D GetOrCreateBlockPhysicsMaterial()
    {
        PhysicsMaterial2D material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(BLOCK_PHYSICS_MATERIAL_PATH);
        if (material != null)
        {
            material.friction = DEFAULT_BLOCK_FRICTION;
            material.bounciness = DEFAULT_BLOCK_BOUNCINESS;
            EditorUtility.SetDirty(material);
            return material;
        }

        material = new PhysicsMaterial2D("BlockPhysicsMaterial")
        {
            friction = DEFAULT_BLOCK_FRICTION,
            bounciness = DEFAULT_BLOCK_BOUNCINESS
        };

        AssetDatabase.CreateAsset(material, BLOCK_PHYSICS_MATERIAL_PATH);
        AssetDatabase.ImportAsset(BLOCK_PHYSICS_MATERIAL_PATH);
        return material;
    }

    // 创建六边形球Prefab
    private static void CreateHexagonBallPrefab()
    {
        GameObject go = new GameObject("HexagonBall");

        // 设置Tag（用于左右边界触发GameOver）
        // 注意：Unity 的 Tag 必须在项目里预先创建，否则这里会抛异常。
        go.tag = "HexagonBall";

        // Sprite
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CreateHexagonSprite();
        sr.sortingOrder = 1; // 确保在方块上方

        // PolygonCollider2D (六边形)
        PolygonCollider2D collider = go.AddComponent<PolygonCollider2D>();
        collider.points = CreateHexagonColliderPoints();

        // Rigidbody2D - Dynamic (会受物理影响)
        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.mass = 1f;
        rb.gravityScale = 1f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // HexagonBall脚本
        go.AddComponent<HexagonBall>();

        // 设置Layer
        go.layer = LayerMask.NameToLayer("HexagonBall");

        SavePrefab(go, "Assets/Prefabs/HexagonBall.prefab");
        Object.DestroyImmediate(go);
    }

    // 创建六边形Sprite
    private static Sprite CreateHexagonSprite()
    {
        const float HEXAGON_DIAMETER = 2.0f; // 直径=2个方格
        int textureSize = 256;
        float radius = textureSize / 2f * 0.9f; // 略小于纹理一半，留出边缘

        Texture2D texture = new Texture2D(textureSize, textureSize);
        Color[] pixels = new Color[textureSize * textureSize];

        Vector2 center = new Vector2(textureSize / 2f, textureSize / 2f);

        // 透明背景
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }

        // 绘制六边形
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                Vector2 point = new Vector2(x, y);
                if (IsInsideHexagon(point, center, radius))
                {
                    pixels[y * textureSize + x] = Color.white;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        string path = "Assets/Sprites/HexagonBall.png";
        byte[] pngData = texture.EncodeToPNG();
        File.WriteAllBytes(path, pngData);
        AssetDatabase.ImportAsset(path);

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.spritePixelsPerUnit = textureSize / HEXAGON_DIAMETER; // 128 PPU，直径=2个方格
            importer.textureType = TextureImporterType.Sprite;
            importer.filterMode = FilterMode.Bilinear;
            importer.spriteImportMode = SpriteImportMode.Single;
            AssetDatabase.WriteImportSettingsIfDirty(path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // 创建六边形碰撞器的点
    private static Vector2[] CreateHexagonColliderPoints()
    {
        const float HEXAGON_DIAMETER = 2.0f; // 直径=2个方格
        const float COLLIDER_RADIUS_SCALE = 0.90f; // 碰撞器略小于渲染，避免“碰撞偏大”的感觉
        float radius = (HEXAGON_DIAMETER / 2f) * COLLIDER_RADIUS_SCALE;

        Vector2[] points = new Vector2[6];
        for (int i = 0; i < 6; i++)
        {
            float angle = Mathf.PI / 3f * i; // 60度间隔
            points[i] = new Vector2(
                radius * Mathf.Cos(angle),
                radius * Mathf.Sin(angle)
            );
        }
        return points;
    }

    // 判断点是否在六边形内
    private static bool IsInsideHexagon(Vector2 point, Vector2 center, float radius)
    {
        Vector2 diff = point - center;
        float distance = diff.magnitude;

        if (distance > radius) return false;

        // 六边形的六个边界
        for (int i = 0; i < 6; i++)
        {
            float angle = Mathf.PI / 3f * i;
            Vector2 normal = new Vector2(Mathf.Cos(angle + Mathf.PI / 6f), Mathf.Sin(angle + Mathf.PI / 6f));
            float edgeDistance = radius * Mathf.Cos(Mathf.PI / 6f);

            if (Vector2.Dot(diff, normal) > edgeDistance)
                return false;
        }

        return true;
    }
}
