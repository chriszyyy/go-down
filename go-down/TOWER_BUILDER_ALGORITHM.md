# TowerBuilder 智能拼装算法设计文档

## 概述

TowerBuilder是游戏的核心系统，负责使用Prefab智能拼装塔结构。关键特性：
- 支持方块旋转（0°/90°/180°/270°）
- 动态宽度适配
- 间隙最小化
- 可选对称布局

---

## 算法需求

### 输入参数
- `layerCount`: 塔的总层数（如8层）
- `layerWidth`: 每层的宽度（如8个单位）
- `startHeight`: 起始Y坐标（如-3）
- `layerSpacing`: 层间距（如1单位）
- `prefabs[]`: 6种方块Prefab数组

### 输出
- 在场景中生成完整的塔结构
- 每个方块正确放置和旋转
- 六边形球放置在塔顶

---

## 核心算法流程

```
BuildTower():
├── 1. 初始化塔结构
├── 2. 遍历每一层
│   ├── 2.1 计算当前层Y坐标
│   ├── 2.2 调用 FillLayer()
│   └── 2.3 激活物理（除底层外）
├── 3. 生成六边形球
└── 4. 返回成功/失败

FillLayer(layerIndex, yPos):
├── 1. 初始化 remainingWidth = layerWidth
├── 2. 初始化 currentX = -layerWidth/2
├── 3. while remainingWidth > 0.1:
│   ├── 3.1 SelectBestPrefab(remainingWidth)
│   ├── 3.2 TryAllRotations(prefab, remainingWidth)
│   ├── 3.3 PlaceBlock(prefab, x, y, rotation)
│   └── 3.4 更新 currentX 和 remainingWidth
└── 4. 返回填充成功/失败
```

---

## 详细设计

### 1. 方块选择策略 (SelectBestPrefab)

```csharp
GameObject SelectBestPrefab(float remainingWidth)
{
    // 优先级：
    // 1. 完美匹配（宽度正好等于remainingWidth）
    // 2. 最接近但不超过remainingWidth的最大方块
    // 3. 随机选择小方块
    
    List<Candidate> candidates = new List<Candidate>();
    
    foreach (prefab in allPrefabs)
    {
        foreach (rotation in [0, 90, 180, 270])
        {
            float width = GetWidthAfterRotation(prefab, rotation);
            
            if (width <= remainingWidth)
            {
                candidates.Add(new Candidate {
                    prefab = prefab,
                    rotation = rotation,
                    width = width,
                    fitScore = CalculateFitScore(width, remainingWidth)
                });
            }
        }
    }
    
    // 按fitScore排序，选择最优
    return candidates.OrderByDescending(c => c.fitScore).First();
}

float CalculateFitScore(float blockWidth, float remainingWidth)
{
    // 完美匹配得分最高
    if (Mathf.Abs(blockWidth - remainingWidth) < 0.1f)
        return 1000f;
    
    // 大方块优先（减少碎片）
    float sizeScore = blockWidth / layerWidth * 100f;
    
    // 间隙惩罚
    float gapPenalty = (remainingWidth - blockWidth) * 10f;
    
    return sizeScore - gapPenalty;
}
```

### 2. 旋转计算 (GetWidthAfterRotation)

```csharp
float GetWidthAfterRotation(GameObject prefab, float angle)
{
    // 获取方块的原始尺寸
    Bounds bounds = GetPrefabBounds(prefab);
    float originalWidth = bounds.size.x;
    float originalHeight = bounds.size.y;
    
    // 根据旋转角度计算新宽度
    switch (angle)
    {
        case 0:
        case 180:
            return originalWidth;
        case 90:
        case 270:
            return originalHeight; // 宽高互换
        default:
            return originalWidth;
    }
}

Bounds GetPrefabBounds(GameObject prefab)
{
    // 方法1：从Collider获取
    Collider2D collider = prefab.GetComponent<Collider2D>();
    if (collider != null)
        return collider.bounds;
    
    // 方法2：从SpriteRenderer获取
    SpriteRenderer sr = prefab.GetComponent<SpriteRenderer>();
    if (sr != null)
        return sr.bounds;
    
    // 方法3：手动配置（最准确）
    TowerBlock block = prefab.GetComponent<TowerBlock>();
    return new Bounds(Vector3.zero, new Vector3(block.width, block.height, 1f));
}
```

