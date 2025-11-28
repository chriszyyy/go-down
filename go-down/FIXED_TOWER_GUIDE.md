# 固定塔测试场景使用指南

## 快速开始

### 1. 场景设置
在 Unity Scene 中创建一个空 GameObject：
1. 右键 Hierarchy → Create Empty
2. 命名为 `FixedTower`
3. 添加 `FixedTowerManager` 组件

### 2. 配置参数（Inspector 面板）

**塔配置**
- Tower Layers: `8` （塔的层数）
- Blocks Per Layer: `8` （每层方块数）
- Block Size: `1` （方块大小）
- Layer Spacing: `1` （层与层之间的间距）
- Start Height: `-3` （塔底部的起始高度）

**六边形球配置**
- Spawn Hexagon Ball: `✓` （自动生成六边形球）
- Ball Height Offset: `1.5` （球在塔顶上方的高度）

### 3. 运行游戏
点击 Play 按钮，你会看到：
- 一个 8 层的方块塔，每层 8 个方块
- 一个六边形球在塔顶上方

### 4. 测试核心功能

**点击消除方块**
- 鼠标点击任意方块，该方块会消失
- 被点击方块上方的所有方块会受重力影响开始下落

**六边形球物理**
- 球会随着方块消除而下落
- 球需要保持平衡不掉出边界

**观察 Console 日志**
- `方块被消除: Block_L2_X3` - 显示哪个方块被点击
- `激活 2 层以上的方块物理` - 显示哪些方块开始下落

## 与 ShapeTowerManager 的对比

| 特性 | FixedTowerManager | ShapeTowerManager |
|------|-------------------|-------------------|
| 布局 | 固定矩形塔 | 俄罗斯方块形状 |
| 复杂度 | 简单直接 | 复杂智能填充 |
| 用途 | **测试核心功能** | 实际游戏玩法 |
| 方块类型 | 单个方块 | 形状组 |

## 当前测试重点

使用 FixedTowerManager 先完善这些核心功能：
1. ✅ 方块点击检测
2. ✅ 方块消除动画
3. ✅ 物理系统激活
4. ⏳ 六边形球平衡检测
5. ⏳ 游戏结束判定
6. ⏳ 边界检测
7. ⏳ 游戏状态管理

## 下一步

当核心功能测试完成后，可以：
1. 切换回 ShapeTowerManager 使用俄罗斯方块形状
2. 或者基于 FixedTowerManager 继续完善其他功能
