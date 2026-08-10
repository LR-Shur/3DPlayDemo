# 项目架构与测试入门

这份文档面向“已经会写一点 C# 和 MonoBehaviour，但还没完整做过商业项目架构”的开发阶段。先理解职责边界，再顺着事件流看代码，会比逐文件背代码快很多。

## 1. 当前分层

项目采用“按功能模块纵向切分，模块内部再分层”的方式。它吸收了 MVCS 的思想，但没有把所有代码硬塞进四个巨型目录。

### Architecture

负责通用基础设施，不知道玩家、敌人和伤害的具体规则。

- `GameBootstrap`：游戏进程级组合入口。
- `SceneBootstrap`：场景级组合入口，让任意场景都能独立运行。
- `GameContext`：保存游戏生命周期内的服务。
- `SceneContext`：保存当前场景生命周期内的服务。
- `IEventBus`：跨模块事件通信的接口。
- `EventBus`：同步事件中心实现。
- `IServiceRegistry`：按接口注册和获取 Server/Service。

### Infrastructure

负责第三方或平台能力的具体实现。

- `YooAssetService`：统一加载配置、预制体和场景。
- Lease：显式表达“谁持有这个资源、何时释放”，避免加载句柄散落在业务层。

### Gameplay

按玩法功能组织。

- `Combat`：伤害、生命、阵营、防御和生命事件。
- `Enemy`：感知、移动、战斗、状态机和 Animator 适配。
- `Player`：输入、状态机、移动、动画、装备属性绑定和复活。

### 独立业务模块

- `Inventory`：背包领域模型和背包 Server。
- `Equipment`：十个装备槽、最终属性、套装和装备 Server。
- `Buffs`：每个实体自己的 `BuffHandle`、Buff 工厂和雷易伤规则。
- `WorldInteraction`：附近候选、稳定焦点、地图拾取和 E 键交互。
- `GameFlow`：关卡流程状态机和关卡会话只读模型。
- `Quest`：任务定义、进度和奖励。
- `Characters`：角色名册、解锁与当前选择。
- `Dialogue`：对话图、会话状态机、条件和命令处理器。
- `Presentation.UI`：被动 View、Presenter、页面导航和输入模式。

每个功能内部继续使用下面几层：

- `Core/Domain`：纯数据和规则，尽量不依赖 Unity 场景。
- `Application`：完成一个用例的 Server/Service。
- `Data`：ScriptableObject 配置及其到 Core 的转换。
- `Presentation`：UI、Animator、特效和 MonoBehaviour 适配。
- `Events`：已经发生的跨模块事实。
- `Editor`：只在编辑器内运行的幂等内容构建器。

## 2. 为什么不是“所有通信都走事件中心”

事件中心适合广播已经发生的事实，不适合代替所有方法调用。

同一个实体内部，一对一且必须立即执行的命令，直接调用更清楚：

```text
Health.Died -> EnemyController -> EnemyDeathState
```

跨模块的通知适合事件中心：

```text
Health.Died
    -> HealthEventRelay
    -> EntityDiedEvent
        -> HUD
        -> 关卡统计
        -> 掉落系统
        -> 任务系统
        -> 音效系统
```

可以用三句话判断：

- 命令某个明确对象做事：接口方法。
- 请求一个业务操作并得到结果：Server/Service。
- 通知若干未知接收者“某件事已经发生”：Event。

不要用事件去询问数据，也不要依赖订阅者执行顺序完成关键事务。权威数据仍然保存在对应 Service 中，事件只通知其他层重新读取快照。

## 3. 为什么 DamageSource 不直接保存 GameObject

`DamageInfo` 只传递本次伤害事实，来源则依赖 `IDamageSource`。这样来源可以是：

- 剑的 Hitbox；
- 子弹；
- Buff 的持续伤害；
- 陷阱；
- 测试里的假对象。

`GameObject` 是 Unity 容器，不等于伤害业务身份。接口能只暴露伤害系统真正需要的能力，也更容易写纯测试。

如果以后来源逻辑越来越复杂，可以再增加一个普通 C# `DamageHandle`，让 MonoBehaviour 只负责物理命中。不过目前的 `IDamageSource + DamageInfo + DamageHandler` 已经把职责拆开，不必为了“看起来高级”提前多包一层。

## 4. 装备、Buff 与伤害为什么分开

它们变化频率和生命周期不同。

### EquipmentLoadout

负责长期、低频的角色构筑：

- 武器、头盔、盔甲、手套、鞋子；
- 五个饰品槽；
- 基础值、固定加成、百分比加成、独立乘区；
- 套装件数奖励；
- 不可变属性快照。

当前公式为：

```text
最终值 = (基础值 + 固定加成)
       × (1 + 所有加算百分比之和)
       × 每个独立乘算区
```

### BuffHandle

负责战斗中高频、短生命周期的动态效果。每个角色各有一个 Handle，Buff 不会被错误地全局共享。

