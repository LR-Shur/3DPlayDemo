# 3DPlayDemo 开发会话记录

> 最后更新：2026-08-10（Luban 编译修复与会话同步）
>
> 用途：这是项目的“最后一次会话记录”。换环境或继续开发前，先阅读本文件，再查看 `git log` 和工作区状态。

## 0. 会话记录维护规则

- 每次完成一轮开发后更新本文件，覆盖当前提交、验证结果、遗留问题和下一步计划。
- 新环境拉取仓库后，优先阅读本文件，再打开 Unity；不要把旧聊天记录当成代码现状。
- 只记录已经落地或已经验证的事实；未提交的本地改动必须单独列出。

## 1. 项目一句话

基于 Unity 的第三人称动作战斗 Demo，包含可游玩的霓虹训练关卡、商业化多页面 UI、装备/饰品系统、Buff 系统、背包、任务、角色名册和对话系统。

## 2. 当前可玩内容

- 从 `Assets/Scenes/Boot.unity` 进入，自动加载战斗关卡 `Level_Combat_001`。
- 玩家：移动、普通攻击、翻滚、死亡延迟复活。
- 敌人：人形骑士，包含闲置/巡逻/追击/受击/攻击/死亡状态机，死亡播放动画后销毁。
- 战斗：剑 Hitbox、DamageInfo/IDamageable/IDamageSource、阵营、防御、雷属性易伤 Buff。
- 地图拾取：发光拾取物，靠近显示提示，按 E 拾取，背包满时保留场景物体。
- 装备：武器/头盔/盔甲/手套/鞋子 + 5 个饰品槽，18 件装备、2 套套装、6 项最终属性。
- 背包：24 槽位、堆叠规则、物品详情。
- Buff：`BuffInfo -> BuffFactoryRegistry -> IBuff -> BuffHandle`；雷剑命中叠雷易伤，每层 12%，最多 5 层，只放大雷属性伤害。
- 任务：5 个中文任务（初次巡防、场地回收、战区许可、雷鸣共振、进阶综合演练），支持追踪、进度、奖励领取，领取失败保持可重领。
- 角色名册：艾莲默认解锁，铃、比利已登记但锁定；可查看资料、解锁、选择。
- 对话：3 组中文对话（训练终端引导、与 Rusk 初次会面、巡逻队汇报），对话图 + 条件/命令；世界里有 3 个对话终端，靠近按 E 打开对话浮层；空格继续、数字键 1-4 选择。
- UI：仓库、装备、任务、角色、档案导航；HUD 生命条、任务追踪、交互提示、拾取 Toast。
- 资源：YooAsset 统一加载，EditorSimulate 模式。

## 3. 按键

| 输入 | 作用 |
| --- | --- |
| WASD | 移动 |
| 鼠标左键 | 普通攻击 |
| 输入映射中的 Dodge | 翻滚 |
| E | 拾取 / 对话 |
| B / Tab | 打开或关闭仓库 |
| C | 打开或关闭装备 |
| 空格 / 回车 | 对话继续 |
| 数字键 1-4 | 对话选项 |
| Esc | 关闭主菜单 / 取消对话 |

## 4. 架构分层

项目采用“按功能模块纵向切分、模块内部再分层”的结构，吸收 MVCS 思想，但不硬性划分四个巨型目录。

```text
Assets/Scripts
├─ Architecture        # Bootstrap、EventBus、ServiceRegistry、资产接口
├─ Infrastructure      # YooAsset 实现与资源租约
├─ Composition         # 应用组合根 GameApplicationStartup
├─ Gameplay            # 玩家、敌人、战斗、镜头
├─ Inventory           # 背包领域模型与应用服务
├─ Equipment           # 装备槽、属性、套装
├─ Buffs               # BuffHandle、工厂、雷易伤
├─ Quest               # 任务领域模型、Fact 推进、奖励事务
├─ Characters          # 角色名册、解锁、选择
├─ Dialogue            # 对话图、会话状态机、条件/命令
├─ WorldInteraction    # 交互扫描、焦点、拾取、对话终端
├─ GameFlow            # 关卡状态机、关卡只读模型
└─ Presentation/UI     # View、Presenter、页面导航、输入模态
```

