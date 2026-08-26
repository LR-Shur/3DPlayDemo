# 3DPlayDemo

基于 Unity 的第三人称都市异常动作 Roguelite Demo，暂定名“霓虹回收协议”。项目已具备多关卡战斗、装备与主动道具、Buff、背包、任务、角色名册、对话、商店和局内存档。

## 当前版本进展

- 七个可玩关卡均已保存有效 NavMesh，敌人可在不同关卡正常追击和进入攻击距离。
- 每个敌人原型只使用 `enemy_archetypes.csv` 指定的唯一预制体，关卡数据不再单独决定怪物外观和运行时骨架。
- 已加强受击闪红、死亡前受击反馈、僵直、伤害飘字与敌人移动稳定性。
- 已加入炽焰连击、潮汐续航、疾风机动、震岳破防四套五件构筑及 2/4/5 件奖励。
- 敌人掉落按原型映射构筑池；普通怪、精英和 Boss 使用不同装备概率与品质规则。
- 背包、装备和商店已加入打开、关闭、按钮与详情切换动效。

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
- 装备、构筑与掉落配置：[Assets/Config/Luban/装备与套装配置说明.md](Assets/Config/Luban/装备与套装配置说明.md)
- 关卡制作与敌人点位规范：[Assets/Prefabs/Level/关卡制作说明.md](Assets/Prefabs/Level/关卡制作说明.md)

## 测试

```text
Window > General > Test Runner
```

测试数量会随功能增加而变化，提交前应运行与改动对应的 EditMode/PlayMode 测试。

2026-08-26 的当前里程碑已通过敌人 canonical 预制体、全关卡 NavMesh、装备构筑和掉落解析的定向测试，并在 Combat002、CryoGarden 完成 PlayMode 追击验证。

> 仓库包含第三方素材，请保持私有，不要公开分发。