```text
BuffInfo
    -> BuffFactoryRegistry
    -> IBuff 实例
    -> 目标自己的 BuffHandle
```

`BuffInfo` 是输入数据，工厂负责按稳定 Buff ID 创建具体类，`BuffHandle` 负责叠层、刷新、Tick 和伤害修正。

雷剑的流程是：

```text
SwordHitbox 命中
    -> DamageHandler 结算本次雷伤
    -> LightningWeaponHitEffect
    -> 目标 BuffHandle.Apply(雷易伤 BuffInfo)
    -> 后续雷伤按层数放大
```

第一次命中后才施加易伤，所以第一次不吃增幅；最多五层，每层 12%，满层后续雷伤增加 60%。物理伤害不受这个 Buff 影响。

## 5. 背包、装备 UI 和拾取的事件流

### 换装

```text
EquipmentScreenView 按钮
    -> EquipmentPresenter
    -> IEquipmentService.Equip
    -> EquipmentLoadout
    -> EquipmentChangedEvent
        -> EquipmentPresenter 重读快照并刷新 UI
        -> PlayerEquipmentStatBinder 更新生命、防御和攻击
```

View 不直接改属性，Presenter 不保存权威装备数据，最终数值只由 Equipment Service 的快照给出。

### 世界拾取

```text
PlayerInteractionController
    -> Physics 扫描附近 WorldItemPickup
    -> InteractionFocusModel 选择最高优先级候选
    -> InteractionPromptChangedEvent
    -> HUD 显示“E 拾取”

玩家按 E
    -> IInteractable.Interact
    -> IInventoryService.TryAdd
    -> InventoryChangedEvent / ItemAcquiredEvent
    -> HUD Toast、背包页面、任务系统分别响应
```

背包满时 `TryAdd` 失败，拾取物不会销毁；成功时才关闭光标并删除场景对象。

## 6. 关卡为什么适合状态机

关卡有明确、互斥而且有顺序的阶段，因此适合状态机：

```text
Preparing -> Intro -> Combat -> Cleared/Failed -> Exiting
```

状态机的价值不是“代码更高级”，而是把合法跳转集中在一个地方。UI、输入和敌人生成只读取当前阶段，不需要各自猜测十几个布尔值的组合。

如果只是一个没有阶段切换的静态房间，就没必要使用状态机。当前战斗关卡包含准备、战斗、胜负和退出，因此使用它是合理的。

## 7. 任务为什么使用 Fact 推进

任务不直接查找敌人 GameObject，也不把“杀敌、拾取、通关、完成对话”分别写成四套任务类。玩法系统只发布已经发生的事实，任务 Server 把它们统一转换成 `QuestFact`：

```text
EnemyDefeatedEvent  -> QuestFact(EnemyDefeated, enemyArchetypeId)
ItemAcquiredEvent   -> QuestFact(ItemAcquired, itemId, quantity)
LevelCompletedEvent -> QuestFact(LevelCompleted, levelId)
DialogueCompleted   -> QuestFact(DialogueCompleted, dialogueId)
```

一个任务目标只关心三个值：事实类型、目标 ID、所需数量。这样增加“击败 10 个同类敌人”只需要新建配置，不需要再写一个新的 MonoBehaviour。

领取奖励属于应用层事务，不属于纯任务进度模型：

```text
QuestProgressModel：只判断能否完成/领取
QuestService：预检背包容量、发放物品、提交 Claimed 状态、发布事件
```

如果背包装不下全部奖励，任务保持 `Completed`，玩家清理空间后仍可再次领取。

## 8. 对话为什么使用图和会话状态机

对话资产是一张有向图。节点可以是台词、选择或结束，选择可以带条件和命令。

```text
DialogueDefinition
    -> DialogueGraphSpec
    -> DialogueSession
        -> AwaitingContinue
        -> AwaitingChoice
        -> Completed / Cancelled
```

`IDialogueCondition` 和 `IDialogueCommand` 让条件、命令可插拔。当前内置变量存在、变量相等和写入变量；以后可以在组合层注册“任务已完成”“角色好感度达到”等处理器，不需要修改对话会话核心。

对话核心不保存 `GameObject`，只保存说话者稳定 ID、台词文本、可见选项和变量。具体头像、模型、语音和镜头属于表现层。

世界里的 `WorldDialogueTerminal` 把对话与现有交互系统接在一起：玩家靠近时 HUD 显示“对话”，按 E 通过 `IDialogueService.Start` 启动会话；`DialoguePresenter` 申请模态输入租约，让对话期间玩家移动和攻击被阻塞。选择节点由条件和命令驱动，完成对话会发布 `DialogueCompletedEvent`，任务系统再把它作为 `QuestFactType.DialogueCompleted` 推进目标。

## 9. Bootstrap 如何让场景独立运行

`GameBootstrap` 使用 `RuntimeInitializeOnLoadMethod` 在场景加载前自动创建，`GameApplicationStartup` 按依赖顺序安装资源、背包、装备、任务、角色、对话和 UI 服务。

