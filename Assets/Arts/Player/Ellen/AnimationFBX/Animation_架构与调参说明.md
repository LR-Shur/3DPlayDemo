# Ellen 动画架构与调参说明

## 当前采用的分层

```text
输入层
  ↓ 只产生“移动、攻击、翻滚”等意图
玩家状态机
  ↓ 只决定当前行为和 PlayerAnimationId
PlayerAnimationCatalog
  ↓ 将稳定 ID 映射到玩法元数据与 TransitionAsset
TransitionAsset
  ↓ 管理动画、淡入、速度、起始时间和可复用事件
Animancer
  ↓ 创建并播放 AnimancerState
Animator / 模型
```

状态机不直接保存 `AnimationClip`，也不直接填写淡入时间。播放层统一调用
`AnimancerComponent.Play(TransitionAsset)`。这样以后把单动画替换为 Mixer、
方向混合或自定义 Transition 时，不需要改攻击状态、移动状态或输入代码。

## 资源放在哪里

- 原始 FBX：`Assets/Arts/Player/Ellen/AnimationFBX`
- 自动生成的原地动画：`Assets/Arts/Player/Ellen/AnimationGenerated/InPlace`
- 每个动画的 TransitionAsset：`Assets/Arts/Player/Ellen/AnimationGenerated/Transitions`
- ID 与玩法数据目录：`Assets/Resources/Player/EllenAnimationCatalog.asset`

原始 FBX 不直接参与运行时播放。生成器会保留完整骨骼动画，并移除会让表现模型脱离
Player 逻辑根的水平根骨骼位移。

## 平时应该在哪里调

选中对应的 TransitionAsset，可以直接调整：

- `Fade Duration`：切入该动作所需的淡入时间。
- `Speed`：动画播放速度；攻击后摇过长时，优先检查动作拆分和取消窗口，再适量提高速度。
- `Normalized Start Time`：动画从哪个归一化进度开始。默认是 `0`，确保重复攻击会从头播放。
- `Events`：与动画资源自身相关、可供所有角色实例共享的时间点事件。
- `Clip`：当前实际播放的原地动画；也可以把整个资源替换为其他 TransitionAsset 类型。

角色实例专属的结束回调不会写进共享 TransitionAsset，而是绑定到每次播放返回的
`AnimancerState`。否则多个角色共用同一资源时，回调会互相覆盖。

`TurnBack` 还包含名为 `TurnComplete` 的事件，默认位于 `0.60`。它表示转身主体动作
已经完成，可以面向目标方向并淡入移动循环。持续反向输入等待该事件；玩家改变方向或
松开输入时才使用 Catalog 中的取消窗口，因此“正常完成”和“主动取消”不会再混用。

## 仍然保留在 PlayerAnimationDefinition 的内容

以下数据属于玩法规则，不属于动画过渡资源：

- 动画稳定 ID 与分类。
- 负责处理动画的状态类别。
- 位移策略：代码移动、数据曲线位移、Root Motion 或保持原地。
- 翻滚、突进等动作的总位移和累计位移曲线。
- 取消窗口的开始、结束进度，以及允许接管动作的行为类型。

这能让动画师调播放表现、程序调玩法规则，双方不会修改同一组字段。

### 取消窗口怎么配置

选中 `Assets/Resources/Player/EllenAnimationCatalog.asset`，展开某个动画定义：

- `Cancel Start Normalized Time`：从动画的哪个归一化进度开始允许接管。
- `Cancel End Normalized Time`：到哪个归一化进度停止允许接管。
- `Cancel Targets`：窗口内允许哪些行为接管，可组合选择 `Movement`、`Dodge`、`Attack`、`Skill`。

例如 `Attack_Rush_End` 默认从 `0.08` 开始允许 `Movement | Dodge`，所以收势刚开始后，
移动或翻滚就能自然切走。没有勾选目标的动作不会因为一个含糊的“可打断”布尔值而被
所有行为随意打断。

`TurnBack` 默认从 `0.15` 开始允许 `Movement` 取消。这个窗口只处理玩家中途松开或
改变方向；持续保持反向输入时会等待 TransitionAsset 的 `TurnComplete` 事件。

## 移动子状态文件

- `IdleState`：待机。
- `WalkStartState`：开始走路；松键会立刻进入短收势，继续按住则自然进入循环。
- `WalkState`：持续走路。
- `WalkStopState`：走路收势，可被新的移动输入立即接管。
- `RunState`：持续跑步。
- `RunStopState`：跑步收势，可被新的移动输入立即接管。
- `TurnState`：反向转身；持续反向时在转身主体完成后接管，方向再次变化时立即重算。
- `LocomotionState`：移动子状态机入口，只负责公共输入和子状态切换，不再塞入所有时序细节。

## 重新生成

执行 Unity 菜单 `Train/Player/重新生成 Ellen 动画目录`。

生成器只会初始化缺失的 TransitionAsset。已经存在的 TransitionAsset 不会被覆盖，
所以手动调整的淡入、速度、起始时间和事件能够保留。已经使用新版取消规则的 Catalog
条目也会保留；旧版的移动收势、转身和战斗收势会在首次升级时采用新版默认值，避免旧的
长后摇数值继续覆盖修复结果。