### 启动流程

`GameBootstrap` 在场景加载前自动创建，`GameApplicationStartup` 按依赖顺序安装：

1. 资源服务（YooAsset）
2. 输入模式服务
3. 背包
4. 装备
5. 任务
6. 角色
7. 对话
8. UI

任意场景单独打开点 Play 也能自动补齐这些服务。

### 事件通信原则

- 命令某个明确对象做事：接口方法。
- 请求业务操作并拿到结果：Server/Service。
- 通知多个未知接收者“某事已发生”：EventBus。

权威数据保存在对应 Service 中，事件只通知其他层重读快照。

## 5. 关键文件

| 文件 | 作用 |
| --- | --- |
| `Assets/Scripts/Architecture/Bootstrap/GameBootstrap.cs` | 进程级组合根 |
| `Assets/Scripts/Composition/GameApplicationStartup.cs` | 服务安装与启动生命周期 |
| `Assets/Scripts/Gameplay/Combat/SwordHitbox.cs` | 剑触发器伤害源 |
| `Assets/Scripts/Gameplay/Combat/Health.cs` | 通用生命组件 |
| `Assets/Scripts/Buffs/Core/BuffHandle.cs` | 每个实体的 Buff 句柄 |
| `Assets/Scripts/Equipment/Application/EquipmentService.cs` | 装备/饰品服务 |
| `Assets/Scripts/Inventory/Application/InventoryService.cs` | 背包服务 |
| `Assets/Scripts/Quest/Application/QuestService.cs` | 任务服务 |
| `Assets/Scripts/Characters/Application/CharacterRosterService.cs` | 角色名册服务 |
| `Assets/Scripts/Dialogue/Application/DialogueService.cs` | 对话服务 |
| `Assets/Scripts/WorldInteraction/Runtime/WorldDialogueTerminal.cs` | 世界对话终端 |
| `Assets/Scripts/Presentation/UI/Runtime/UIService.cs` | 主菜单页面与 Presenter 生命周期 |
| `Assets/Scripts/Editor/UI/GameUIContentBuilder.cs` | 商业化 UI 预制体生成器 |
| `Docs/ArchitectureAndTesting.zh-CN.md` | 架构与测试教学文档 |

## 6. 内容构建菜单

编辑器菜单 `Tools/Train/Content` 下提供幂等构建器：

- `Build Game UI`：字体、主题、GameUIRoot 预制体
- `Build Quest Content`：5 个任务
- `Build Character Content`：3 名角色
- `Build Dialogue Content`：3 组对话
- `Build Dialogue Terminals`：世界对话终端
- `Build World Pickups`：世界拾取物
- `Build Inventory Content`、`Build Equipment Content`

重复执行不会产生重复资产。

## 7. 测试

最后一次全量结果：

- EditMode：201/201 通过
- PlayMode：7/7 通过

运行方式：

```text
Window > General > Test Runner
```

只跑一个测试：选中测试方法后 `Run Selected`，或把完整名称传给 `test_names`。

测试目录：

```text
Assets/Tests
├─ EditMode
│  ├─ Architecture / Buffs / Combat / Characters / Dialogue
│  ├─ Enemy / Equipment / GameFlow / Inventory / Quest / UI / WorldInteraction
└─ PlayMode
   ├─ Assets / Combat / Enemy / GameFlow / Inventory
```

## 8. 素材说明与版权提醒

项目包含第三方素材：

- KawaiiCity 城市场景（原始 unitypackage 未纳入 Git）
- KayKit 人形骑士
- 绝区零风格角色模型（Belle、Billy 已精简导入）
- Kenney SciFi UI 图标
- NotoSansCJK 中文字体

请保持仓库为 **私有**，不要公开分发这些素材。

## 9. 已知限制与下一步

已完成：

- [x] YooAsset 统一资源加载
- [x] 可游玩战斗关卡与关卡状态机
- [x] 背包、装备/饰品、Buff
- [x] 地图拾取与 E 键交互
- [x] 任务、角色名册、对话系统
- [x] 商业化多页面 UI
- [x] 中文注释审计与测试文档
- [x] GameFlow 复用 Gameplay.Common 异步状态机基类，转换策略改为数据驱动

