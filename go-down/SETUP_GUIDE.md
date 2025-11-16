# Unity 项目设置指南

**项目名称：** 六边形平衡消除游戏  
**Unity 版本：** 2022.3+ (LTS)  
**最后更新：** 2025-11-16

---

## 📋 目录

1. [Physics2D 参数设置](#1-physics2d-参数设置)
2. [图层配置](#2-图层配置-layers)
3. [主场景创建](#3-主场景创建-maingamescene)
4. [场景基础对象设置](#4-场景基础对象设置)
5. [验证清单](#5-验证清单)

---

## 1. Physics2D 参数设置

### 步骤：
`Edit` → `Project Settings` → `Physics 2D`

### 参数配置：

```yaml
Gravity:
  X: 0
  Y: -9.81          # 或 -15 (增强重力，让下落更快)

Default Material: (留空)

Velocity Iterations: 8
Position Iterations: 3

Sleep Threshold: 0.005    # 物体静止阈值
Time to Sleep: 0.5        # 0.5秒后进入休眠

Queries Hit Triggers: ✓   # 勾选
```

### 说明：
- **Gravity Y**: 控制重力强度，`-9.81` 是真实重力，`-15` 让游戏节奏更快
- **Sleep Threshold**: 当物体速度 < 0.005 时会休眠，节省性能
- **Time to Sleep**: 持续静止 0.5 秒后进入休眠状态

---

## 2. 图层配置 (Layers)

### 步骤：
`Edit` → `Project Settings` → `Tags and Layers` → **Layers** 部分

### 创建自定义图层：

```
Layer 6: Block           # 方块层
Layer 7: HexagonBall     # 六边形球层
Layer 8: Boundary        # 边界层
```

### 碰撞矩阵设置：

回到 `Edit` → `Project Settings` → `Physics 2D`，滚动到底部的 **Layer Collision Matrix**

#### 需要勾选的碰撞关系：
```
✓ HexagonBall ↔ Block      (球能碰到方块)
✓ HexagonBall ↔ Boundary   (球能碰到边界)
✓ Block ↔ Block            (方块之间能堆叠)
```

#### 需要取消的碰撞关系：
```
✗ HexagonBall ↔ UI         (去掉)
✗ Block ↔ UI               (去掉)
✗ Block ↔ Boundary         (去掉，方块不需要碰边界)
✗ Boundary ↔ Boundary      (去掉)
✗ 其他不必要的碰撞         (全部去掉)
```

### 为什么这样设置：
- **只保留必要的碰撞** → 提高性能
- **避免 UI 干扰** → 防止按钮影响物理计算
- **清晰的碰撞逻辑** → 更容易调试

---

## 3. 主场景创建 (MainGameScene)

### 步骤：

1. **创建场景**
   - `File` → `New Scene`
   - 选择 **Basic 2D (Built-in)** 模板
   - `File` → `Save As` → `Assets/Scenes/MainGameScene.unity`

2. **为什么选 Built-in 而不是 URP？**
   - 更简单，不需要配置渲染管线
   - 对简单 2D 游戏性能更好
   - 学习曲线低，适合快速开发

---

## 4. 场景基础对象设置

### 4.1 Main Camera 设置

选中 `Main Camera`，在 Inspector 中配置：

```yaml
Transform:
  Position: (0, 5, -10)

Camera Component:
  Projection: Orthographic
  Size: 10                    # 根据游戏画面调整
  
  Environment:
    Background Type: Solid Color
    Background: #1A1A2E       # 深蓝色背景
                              # RGB: (26, 26, 46)
```

**其他推荐背景色：**
```
深蓝色: #1A1A2E  (26, 26, 46)  - 神秘感 ✓
深灰色: #2C2C2C  (44, 44, 44)  - 简约风
深紫色: #1E1E3F  (30, 30, 63)  - 科技感
纯黑色: #000000  (0, 0, 0)     - 极简
```

---

### 4.2 Canvas 创建（UI 根节点）

1. **创建 Canvas**
   - 右键 Hierarchy → `UI` → `Canvas`

2. **Canvas 设置**
   ```yaml
   Canvas Component:
     Render Mode: Screen Space - Overlay
   
   Canvas Scaler Component:
     UI Scale Mode: Scale With Screen Size
     Reference Resolution: 
       X: 1080
       Y: 1920              # 竖屏手机分辨率
     Match: 0.5             # Width 和 Height 平衡
   ```

3. **说明**
   - 竖屏游戏使用 1080x1920
   - Match 0.5 表示宽高平衡适配

---

### 4.3 GameManager 空对象

1. **创建对象**
   - 右键 Hierarchy → `Create Empty`
   - 命名为 `GameManager`
   - Position: `(0, 0, 0)`

2. **用途**
   - 后续会添加 `GameManager.cs` 脚本
   - 管理游戏整体状态和流程

---

### 4.4 边界检测器 (Boundaries)

#### 创建父对象
1. 右键 Hierarchy → `Create Empty`
2. 命名为 `Boundaries`
3. Position: `(0, 0, 0)`

#### 创建左边界
1. 右键 `Boundaries` → `Create Empty` (作为子对象)
2. 命名为 `LeftBoundary`
3. 配置：
   ```yaml
   Transform:
     Position: (-5, 5, 0)
   
   Add Component → Box Collider 2D:
     Size: (1, 20)
   
   Layer: Boundary
   ```

#### 创建右边界
1. 右键 `Boundaries` → `Create Empty` (作为子对象)
2. 命名为 `RightBoundary`
3. 配置：
   ```yaml
   Transform:
     Position: (5, 5, 0)
   
   Add Component → Box Collider 2D:
     Size: (1, 20)
   
   Layer: Boundary
   ```

#### 快速复制技巧
- 创建好 LeftBoundary 后
- 选中它按 `Ctrl+D` (Windows) 或 `Cmd+D` (Mac) 复制
- 重命名为 RightBoundary
- 只需修改 Position X 从 `-5` 改为 `5`

#### 可视化边界（可选，调试用）
如果想在 Scene 视图看到边界：
1. 选中边界对象
2. Add Component → `Sprite Renderer`
3. 设置颜色为半透明红色

**注意：** 实际游戏中可以不显示，只用 Collider 做碰撞检测。

---

### 4.5 最终 Hierarchy 结构

完成后，Hierarchy 应该是这样的：

```
MainGameScene
├── Main Camera
├── GameManager
├── Canvas
│   └── EventSystem (自动创建)
└── Boundaries
    ├── LeftBoundary
    └── RightBoundary
```

---

## 5. 验证清单

完成上述步骤后，请检查：

### Physics 2D
- [ ] Gravity Y 设置为 -9.81 或 -15
- [ ] Sleep Threshold 设置为 0.005
- [ ] 创建了 3 个自定义 Layer (Block, HexagonBall, Boundary)
- [ ] Layer Collision Matrix 只勾选了必要的碰撞

### 场景设置
- [ ] 创建了 MainGameScene.unity
- [ ] Main Camera 背景色为深色
- [ ] Main Camera Size 为 10
- [ ] 创建了 Canvas（1080x1920）
- [ ] 创建了 GameManager 空对象
- [ ] 创建了 Boundaries 及左右边界

### 边界设置
- [ ] LeftBoundary Position (-5, 5, 0)
- [ ] RightBoundary Position (5, 5, 0)
- [ ] 两个边界都有 BoxCollider2D (Size: 1x20)
- [ ] 两个边界 Layer 都设为 Boundary

---

## 6. 下一步

完成以上设置后，可以开始：

1. **创建核心脚本**
   - `GameManager.cs` - 游戏管理器
   - `Block.cs` - 方块脚本
   - `HexagonBall.cs` - 六边形球脚本
   - `BoundaryChecker.cs` - 边界检测器

2. **创建预制体**
   - Block Prefab - 方块预制体
   - HexagonBall Prefab - 六边形预制体

3. **测试基础玩法**
   - 方块点击消除
   - 球的重力下落
   - 边界碰撞检测

---

## 📝 常见问题 (FAQ)

### Q: 找不到 Background 颜色设置？
**A:** 在 Camera 组件的 **Environment** 部分，先将 **Background Type** 设为 **Solid Color**，才会出现 Background 颜色块。

### Q: Layer Collision Matrix 在哪里？
**A:** `Edit` → `Project Settings` → `Physics 2D`，滚动到最底部。

### Q: 为什么要用 Built-in 而不是 URP？
**A:** 对于简单的 2D 物理游戏，Built-in 渲染管线更简单、性能更好。URP 适合需要高级光照和后处理的项目。

### Q: Sleep Threshold 是什么？
**A:** 当物体速度低于这个值时会进入休眠状态，停止物理计算以节省性能。默认 0.005 适合大多数情况。

### Q: 边界的 Size 为什么是 (1, 20)？
**A:** 宽度 1 足够挡住球，高度 20 覆盖整个游戏区域（根据实际调整）。

---

## 🔧 故障排除

### 问题：物体一直抖动，不会休眠
**解决：** 检查 Sleep Threshold 是否太小，尝试增大到 0.01

### 问题：球穿过方块
**解决：** 
1. 检查 Layer Collision Matrix 是否正确勾选
2. 检查物体的 Layer 是否设置正确
3. 增加 Physics 2D 的 Velocity Iterations

### 问题：UI 按钮点击不响应
**解决：** 确保 UI Layer 没有和游戏物体产生物理碰撞

---

## 📚 参考资源

- [Unity 2D Physics 文档](https://docs.unity3d.com/Manual/Physics2DReference.html)
- [Unity Layers 文档](https://docs.unity3d.com/Manual/Layers.html)
- [Unity Canvas 文档](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/UICanvas.html)

---

**设置完成！** 🎉

现在可以开始编写核心游戏逻辑了。参考 `development_tasks.md` 继续后续开发。