`SceneBootstrap` 保存当前场景的上下文。正式流程可以从 Boot 场景进入，开发时也可以直接打开战斗场景点击 Play；缺少入口对象时系统会自动补齐。

组合根只做三件事：

1. 创建对象；
2. 按接口安装依赖；
3. 管理生命周期。

它不应该包含伤害公式、任务判断或 UI 排版。

## 10. EventBus 的作用域与释放

- `GameContext.Events`：游戏级总线。
- `SceneContext.Events`：场景级总线。

场景事件会向游戏级总线冒泡。场景卸载时，场景总线会被释放，避免旧场景订阅残留。

订阅会返回 `IDisposable`：

```csharp
private IDisposable _subscription;

private void OnEnable()
{
    _subscription = events.Subscribe<EntityDiedEvent>(OnEntityDied);
}

private void OnDisable()
{
    _subscription?.Dispose();
    _subscription = null;
}
```

如果忘记释放，旧 Presenter 或旧场景对象仍可能收到消息，这就是常见的“页面关了却重复响应”问题。

## 11. 测试类的原理

测试方法也是普通 C# 方法，只是测试框架会自动：

1. 找到带 `[Test]` 或 `[UnityTest]` 的方法；
2. 为测试类创建独立实例；
3. 执行准备代码；
4. 调用被测试行为；
5. 用断言比较实际结果和预期结果；
6. 记录通过、失败和异常；
7. 执行清理代码。

常见 AAA 结构：

1. Arrange：准备对象和输入。
2. Act：执行被测试行为。
3. Assert：验证结果。

```csharp
[Test]
public void ModifyIncomingDamage_FiveTwelvePercentStacksAddSixtyPercent()
{
    // Arrange
    var handle = new BuffHandle(
        BuffFactoryRegistry.CreateWithBuiltIns());
    for (var i = 0; i < 5; i++)
    {
        handle.Apply(
            new BuffInfo(
                LightningVulnerabilityBuff.StableId,
                "weapon.thunder_blade",
                6f,
                0.12f,
                1,
                5));
    }

    var context = new DamageContext(100f, DamageElement.Electric);

    // Act
    var actual = handle.ModifyIncomingDamage(context).Amount;

    // Assert
    Assert.That(actual, Is.EqualTo(160f).Within(0.001f));
}
```

`[SetUp]` 在每个测试前执行，`[TearDown]` 在每个测试后执行。每个测试都必须能够单独运行，不能依赖另一个测试先帮它创建数据。

测试最大的作用不是证明“永远没有 Bug”，而是把重要规则变成可重复执行的契约。以后改伤害公式或装备槽位时，旧契约一旦被破坏就会立即指出具体位置。

## 12. EditMode 与 PlayMode

EditMode 适合：

- 伤害和属性公式；
- EventBus；
- 普通 C# 状态机；
- 背包、装备、Buff、任务领域模型；
- Presenter；
- 配置和预制体引用校验。

PlayMode 适合：

- 协程；
- `Destroy`；
- 物理帧和触发器；
- Animator；
- 玩家死亡后延迟复活；
- 敌人死亡动画后删除；
- YooAsset 场景加载；
- 完整启动流程。

优先写 EditMode 测试，因为它快、稳定、容易定位。只有必须依赖 Unity 帧循环时才写 PlayMode。

## 13. 只运行一个测试

打开：

```text
Window > General > Test Runner
```

展开测试树，选中某个测试方法，点击 `Run Selected`，或者右键这个方法选择 `Run`。

例如：

```text
Train.Tests.EditMode.Buffs.BuffHandleTests
    .ModifyIncomingDamage_FiveTwelvePercentStacksAddSixtyPercent
```

自动化工具也可以把完整名称传给 `test_names`，只执行这一项。因为测试之间不共享前置状态，单独运行和全量运行应该得到相同结果。

## 14. 当前测试目录

```text
Assets/Tests
├─ EditMode
│  ├─ Architecture
│  ├─ Buffs
│  ├─ Combat
│  ├─ Enemy
│  ├─ Equipment
│  ├─ GameFlow
│  ├─ Inventory
│  ├─ Characters
│  ├─ Dialogue
│  ├─ Quest
│  ├─ UI
│  └─ WorldInteraction
└─ PlayMode
   ├─ Assets
   ├─ Combat
   ├─ Enemy
   ├─ GameFlow
   └─ Inventory
```

## 15. 当前操作

- 移动：WASD。
- 普通攻击：鼠标左键。
- 翻滚：项目输入映射中的 Dodge。
- 交互/拾取：E。
- 背包：B 或 Tab。
- 装备：C。
- 任务：菜单导航中的“任务”。
- 角色：菜单导航中的“角色”。
- 对话继续：空格或回车。
- 对话选项：数字键 1-4。
- 关闭主菜单：Esc。

如果后续改键，优先修改 Input Action 配置和 UI 提示数据，不要在多个玩法类中散落新的硬编码按键。
