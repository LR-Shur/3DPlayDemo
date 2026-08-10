# 3DPlayDemo 当前进度

更新时间：2026-08-11

## 已完成

- 可从 `Assets/Scenes/Boot.unity` 进入战斗关卡并游玩。
- 玩家移动、攻击、翻滚、死亡后延迟复活；敌人具有人形骑士状态机和死亡销毁流程。
- 剑 Hitbox、DamageInfo、IDamageable、阵营、防御、雷属性易伤 Buff 已接通。
- 背包、武器/头盔/盔甲/手套/鞋子、5 个饰品槽、套装属性和拾取交互已完成。
- 任务、角色名册、对话图、世界对话终端、商业化多页面 UI 已完成。
- YooAsset 统一资源加载、启动组合根、EventBus、Service/Server 分层和中文注释审计已完成。
- Luban 配置流水线已接入；`Train.Composition` 已显式引用 `Luban.Runtime`，6 张中文 CSV 已统一为 UTF-8 BOM。
- 修复了缺失 URP 资源引用造成的场景洋红材质显示问题；保留原有渲染管线与全局配置，只补回项目原本引用的 Forward Renderer 和质量级别资源。
- 新增第 2 战区与 Boss 关卡：场景布置、敌人变体、关卡定义、掉落、通关奖励和 Build Settings 已接通。
- 新增金币、战利品掉落、通关后续关卡按钮和战地商店；金币固定在左上生命条下方，仓库/装备按钮固定在右上，已通过实际运行截图确认不遮挡。
- 背包容量从 24 提升到 36；商店可购买装备并自动尝试装备，沿用现有装备属性、套装和 Buff 体系。
- 最新测试结果：EditMode 204/204，PlayMode 7/7。
- 本轮打磨新增统一命中反馈：敌人受到伤害时显示伤害数字和轻量命中粒子；Boss 关卡显示独立 Boss 血条，战斗提示会自动收起。

## GitHub 同步

- 仓库：[LR-Shur/3DPlayDemo](https://github.com/LR-Shur/3DPlayDemo)
- 分支：`agent/player-state-machine`
- 提交：`dfa7374`
- 会话记录：[`SESSION_RECORD.zh-CN.md`](SESSION_RECORD.zh-CN.md)

为了控制仓库体积，未被关卡引用的 KawaiiCity 城市素材目录已加入 `.gitignore`，但仍保留在开发机上；当前新增场景使用项目内现有模型与程序化工业布景。

## 下一阶段

1. 存档/读档：持久化金币、关卡解锁、背包和装备进度。
2. 商店与掉落深化：价格曲线、稀有度颜色、装备词条和更多战利品模型。
3. 战斗深化：受击反馈、连击窗口、精英技能和 Boss 阶段机制。
4. 角色选择：切换实际玩家模型、动画和技能组，并补齐音效、特效和伤害数字。

## 继续开发

```text
打开 Unity 项目
切换到 agent/player-state-machine 分支
执行 Tools/Train/Content 下的内容构建菜单
打开 Assets/Scenes/Boot.unity 并进入 Play
```
