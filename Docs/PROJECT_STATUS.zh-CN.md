# 3DPlayDemo 当前进度

更新时间：2026-08-10

## 已完成

- 可从 `Assets/Scenes/Boot.unity` 进入战斗关卡并游玩。
- 玩家移动、攻击、翻滚、死亡后延迟复活；敌人具有人形骑士状态机和死亡销毁流程。
- 剑 Hitbox、DamageInfo、IDamageable、阵营、防御、雷属性易伤 Buff 已接通。
- 背包、武器/头盔/盔甲/手套/鞋子、5 个饰品槽、套装属性和拾取交互已完成。
- 任务、角色名册、对话图、世界对话终端、商业化多页面 UI 已完成。
- YooAsset 统一资源加载、启动组合根、EventBus、Service/Server 分层和中文注释审计已完成。
- 最近一次记录的测试结果：EditMode 201/201，PlayMode 7/7。此次同步遵照要求未重跑测试。

## GitHub 同步

- 仓库：[LR-Shur/3DPlayDemo](https://github.com/LR-Shur/3DPlayDemo)
- 分支：`agent/player-state-machine`
- 提交：`db2da39`
- 会话记录：[`SESSION_RECORD.zh-CN.md`](SESSION_RECORD.zh-CN.md)

为了控制仓库体积，未被关卡引用的 KawaiiCity 城市素材目录已加入 `.gitignore`，但仍保留在开发机上；重新需要时可从本地素材导入。

## 下一阶段

1. 存档/读档：持久化任务、背包、装备和角色进度。
2. 角色选择：切换实际玩家模型、动画和技能组。
3. NPC 场景化：把对话从终端扩展到可接近的 NPC。
4. 后续再补音效、特效、伤害数字、等级和技能养成。

## 继续开发

```text
打开 Unity 项目
切换到 agent/player-state-machine 分支
执行 Tools/Train/Content 下的内容构建菜单
打开 Assets/Scenes/Boot.unity 并进入 Play
```