建议下一步：

- [ ] 档案页接入真实图鉴/收集数据
- [ ] 角色“选择”真正切换场景玩家模型与技能组
- [ ] 把对话 NPC 放到场景而非只有终端
- [ ] 存档/读档（任务、背包、装备、角色进度）
- [ ] 更多敌人行为（行为树或更丰富状态）
- [ ] 音效、特效、伤害数字
- [ ] 角色等级/技能养成

## 10. 如何在新机器继续

```bash
git clone <仓库地址> 3DPlayDemo
```

用 Unity 打开项目，先执行：

```text
Tools/Train/Content/Build Game UI
Tools/Train/Content/Build Quest Content
Tools/Train/Content/Build Character Content
Tools/Train/Content/Build Dialogue Content
Tools/Train/Content/Build Dialogue Terminals
Tools/Train/Content/Build World Pickups
```

然后打开 `Assets/Scenes/Boot.unity` 点 Play。

## 11. 本次会话增量：Luban 编译与中文表格修复

### 报错原因

Unity 报告 `Luban`、`ByteBuf` 类型不存在。Luban 包已经嵌入并导入成功，但 `Train.Composition` 的程序集定义没有显式引用 `Luban.Runtime`，导致生成的表代码无法解析运行时类型。

### 已完成修改

- 在 `Assets/Scripts/Composition/Train.Composition.asmdef` 增加 `Luban.Runtime` 程序集引用。
- `Assets/Config/Luban/Data` 下 6 张 CSV 统一保存为 UTF-8 BOM，解决 Windows Excel 按系统代码页打开时的中文乱码。
- `Tools/GenerateLubanConfig.ps1` 增强路径兼容性；已实际运行并成功生成 C# 表代码和 `.bytes` 数据。
- `Tools/README.md` 增加 CSV 编码约定：以后所有包含中文的 Luban 表都保存为 UTF-8 BOM。
- Unity 重新导入并编译后，没有出现修复之后的新 `Luban/ByteBuf` 编译错误；旧错误只存在于 Editor.log 的历史编译段。

### GitHub 状态

- 仓库：`https://github.com/LR-Shur/3DPlayDemo.git`
- 当前分支：`agent/player-state-machine`
- 当前提交：`1272b0e fix Luban assembly reference and UTF-8 tables`
- 现有 PR：`https://github.com/LR-Shur/3DPlayDemo/pull/1`
- 本次只提交了 Luban 修复、CSV 编码、文档和 Unity `.meta`；没有提交 `.unitypackage` 或大体积城市素材。

### 有意保留的本地未提交文件

- `Assets/Arts/UI/Fonts/NotoSansCJKsc-Regular SDF.asset`：已有本地字体改动，不属于本次 Luban 修复。
- `Assets/Scripts/Infrastructure/Config.meta`：空目录遗留的未跟踪 `.meta`，没有纳入提交。

## 12. 2026-08-10 最终同步状态

- 已将当前工程推送到 GitHub：`https://github.com/LR-Shur/3DPlayDemo.git`
- 当前分支：`agent/player-state-machine`
- 当前提交：`1272b0e fix Luban assembly reference and UTF-8 tables`
- 已同步 Luban 配置流水线、UTF-8 CSV、会话记录和架构/测试文档。
- 为避免仓库体积过大，未使用且未被场景引用的 `Assets/Arts/KawaiiCity/` 与 `Assets/Arts/KawaiiCity_URP/` 已加入忽略规则；本地素材仍保留，没有删除本机文件。
- 本次没有重新运行完整 Unity 测试；文档中记录的最近一次结果仍为 EditMode 201/201、PlayMode 7/7。本次额外运行了 Luban 生成脚本并成功完成校验、代码生成和二进制生成。

从其他机器继续时，先切换到 `agent/player-state-machine` 分支，阅读本文件第 11 节，再按第 10 节执行内容构建菜单。下一阶段优先级建议为：先确认 Unity 无编译错误，再做存档/读档、角色选择真正换模、NPC 场景化，然后补音效/特效和养成系统。
