# 预制体(Prefab)创建指南

## 架构说明

新的系统架构更加清晰和可扩展：

```
TowerBlock (基类)
├── 单格方块 Prefab
├── 正方形(2x2) Prefab
├── L3型 Prefab
├── L4型 Prefab
├── L5型 Prefab
└── I型(4格) Prefab

HexagonBall (独立类)
└── 六边形球 Prefab

TowerBuilder (管理器)
└── 使用预制体拼装塔
```

---

## 步骤1：创建方块预制体

### 1.1 单格方块 (SingleBlock)

1. **Hierarchy** → 右键 → Create Empty → 命名为 `SingleBlock`
2. 添加组件：
   - `SpriteRenderer` - 设置方块的Sprite和颜色
   - `BoxCollider2D` - Size: (0.95, 0.95) **重要：比Sprite略小，留出间隙**
   - `Rigidbody2D` - Body Type: Kinematic
   - `TowerBlock` 脚本
3. **TowerBlock 配置**：
   - Block Type Name: "单格方块"
   - Score Value: 10
   - Is Static: ✓
4. **Transform**：
   - Position: (0, 0, 0)
   - Scale: (1, 1, 1)
5. **Sprite 配置关键**：
   - 如果使用 64x64 像素的Sprite
   - **Pixels Per Unit 必须设为 64**（这样Sprite在世界坐标中正好是1x1单位）
   - Pivot: Center (0.5, 0.5)
6. 拖拽到 `Assets/Prefabs/Blocks/` 创建Prefab

**碰撞器大小控制要点**：
- Sprite实际显示大小 = (纹理像素宽度 ÷ Pixels Per Unit) × Transform.Scale
- 例如：64像素 ÷ 64 PPU × 1 Scale = 1单位
- BoxCollider2D.Size 应该略小于Sprite大小（0.95）避免方块贴太紧

### 1.2 正方形 (SquareBlock 2x2)

1. **Hierarchy** → Create Empty → `SquareBlock`
2. 添加组件：
   - `SpriteRenderer` - Sprite应该是2x2大小（128x128像素，PPU=64）
   - `BoxCollider2D` - Size: (1.9, 1.9) **注意：2x2方块的碰撞器设为1.9留间隙**
   - `Rigidbody2D` - Body Type: Kinematic, Mass: 4
   - `TowerBlock` 脚本
3. **TowerBlock 配置**：
   - Block Type Name: "正方形方块"
   - Score Value: 40
4. **Sprite 尺寸**：
   - 128x128 像素纹理 ÷ 64 PPU = 2x2单位
   - 或者使用 64x64 像素 + Transform.Scale (2, 2, 1)
5. 创建Prefab

### 1.3 L3型方块 (L3Block)

形状：
```
█
██
```

1. Create Empty → `L3Block`
2. 组件配置：
   - `SpriteRenderer` - L3形状的Sprite
   - `PolygonCollider2D` - 自动生成轮廓
   - `Rigidbody2D` - Mass: 3
   - `TowerBlock` 脚本
3. **TowerBlock 配置**：
   - Block Type Name: "L3方块"
   - Score Value: 30

### 1.4 L4型方块 (L4Block)

形状：
```
█
█
██
```

类似L3的配置，Mass: 4, Score: 40

### 1.5 L5型方块 (L5Block)

形状：
```
█
█
█
██
```

Mass: 5, Score: 50

### 1.6 I型方块 (LineBlock 4格)

形状：
```
████
```

1. Create Empty → `LineBlock`
2. 组件配置：
   - `SpriteRenderer` - 4格横条Sprite
   - `BoxCollider2D` - Size: (4, 1)
   - `Rigidbody2D` - Mass: 4
   - `TowerBlock` 脚本
3. Score: 40

---

## 步骤2：创建六边形球预制体

### 2.1 HexagonBall Prefab

1. **Hierarchy** → Create Empty → `HexagonBall`
2. 添加组件：
   - `SpriteRenderer` - 六边形Sprite（使用 HexagonBallFactory 生成）
   - `PolygonCollider2D` - 6个点的六边形
   - `Rigidbody2D` - Body Type: Dynamic, Mass: 1
   - `HexagonBall` 脚本（已有）
3. **Layer**: HexagonBall
4. 创建Prefab到 `Assets/Prefabs/`

---

## 步骤3：配置 TowerBuilder

1. **Hierarchy** → Create Empty → `TowerBuilder`
2. 添加 `TowerBuilder` 组件
3. **配置 Inspector**：
   - Tower Layers: 8
   - Layer Width: 8
   - Layer Spacing: 1
   - Start Height: -3
