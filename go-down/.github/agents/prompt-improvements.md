# Agent Prompt — 待优化清单（Workflow 级）

跟踪 [go-down.agent.md](go-down.agent.md) 在实际使用中暴露的 **流程问题**，不是技术细节。每条 = 让 agent 更稳健完成任务的元规则。

> **收录原则：项目无关、可复用。**
> 如果一条建议是"某个 API 的某个参数要写成某个格式"——它属于代码注释 / 错误处理，不属于这里。
> 这里的条目应该是"agent 应该用什么策略思考"，而不是"agent 该记住哪些知识点"。

完成后把条目移到底部"已纳入 prompt"区。

---

## 待办

_暂无_。下次发现新的流程级摩擦点写在这里。

---

## 已纳入 prompt

（每次更新 [go-down.agent.md](go-down.agent.md) 后把对应条目搬到这里。格式：标题 + 一行变更摘要 + 日期。）

- **2026-05-04** — todo list 改为强制走完所有 step，不能跳过；状态机/跳转任务必须列出所有触发路径作为单独 todo 项
- **2026-05-04** — 新增 "调用子 agent 的场景" 章节：探索未知/并行查询走 subagent，有状态依赖的修改不走
- **2026-05-04** — 新增 "Verify Symptom Before Fix"：bug 修复前用 1-2 个 tool 检验症状，不直接从用户描述推根因
- **2026-05-04** — Edit/Validate 表格里 UXML/USS 行升级为 "**必须** Play 模式 + render_ui"，明确不接受 UI Builder 预览
- **2026-05-04** — 新增 "Source of Truth 优先级" 章节：磁盘 > Unity MCP 状态 > Play render > 编辑器实时预览
- **2026-05-04** — Self-Evaluation 第 2 条量化：先 `find_gameobjects` / `grep_search` 找旧实现；完成后检查 "旧的去哪了"
- **2026-05-04** — Self-Evaluation 第 5 条强化：占位代码必须 `// TODO:` 注释 + 完成总结里点名清单
- **2026-05-04** — When Blocked 加 "失败两次必须切换策略" 显式选项（subagent / 静态分析 / 提问）
- **2026-05-04** — 新增 "Devil's Advocate" 章节：提出方案前必须做对立面批判（4 问），适用于多文件/架构/API 类改动；回复里要写出 trade-off

---

## 维护方式

- 工作中发现 **流程级** 摩擦点 → append 到"待办"
- 项目特定的代码坑 → 直接写在源文件注释里，**不来这里**
- 用户说"更新 agent prompt"时：读这个文件 → 挑能写成 1-3 行规则的条目 → 改 [go-down.agent.md](go-down.agent.md) → 把搬走的条目移到"已纳入"
