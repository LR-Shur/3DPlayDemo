# 3DPlayDemo

基于 Unity 的第三人称都市异常动作 Roguelite Demo，暂定名“霓虹回收协议”。项目已具备多关卡战斗、装备与主动道具、Buff、背包、任务、角色名册、对话、商店和局内存档。

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

- 当前策划方向：[Docs/GAME_DESIGN_DIRECTION.zh-CN.md](Docs/GAME_DESIGN_DIRECTION.zh-CN.md)
- 架构与测试教学：[Docs/ArchitectureAndTesting.zh-CN.md](Docs/ArchitectureAndTesting.zh-CN.md)

## 测试

```text
Window > General > Test Runner
```

测试数量会随功能增加而变化，提交前应运行与改动对应的 EditMode/PlayMode 测试。

> 仓库包含第三方素材，请保持私有，不要公开分发。
