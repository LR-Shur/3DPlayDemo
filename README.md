# 3DPlayDemo

基于 Unity 的第三人称动作战斗 Demo：可游玩训练关卡、商业化多页面 UI、装备/饰品、Buff、背包、任务、角色名册与对话系统。

## 快速开始

1. 用 Unity 打开项目根目录。
2. 执行 `Tools/Train/Content` 下的构建菜单（Game UI、Quest、Character、Dialogue、Dialogue Terminals、World Pickups）。
3. 打开 `Assets/Scenes/Boot.unity` 并进入 Play。

## 操作

| 输入 | 作用 |
| --- | --- |
| WASD | 移动 |
| 鼠标左键 | 普通攻击 |
| E | 拾取 / 对话 |
| B / Tab | 仓库 |
| C | 装备 |
| 空格 / 回车 | 对话继续 |
| 数字键 1-4 | 对话选项 |
| Esc | 关闭主菜单 / 取消对话 |

## 文档

- 架构与测试教学：[Docs/ArchitectureAndTesting.zh-CN.md](Docs/ArchitectureAndTesting.zh-CN.md)
- 会话与进度记录：[Docs/SESSION_RECORD.zh-CN.md](Docs/SESSION_RECORD.zh-CN.md)

## 测试

```text
Window > General > Test Runner
```

当前 EditMode 201/201、PlayMode 7/7 通过。

> 仓库包含第三方素材，请保持私有，不要公开分发。