### 3. 方块放置 (PlaceBlock)

```csharp
void PlaceBlock(GameObject prefab, float x, float y, float rotation)
{
    // 1. 实例化Prefab
    GameObject block = Instantiate(prefab, transform);
    
    // 2. 计算位置（考虑pivot和旋转后的偏移）
    Vector3 position = CalculatePosition(prefab, x, y, rotation);
    block.transform.position = position;
    
    // 3. 应用旋转
    block.transform.rotation = Quaternion.Euler(0, 0, rotation);
    
    // 4. 设置Layer
    block.layer = LayerMask.NameToLayer("Block");
    
    // 5. 注册到列表
    allBlocks.Add(block);
    layerBlocks[currentLayer].Add(block);
}

Vector3 CalculatePosition(GameObject prefab, float x, float y, float rotation)
{
    // 获取方块旋转后的实际宽度
    float width = GetWidthAfterRotation(prefab, rotation);
    
    // Pivot在中心，所以X坐标需要加上半个宽度
    float offsetX = width / 2f;
    
    return new Vector3(x + offsetX, y, 0);
}
```

---

## 特殊情况处理

### 1. 间隙处理
```csharp
// 如果剩余宽度太小（<0.5单位），跳过
if (remainingWidth < 0.5f)
{
    Debug.LogWarning($"Layer {layerIndex} has gap: {remainingWidth}");
    break; // 跳出循环，接受小间隙
}
```

### 2. 无限循环保护
```csharp
int maxIterations = 20;
int iterations = 0;

while (remainingWidth > 0.1f && iterations < maxIterations)
{
    iterations++;
    // ... 放置逻辑
}

if (iterations >= maxIterations)
{
    Debug.LogError("FillLayer failed: too many iterations");
}
```

### 3. 对称布局（可选）
```csharp
bool enableSymmetry = true;

if (enableSymmetry && currentX < 0)
{
    // 在负X轴放置后，镜像到正X轴
    Vector3 mirrorPos = new Vector3(-block.transform.position.x, y, 0);
    GameObject mirrorBlock = Instantiate(prefab, mirrorPos, block.transform.rotation);
}
```

---

## 数据结构

### TowerBlock扩展属性
```csharp
public class TowerBlock : MonoBehaviour
{
    // 现有属性
    public string blockTypeName;
    public int scoreValue;
    public bool isStatic;
    
    // 新增属性（用于算法）
    public float width = 1f;  // 原始宽度（未旋转）
    public float height = 1f; // 原始高度（未旋转）
    
    // 辅助方法
    public Vector2 GetRotatedSize(float angle)
    {
        if (angle == 90 || angle == 270)
            return new Vector2(height, width); // 宽高互换
        return new Vector2(width, height);
    }
}
```

### Prefab配置建议
```
SingleBlock: width=1, height=1
SquareBlock: width=2, height=2
L3Block: width=2, height=3
L4Block: width=2, height=4
L5Block: width=3, height=3
LineBlock: width=4, height=1
```

---

## 测试用例

### 测试1：简单填充（8宽度）
```
输入: layerWidth = 8
期望输出: 
- 方案1: [LineBlock(4)] + [LineBlock(4)]
- 方案2: [SquareBlock(2)] × 4
- 方案3: [L3Block(2)] + [L3Block(2)] + [LineBlock(4)]
```

### 测试2：旋转必要性
```
输入: layerWidth = 7, 剩余宽度 = 3
期望: 选择L3Block并旋转90°（宽度从2变为3）
```

### 测试3：间隙容忍
```
输入: layerWidth = 8, 最后剩余 = 0.3
期望: 接受间隙，不强制填充
```

---

## 性能优化

1. **Prefab缓存**：预先计算所有Prefab的尺寸表
2. **候选预筛选**：过滤掉明显不合适的Prefab
3. **批量实例化**：使用Unity的批量API
4. **避免重复计算**：缓存旋转后的尺寸

---

## 下一步

1. 在TowerBlock中添加 `width` 和 `height` 属性
2. 实现 `GetWidthAfterRotation()` 方法
3. 实现 `SelectBestPrefab()` 的评分系统
4. 测试简单场景（单层8宽度）
5. 逐步增加复杂度（多层、旋转、对称）
