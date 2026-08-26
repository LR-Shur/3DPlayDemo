# 霓虹回收协议

> 基于 Unity 开发的第三人称都市异常动作 Roguelite 原型。

玩家进入受异常能源污染的封锁区，在短时战斗中完成回收任务，并通过装备、套装、主动道具与局内资源形成构筑。目前项目已经具备一套可以连续迭代的战斗、关卡和成长框架，仍处于玩法与表现共同打磨阶段。

## 项目概览

| 项目 | 当前状态 |
| --- | --- |
| 引擎 | Unity 6000.3.20f1 / URP 17.3 |
| 类型 | 第三人称动作 Roguelite |
| 可玩内容 | 7 个战斗关卡，包含训练、机制、工业、冷却花园与 Boss 场景 |
| 战斗 | 普通攻击、闪避、受击僵直、命中反馈、敌人近战与远程行为 |
| 成长 | 10 个装备槽、套装奖励、主动道具、Buff、商店与局内掉落 |
| 内容系统 | Luban 配置、YooAsset 资源加载、ScriptableObject 关卡数据 |
| 工程状态 | 原型开发中，当前重点是关卡质量、战斗手感与 UI 表现 |

## 当前可玩内容

### 战斗与敌人

- 13 个敌人原型统一映射各自的 canonical 预制体。
- 同类敌人的尺寸、属性、动画、碰撞和 NavMeshAgent 在预制体层集中维护。
- 七个可玩关卡均保存有效 NavMesh，敌人可以追击玩家并在攻击距离内切换行为。
- 已加入受击闪红、最后一击反馈、僵直、死亡演出和更清楚的伤害飘字。
- 近战敌人不会依靠物理推挤持续推动玩家，移动与攻击职责保持分离。

### 装备与构筑

当前提供四套五件构筑，每套包含武器、护甲、鞋子、头盔和饰品，并在 2/4/5 件时解锁阶段奖励。

| 构筑 | 战斗方向 | 主要收益 |
| --- | --- | --- |
| 炽焰连击 | 贴身连续攻击、燃烧叠层 | 攻击与暴击 |
| 潮汐续航 | 潮湿联动、持续作战 | 生命与防御 |
| 疾风机动 | 闪避追击、风痕爆发 | 攻击与暴击 |
| 震岳破防 | 破碎叠层、重击收尾 | 防御、攻击与暴击伤害 |

掉落由敌人原型映射到对应构筑池：普通怪、精英和 Boss 使用不同装备概率与品质规则；没有掉落装备时，会返回合法的构筑消耗品或强化材料。

### 关卡与界面

- 当前关卡包括 Combat001、Combat002、EnergyRelay、OrbitalCargo、CryoGarden、Boss001 与主流程场景。
- EnergyRelay 已作为当前场景质量基准，其余关卡继续向清晰路线、地标和战斗空间靠拢。
- HUD、背包、装备、商店、任务、角色名册和对话系统已接入。
- 背包、装备和商店具备基础打开、关闭、按钮悬停/按下与详情切换动效。

## 技术结构

项目按玩法模块组织，并在模块内部区分 Core、Application、Data、Presentation 与 Editor 职责。

```text
Assets/Scripts
├─ Architecture       游戏与场景上下文、事件总线、服务注册
├─ Infrastructure     YooAsset 等基础设施实现
├─ Gameplay           Player、Enemy、Combat 与 Buff
├─ GameFlow           关卡状态机、生成与结算流程
├─ Inventory          背包领域与服务
├─ Equipment          装备、属性快照与套装规则
├─ Quest              任务事实、进度与奖励
├─ Dialogue           对话图与会话状态机
├─ WorldInteraction   拾取、交互焦点与提示
├─ Presentation.UI    View、Presenter 与页面导航
└─ Composition        运行时组合与跨模块接线
```

几个重要的单一数据源：

- `enemy_archetypes.csv`：敌人原型与唯一预制体。
- `equipment*.csv`：装备属性、效果、套装成员与阶段奖励。
- `items.csv`：背包展示和物品基础信息。
- `Assets/Data/Levels/*.asset`：关卡流程、出生点、敌人编制与奖励。

## 运行项目

1. 使用 Unity `6000.3.20f1` 打开项目根目录。
2. 首次导入后等待脚本和资源完成刷新。
3. 如需重建内容，执行 `Tools/Train/Content` 下对应的 Game UI、Quest、Character、Dialogue、Dialogue Terminals 与 World Pickups 菜单。
4. 打开 `Assets/Scenes/Boot.unity`，进入 Play Mode。

开发单个关卡时也可以直接打开 `Assets/Scenes/Playable` 下的场景运行；启动流程会补齐必要的游戏级服务。

## 操作

| 输入 | 功能 |
| --- | --- |
| WASD | 移动 |
| 鼠标左键 | 普通攻击 |
| E | 拾取 / 交互 / 对话 |
| B / Tab | 打开仓库 |
| C | 打开装备 |
| 空格 / 回车 | 继续对话 |
| 数字键 1–4 | 选择对话选项 |
| Esc | 关闭当前菜单或取消对话 |

## 配置工作流

装备、套装、物品或敌人原型修改完成后，在项目根目录执行：

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\GenerateLubanConfig.ps1
```

脚本会生成运行时表代码并更新 `Assets/StreamingAssets/Config/LubanBytes`。关卡仍使用 Unity ScriptableObject 和场景点位编辑，不迁移到表格中。

## 测试与验证

测试入口：

```text
Window > General > Test Runner
```

项目同时使用 EditMode 与 PlayMode 测试：

- EditMode：领域规则、配置映射、装备构筑、掉落、Presenter 与预制体约定。
- PlayMode：场景启动、物理、Animator、敌人追击、死亡演出和完整流程。

当前里程碑已完成敌人 canonical 预制体、全关卡 NavMesh、装备构筑和掉落解析的定向验证，并在 Combat002 与 CryoGarden 实测敌人追击与攻击距离切换。

## 下一阶段

- 以 EnergyRelay 为基准继续重做其余关卡的空间层次、地标和环境叙事。
- 完善攻击前摇、命中停顿、镜头震动、音效与 Boss 阶段反馈。
- 继续拆分体量较大的运行时控制器和模态 UI，实现更清楚的 View/Presenter 边界。
- 在场景与基础手感稳定后，再推进锁定目标、完美闪避和更完整的韧性/破防机制。

## 文档

- [当前策划方向](Docs/GAME_DESIGN_DIRECTION.zh-CN.md)
- [架构与测试说明](Docs/ArchitectureAndTesting.zh-CN.md)
- [装备、构筑与掉落配置](Assets/Config/Luban/装备与套装配置说明.md)
- [关卡制作与敌人点位规范](Assets/Prefabs/Level/关卡制作说明.md)
- [工具说明](Tools/README.md)

## 素材说明

仓库包含第三方角色、动画、UI、VFX 与环境素材。请保留原始许可证和来源说明；未经确认，不要将仓库内素材单独公开分发或用于仓库授权范围之外的用途。