4. **拖拽预制体到对应槽位**：
   - Single Block Prefab → 单格方块Prefab
   - Square Block Prefab → 正方形Prefab
   - L3 Block Prefab → L3Prefab
   - L4 Block Prefab → L4Prefab
   - L5 Block Prefab → L5Prefab
   - Line Block Prefab → I型Prefab
   - Hexagon Ball Prefab → 六边形球Prefab
5. **六边形球配置**：
   - Spawn Hexagon Ball: ✓
   - Ball Height Offset: 1.5

---

## 步骤4：创建Sprite资源

### 4.1 使用Unity内置形状（临时）

在 `Assets/Sprites/` 创建：
- Square (64x64) - 白色正方形
- 使用 Sprite Editor 创建不同形状

### 4.2 或使用代码生成（推荐用于原型）

可以创建一个 `PrefabGenerator` 工具脚本自动生成：

```csharp
// 在编辑器中执行
[MenuItem("Tools/Generate Block Prefabs")]
public static void GenerateBlockPrefabs()
{
    // 使用 BlockFactory.CreateBlockSprite() 
    // 为每个形状创建对应的Sprite和Prefab
}
```

### 4.3 **控制大小的关键公式**

```
世界坐标大小 = (纹理像素尺寸 ÷ Pixels Per Unit) × Transform.Scale

示例1: 单格方块
- 纹理: 64x64 像素
- PPU: 64
- Scale: (1, 1, 1)
- 结果: (64÷64)×1 = 1×1 单位 ✓

示例2: 正方形方块
- 纹理: 128x128 像素
- PPU: 64
- Scale: (1, 1, 1)
- 结果: (128÷64)×1 = 2×2 单位 ✓

示例3: 另一种方式（不推荐）
- 纹理: 64x64 像素
- PPU: 64
- Scale: (2, 2, 1)
- 结果: (64÷64)×2 = 2×2 单位
```

**推荐做法**：
- 保持 Transform.Scale = (1, 1, 1)
- 通过调整纹理大小和 PPU 来控制显示大小
- 统一 PPU = 64，这样计算简单

### 4.4 碰撞器调整技巧

当发现碰撞器比Sprite大：

1. **检查 SpriteRenderer**：
   - 选中GameObject
   - 查看 Scene 视图中的绿色边框（Sprite边界）
   
2. **检查 BoxCollider2D**：
   - 查看青色边框（碰撞器边界）
   - 如果碰撞器比Sprite大，手动调整 Size

3. **Auto Tiling 问题**：
   - 如果使用了 Auto Tiling，禁用它
   - 手动设置 BoxCollider2D.Size

4. **精确对齐**：
   ```
   Sprite显示大小: 1×1 单位
   → BoxCollider2D.Size: (0.95, 0.95)
   
   Sprite显示大小: 2×2 单位
   → BoxCollider2D.Size: (1.9, 1.9)
   ```

---

## 重要规范

### 统一标准
1. **所有方块的基准尺寸**：1单位 = 1格
2. **Pivot点**：所有Sprite的Pivot设为Center (0.5, 0.5)
3. **Pixels Per Unit**：统一为 64
4. **Layer**：所有方块使用 "Block" 层
5. **Sorting Layer**：统一使用 Default

### 物理参数
- **Collision Detection**: Continuous
- **Linear Drag**: 0.5
- **Angular Drag**: 1.0
- **Gravity Scale**: 1.0

### 碰撞层设置
确保 Physics2D 碰撞矩阵配置正确：
- Block ↔ Block: ✓
- Block ↔ HexagonBall: ✓
- Block ↔ Boundary: ✓

---

## 优势

这个新架构的优势：

1. ✅ **清晰的继承关系** - TowerBlock基类统一管理
2. ✅ **预制体系统** - 美术和策划可以直接调整Prefab
3. ✅ **易于扩展** - 添加新形状只需创建新Prefab
4. ✅ **统一坐标系** - 所有方块使用相同的尺寸标准
5. ✅ **解耦设计** - TowerBuilder只负责拼装，不负责创建
6. ✅ **事件驱动** - 通过事件通信，松耦合

---

## 下一步

1. 创建上述所有Prefab
2. 配置TowerBuilder
3. 运行游戏测试
4. 根据需要调整物理参数和得分

---

**注意**：旧的脚本（Block.cs, BlockShape.cs, BlockShapeGroup.cs等）可以保留作为参考，或在确认新系统工作正常后删除。
