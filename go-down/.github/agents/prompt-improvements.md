# Agent Prompt — 待优化清单

跟踪 [.github/agents/go-down.agent.md](.github/agents/go-down.agent.md) 在实际使用中暴露出来的改进点。每条记录：**症状 → 根因 → 想加的规则**。

完成后把条目移到底部 "已纳入 prompt" 区。

---

## 待办（按优先级排序）

### 1. 重复 GameObject / 旧系统未清理

- **症状**：UI Toolkit 新版 StartMenu 加好后，旧的 uGUI `Canvas/StartPanelRoot` 还是 active，Game View 看到的是旧版被新版盖着。
- **根因**：agent 没主动检查"是否已存在功能等价的旧实现"。
- **应加规则**：新增 UI / 系统时，先 `find_gameobjects` 搜同名/同功能，发现旧的要么禁用要么提示用户决策。

### 2. Unity MCP 调用顺序未固化

- **症状**：经常先 refresh、再读 console，但偶尔顺序反过来导致 console 空。Render UI 在编辑模式下被静默调用、返回空白图但没识别。
- **应加规则**：把"C# 改完 → refresh → 等 ready → read_console → 再操作"和"render_ui 必须 Play 模式"写成检查清单。

### 3. Inspector 属性引用赋值踩坑

- **症状**：通过 MCP 给 SerializeField GameObject 字段赋值时，第一次用 `value: 26496`（裸 instanceID），报 "Could not load asset at path '26496'"；改成 `{"instanceID": 26496}` 才成功。
- **应加规则**：明确 `manage_components.set_property` 对 object reference 必须用 `{"instanceID":..}` / `{"guid":".."}` / `{"path":".."}` 格式，禁止裸数字。

### 4. UI Builder 缓存陷阱

- **症状**：USS 改完，UI Builder 切到别的 UXML 再回来会"回退"到旧样式；用户怀疑代码没生效。
- **应加规则**：明确 "UI Builder 预览不可信，只信 Play 模式 + render_ui"。已经在 prompt 里有一句，可以再强调一次并给出排查步骤。

### 5. Sprite 9-slice 配置坑

- **症状**：spriteBorder 设错（左右上下加起来超过元素尺寸），`background-image` 渲染成全黑。
- **应加规则**：使用 9-slice 时校验 `border.left + border.right < element.width` 且 `border.top + border.bottom < element.height`，并提示用户。

### 6. UI Toolkit 子元素的 USS 选择器约定

- **症状**：用户对 `.unity-base-slider__tracker` 这种"看不见的内置类"困惑——以为是新建元素。
- **应加规则**：写 UI Toolkit 控件主题化代码时，第一次引入 Unity 内置 USS class 要用注释解释来源（"Unity 内部为 Slider 自动创建的子元素"）。

### 7. Panel 切换的 Instance 空陷阱

- **症状**：Shop 点 nav-settings 跳转，但 SettingsPanel 的 UIDocument 子节点 inactive，`Awake` 从未跑过 → `Instance` 是 null → 跳转失败。
- **应加规则**：写"通过 Instance 调用其他 Panel"时必须写成 `Instance ?? FindFirstObjectByType<...>(FindObjectsInactive.Include)`。

### 8. 跨 Panel 调用 Show 时不要覆盖 returnTarget

- **症状**：Settings 点 nav-shop → `ShopPanel.Show(this.returnTarget)` 把 Shop 的 returnTarget 改成了"Settings 的 returnTarget"——结果链路混乱。
- **应加规则**：定义"二级面板互相切换"时只调用 `Show()` 不传参，依赖各 panel 自己 Inspector 里 baked 的 returnTarget。

### 9. 文件 watcher / VS Code 缓存

- **症状**：磁盘上文件改了，VS Code 资源管理器看不见、Roslyn 报 `name does not exist in current context`。
- **应加规则**：遇到"明明改了但工具说没生效"，第一反应是 reload window / refresh Unity，不是反复改代码。

### 10. 占位实现 → 真实接入的 handover

- **症状**：SettingsPanel 一开始所有回调都打 Debug.Log 占位，后来要接 GameUserSettings 时已经写了一堆样板代码要替换。
- **应加规则**：占位实现要在注释顶部统一标 `TODO: 接入 XXX`，并在 `task_complete` 总结里点名"目前是占位，等接入 X"。

---

## 已纳入 prompt

（每次更新 [.github/agents/go-down.agent.md](.github/agents/go-down.agent.md) 后把对应条目搬到这里。）

- _暂无_

---

## 维护方式

- 用户/agent 在工作中发现新坑 → 直接 append 到"待办"区
- 想集中处理一批改进时：让 agent 读这个文件，挑几条改进 prompt，更新 [.github/agents/go-down.agent.md](.github/agents/go-down.agent.md)，把搬到的条目移到"已纳入"
