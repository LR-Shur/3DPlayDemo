using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Animancer;
using Train.Gameplay.Player.Animation.Data;
using UnityEditor;
using UnityEngine;

namespace Train.EditorTools
{
    /// <summary>
    /// 自动扫描 Ellen 的 FBX 动画，并生成已绑定全部 Clip 的动画目录资源。
    /// 目录资源只会在缺失时自动创建；需要重新扫描时可使用菜单命令手动执行。
    /// </summary>
    public static class EllenAnimationCatalogGenerator
    {
        private const string SourceRoot = "Assets/Arts/Player/Ellen/AnimationFBX";
        private const string TransitionRoot = "Assets/Arts/Player/Ellen/AnimationGenerated/Transitions";
        private const string TransitionEventRoot =
            "Assets/Arts/Player/Ellen/AnimationGenerated/Transitions/_Events";
        private const string ContextualTransitionRoot =
            "Assets/Arts/Player/Ellen/AnimationGenerated/Transitions/Locomotion";
        private const string CatalogPath = "Assets/Resources/Player/EllenAnimationCatalog.asset";
        private const string CommonPrefix = "Avatar_Female_Size02_Ellen_Ani_";
        private const float DefaultTurnMovementStartNormalizedTime = 0.52f;
        private const float DefaultTurnCompleteNormalizedTime = 0.60f;

        /// <summary>
        /// 在编辑器启动或脚本重载后自动补齐缺失的 Ellen 动画目录与 TransitionAsset。
        /// 资源数量不足时会继续迁移，避免上次生成中断后留下半套数据。
        /// </summary>
        [InitializeOnLoadMethod]
        private static void CreateCatalogWhenMissing()
        {
            EditorApplication.delayCall += () =>
            {
                if (AssetDatabase.LoadAssetAtPath<PlayerAnimationCatalog>(CatalogPath) == null ||
                    CountGeneratedTransitions() < CountSourceAnimations() ||
                    !HasRequiredContextualTransitions())
                {
                    GenerateCatalog();
                }
            };
        }

        /// <summary>
        /// 统计原始 FBX 动画数量，用于判断 TransitionAsset 迁移是否完整。
        /// </summary>
        /// <returns>Ellen 动画源目录中的 FBX 文件总数。</returns>
        private static int CountSourceAnimations()
        {
            return Directory.Exists(SourceRoot)
                ? Directory.GetFiles(SourceRoot, "*.fbx", SearchOption.AllDirectories).Length
                : 0;
        }

        /// <summary>
        /// 统计已经生成的 TransitionAsset 数量。
        /// </summary>
        /// <returns>Transition 输出目录中的资源文件总数。</returns>
        private static int CountGeneratedTransitions()
        {
            return Directory.Exists(TransitionRoot)
                ? Directory.GetFiles(TransitionRoot, "*.asset", SearchOption.AllDirectories).Length
                : 0;
        }

        /// <summary>
        /// 检查转身进入步行和奔跑所需的来源相关 TransitionAsset 是否完整。
        /// </summary>
        /// <returns>两个必需资源均已生成时返回 true。</returns>
        private static bool HasRequiredContextualTransitions()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<PlayerAnimationCatalog>(
                CatalogPath);
            return catalog != null &&
                   catalog.TryGetContextualTransition(
                       PlayerAnimationId.TurnBack,
                       PlayerAnimationId.Walk,
                       out _) &&
                   catalog.TryGetContextualTransition(
                       PlayerAnimationId.TurnBack,
                       PlayerAnimationId.Run,
                       out _);
        }

        /// <summary>
        /// 从 Unity 菜单重新扫描 FBX，并覆盖更新全部动画目录条目。
        /// </summary>
        [MenuItem("Train/Player/重新生成 Ellen 动画目录")]
        public static void GenerateCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<PlayerAnimationCatalog>(CatalogPath);
            if (catalog == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(CatalogPath)!);
                catalog = ScriptableObject.CreateInstance<PlayerAnimationCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            var definitions = new List<PlayerAnimationDefinition>();
            var paths = Directory.GetFiles(SourceRoot, "*.fbx", SearchOption.AllDirectories)
                .Select(path => path.Replace('\\', '/'))
                .OrderBy(path => path, StringComparer.Ordinal);

            foreach (var path in paths)
            {
                var idName = Path.GetFileNameWithoutExtension(path).Replace(CommonPrefix, string.Empty);
                if (!Enum.TryParse(idName, out PlayerAnimationId id))
                {
                    Debug.LogError($"无法为动画文件生成标识：{path}");
                    continue;
                }

                var sourceClip = AssetDatabase.LoadAllAssetsAtPath(path)
                    .OfType<AnimationClip>()
                    .FirstOrDefault(candidate => candidate.name == idName);
                if (sourceClip == null)
                {
                    Debug.LogError($"未找到 FBX 内名称为 {idName} 的动画剪辑：{path}");
                    continue;
                }

                var category = GetCategory(path);
                var loop = IsLoopingAnimation(id);
                var stateKind = GetStateKind(id);
                var movementPolicy = GetMovementPolicy(id);
                var rootMotionPositionScale = 1f;
                var authoredMotionDistance = GetDefaultAuthoredMotionDistance(id);
                var authoredMotionCurve = CreateDefaultAuthoredMotionCurve(id);
                var cancelStartNormalizedTime = GetDefaultCancelStartNormalizedTime(id);
                var cancelEndNormalizedTime = 1f;
                var cancelTargets = GetDefaultCancelTargets(id);
                if (catalog.TryGetStoredDefinition(id, out var existingDefinition) &&
                    existingDefinition.HasMovementPolicyConfigured)
                {
                    movementPolicy = existingDefinition.MovementPolicy;
                    rootMotionPositionScale = existingDefinition.RootMotionPositionScale;
                    if (existingDefinition.AuthoredMotionDistance > 0f)
                    {
                        authoredMotionDistance = existingDefinition.AuthoredMotionDistance;
                    }

                    if (existingDefinition.AuthoredMotionCurve != null &&
                        existingDefinition.AuthoredMotionCurve.length > 0)
                    {
                        authoredMotionCurve = existingDefinition.AuthoredMotionCurve;
                    }

                    if (existingDefinition.HasCancelRuleConfigured)
                    {
                        cancelStartNormalizedTime =
                            existingDefinition.CancelStartNormalizedTime;
                        cancelEndNormalizedTime = existingDefinition.CancelEndNormalizedTime;
                        cancelTargets = existingDefinition.CancelTargets;
                    }
                    else if (existingDefinition.HasActionTimingConfigured &&
                             !RequiresCancelRuleMigration(id))
                    {
                        cancelStartNormalizedTime =
                            existingDefinition.CancelStartNormalizedTime;
                    }
                }

                if (IsLegacyFullDurationMotionCurve(authoredMotionCurve))
                {
                    authoredMotionCurve = CreateDefaultAuthoredMotionCurve(id);
                }

                movementPolicy = EnforceCoreMovementPolicy(id, movementPolicy);
                if (movementPolicy != PlayerAnimationMovementPolicy.RootMotion)
                {
                    rootMotionPositionScale = 1f;
                }

                var playbackClip = EllenInPlaceAnimationGenerator.CreateOrUpdate(
                    sourceClip,
                    category,
                    id,
                    loop,
                    out var authoredTurnCurve);
                var transition = CreateOrLoadTransition(
                    playbackClip,
                    category,
                    id,
                    loop ? 0.15f : 0.1f);
                EnsureDefaultTransitionEvents(transition, id);
                definitions.Add(new PlayerAnimationDefinition(
                    id,
                    category,
                    transition,
                    stateKind,
                    movementPolicy,
                    rootMotionPositionScale,
                    movementPolicy == PlayerAnimationMovementPolicy.AuthoredMotion
                        ? authoredMotionDistance
                        : 0f,
                    movementPolicy == PlayerAnimationMovementPolicy.AuthoredMotion
                        ? authoredMotionCurve
                        : null,
                    authoredTurnCurve,
                    cancelStartNormalizedTime,
                    cancelEndNormalizedTime,
                    cancelTargets));
            }

            catalog.ReplaceDefinitions(definitions.ToArray());
            catalog.ReplaceContextualTransitions(
                CreateContextualTransitions(definitions));
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"Ellen 动画目录已生成，共收录 {definitions.Count} 个动画。" +
                      "全部条目均通过独立 TransitionAsset 播放原地 .anim 副本；" +
                      "已有 Transition 调参会被保留，原始 FBX 保持不变。", catalog);
        }

        /// <summary>
        /// 创建转身进入移动循环所需的来源相关过渡规则。
        /// 两条规则使用姿势匹配得到的步态相位，普通 Idle 进入 Walk 或 Run 不受影响。
        /// </summary>
        /// <param name="definitions">本次生成的完整动画定义集合。</param>
        /// <returns>需要写入动画目录的来源相关过渡规则。</returns>
        private static PlayerAnimationTransitionRule[] CreateContextualTransitions(
            IReadOnlyCollection<PlayerAnimationDefinition> definitions)
        {
            var walkDefinition = definitions.First(
                definition => definition.Id == PlayerAnimationId.Walk);
            var runDefinition = definitions.First(
                definition => definition.Id == PlayerAnimationId.Run);
            return new[]
            {
                new PlayerAnimationTransitionRule(
                    PlayerAnimationId.TurnBack,
                    PlayerAnimationId.Walk,
                    CreateOrLoadContextualTransition(
                        PlayerAnimationId.TurnBack,
                        walkDefinition,
                        0.625f)),
                new PlayerAnimationTransitionRule(
                    PlayerAnimationId.TurnBack,
                    PlayerAnimationId.Run,
                    CreateOrLoadContextualTransition(
                        PlayerAnimationId.TurnBack,
                        runDefinition,
                        0.61f)),
            };
        }

        /// <summary>
        /// 创建或读取一个来源相关的移动循环 TransitionAsset。
        /// 已存在资源保持 Inspector 调参，新资源从目标定义复制淡入时间并写入匹配后的起始相位。
        /// </summary>
        /// <param name="from">过渡来源动画标识。</param>
        /// <param name="targetDefinition">提供目标剪辑与默认淡入时间的动画定义。</param>
        /// <param name="normalizedStartTime">姿势匹配得到的目标循环起始相位。</param>
        /// <returns>可直接用于指定来源到目标组合的 TransitionAsset。</returns>
        private static TransitionAssetBase CreateOrLoadContextualTransition(
            PlayerAnimationId from,
            PlayerAnimationDefinition targetDefinition,
            float normalizedStartTime)
        {
            var assetPath =
                $"{ContextualTransitionRoot}/{from}_To_{targetDefinition.Id}.asset";
            var existingAsset =
                AssetDatabase.LoadAssetAtPath<TransitionAssetBase>(assetPath);
            if (existingAsset != null)
            {
                return existingAsset;
            }

            var transitionAsset = ScriptableObject.CreateInstance<TransitionAsset>();
            transitionAsset.name = $"{from}_To_{targetDefinition.Id}";
            transitionAsset.Transition = new ClipTransition
            {
                Clip = targetDefinition.PrimaryClip,
                FadeDuration = targetDefinition.FadeDuration,
                Speed = 1f,
                NormalizedStartTime = normalizedStartTime,
            };
            AssetDatabase.CreateAsset(transitionAsset, assetPath);
            return transitionAsset;
        }

        /// <summary>
        /// 创建或读取指定动画的独立 TransitionAsset。
        /// 已存在的资源不会被重新初始化，避免覆盖在 Inspector 中手动调节的淡入、速度、事件和起始时间。
        /// </summary>
        /// <param name="playbackClip">Transition 首次创建时绑定的原地动画剪辑。</param>
        /// <param name="category">用于组织 Transition 资源目录的动画分类。</param>
        /// <param name="id">用于生成稳定资源名称的动画标识。</param>
        /// <param name="defaultFadeDuration">资源首次创建时使用的默认淡入时间。</param>
        /// <returns>可由目录和 Animancer 直接使用的 TransitionAsset。</returns>
        private static TransitionAssetBase CreateOrLoadTransition(
            AnimationClip playbackClip,
            PlayerAnimationCategory category,
            PlayerAnimationId id,
            float defaultFadeDuration)
        {
            var folderPath = $"{TransitionRoot}/{category}";
            Directory.CreateDirectory(folderPath);
            var assetPath = $"{folderPath}/{id}.asset";
            var existingAsset = AssetDatabase.LoadAssetAtPath<TransitionAssetBase>(assetPath);
            if (existingAsset != null)
            {
                return existingAsset;
            }

            var transitionAsset = ScriptableObject.CreateInstance<TransitionAsset>();
            transitionAsset.name = id.ToString();
            transitionAsset.Transition = new ClipTransition
            {
                Clip = playbackClip,
                FadeDuration = defaultFadeDuration,
                Speed = 1f,
                NormalizedStartTime = 0f,
            };
            AssetDatabase.CreateAsset(transitionAsset, assetPath);
            return transitionAsset;
        }

        /// <summary>
        /// 为需要动画姿势关键点的 TransitionAsset 补齐默认命名事件。
        /// 已经存在的同名事件不会被覆盖，动画师可以继续在资源中微调其时间。
        /// </summary>
        /// <param name="transitionAsset">需要检查的 TransitionAsset。</param>
        /// <param name="id">该资源对应的动画标识。</param>
        private static void EnsureDefaultTransitionEvents(
            TransitionAssetBase transitionAsset,
            PlayerAnimationId id)
        {
            if (id != PlayerAnimationId.TurnBack || transitionAsset == null)
            {
                return;
            }

            var serializedTransition = new SerializedObject(transitionAsset);
            var normalizedTimes = serializedTransition.FindProperty(
                "_Transition._Events._NormalizedTimes");
            var callbacks = serializedTransition.FindProperty(
                "_Transition._Events._Callbacks");
            var names = serializedTransition.FindProperty(
                "_Transition._Events._Names");
            if (normalizedTimes == null || callbacks == null || names == null)
            {
                Debug.LogError(
                    $"TransitionAsset {transitionAsset.name} 不包含可编辑的 Animancer 事件序列。",
                    transitionAsset);
                return;
            }

            EnsureNamedTransitionEvent(
                normalizedTimes,
                callbacks,
                names,
                CreateOrLoadEventNameAsset(
                    PlayerAnimationEventNames.TurnMovementStart),
                DefaultTurnMovementStartNormalizedTime);
            EnsureNamedTransitionEvent(
                normalizedTimes,
                callbacks,
                names,
                CreateOrLoadEventNameAsset(
                    PlayerAnimationEventNames.TurnComplete),
                DefaultTurnCompleteNormalizedTime);
            EnsureSerializedEndEvent(normalizedTimes, callbacks);
            serializedTransition.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(transitionAsset);
        }

        /// <summary>
        /// 确保 TransitionAsset 包含指定命名事件，已有事件时间保持不变。
        /// 新事件会按归一化时间插入，保证 Animancer 依照动画顺序触发。
        /// </summary>
        /// <param name="normalizedTimes">TransitionAsset 的事件时间数组。</param>
        /// <param name="callbacks">TransitionAsset 的事件回调数组。</param>
        /// <param name="names">TransitionAsset 的事件名称数组。</param>
        /// <param name="eventNameAsset">需要写入的稳定事件名称资源。</param>
        /// <param name="defaultNormalizedTime">事件首次创建时使用的默认归一化时间。</param>
        private static void EnsureNamedTransitionEvent(
            SerializedProperty normalizedTimes,
            SerializedProperty callbacks,
            SerializedProperty names,
            StringAsset eventNameAsset,
            float defaultNormalizedTime)
        {
            for (var i = 0; i < names.arraySize; i++)
            {
                if (names.GetArrayElementAtIndex(i).objectReferenceValue == eventNameAsset)
                {
                    while (callbacks.arraySize <= i)
                    {
                        callbacks.InsertArrayElementAtIndex(callbacks.arraySize);
                    }

                    var existingCallback = callbacks.GetArrayElementAtIndex(i);
                    if (existingCallback.managedReferenceValue == null)
                    {
                        existingCallback.managedReferenceValue = new Animancer.UnityEvent();
                    }

                    return;
                }
            }

            var insertionIndex = normalizedTimes.arraySize;
            for (var i = 0; i < normalizedTimes.arraySize; i++)
            {
                if (normalizedTimes.GetArrayElementAtIndex(i).floatValue >
                    defaultNormalizedTime)
                {
                    insertionIndex = i;
                    break;
                }
            }

            normalizedTimes.InsertArrayElementAtIndex(insertionIndex);
            callbacks.InsertArrayElementAtIndex(insertionIndex);
            names.InsertArrayElementAtIndex(insertionIndex);
            normalizedTimes.GetArrayElementAtIndex(insertionIndex).floatValue =
                defaultNormalizedTime;
            callbacks.GetArrayElementAtIndex(insertionIndex).managedReferenceValue =
                new Animancer.UnityEvent();
            names.GetArrayElementAtIndex(insertionIndex).objectReferenceValue = eventNameAsset;
        }

        /// <summary>
        /// 确保 Animancer 序列在普通事件之后仍保留独立的结束事件时间。
        /// 序列化格式会把最后一个时间解释为 End Event，因此普通事件不能占用最后一项。
        /// </summary>
        /// <param name="normalizedTimes">TransitionAsset 的事件时间数组。</param>
        /// <param name="callbacks">TransitionAsset 的普通事件回调数组。</param>
        private static void EnsureSerializedEndEvent(
            SerializedProperty normalizedTimes,
            SerializedProperty callbacks)
        {
            while (normalizedTimes.arraySize <= callbacks.arraySize)
            {
                var endIndex = normalizedTimes.arraySize;
                normalizedTimes.InsertArrayElementAtIndex(endIndex);
                normalizedTimes.GetArrayElementAtIndex(endIndex).floatValue = float.NaN;
            }
        }

        /// <summary>
        /// 创建或读取 Animancer 命名事件使用的共享 StringAsset。
        /// TransitionAsset 通过该资源保存稳定事件名，运行时状态再绑定本次播放的回调。
        /// </summary>
        /// <param name="eventName">需要创建或读取的事件名称。</param>
        /// <returns>可写入 Animancer 事件名称数组的 StringAsset。</returns>
        private static StringAsset CreateOrLoadEventNameAsset(string eventName)
        {
            Directory.CreateDirectory(TransitionEventRoot);
            var assetPath = $"{TransitionEventRoot}/{eventName}.asset";
            var eventNameAsset = AssetDatabase.LoadAssetAtPath<StringAsset>(assetPath);
            if (eventNameAsset != null)
            {
                return eventNameAsset;
            }

            eventNameAsset = ScriptableObject.CreateInstance<StringAsset>();
            eventNameAsset.name = eventName;
            AssetDatabase.CreateAsset(eventNameAsset, assetPath);
            return eventNameAsset;
        }

        /// <summary>
        /// 根据动画文件所在文件夹确定其玩法分类。
        /// </summary>
        /// <param name="path">动画 FBX 的 Assets 相对路径。</param>
        /// <returns>对应的动画分类。</returns>
        private static PlayerAnimationCategory GetCategory(string path)
        {
            if (path.Contains("/Locomotion/", StringComparison.Ordinal)) return PlayerAnimationCategory.Locomotion;
            if (path.Contains("/Combat/", StringComparison.Ordinal)) return PlayerAnimationCategory.Combat;
            if (path.Contains("/Reaction/", StringComparison.Ordinal)) return PlayerAnimationCategory.Reaction;
            if (path.Contains("/Transition/", StringComparison.Ordinal)) return PlayerAnimationCategory.Transition;
            if (path.Contains("/Interaction/", StringComparison.Ordinal)) return PlayerAnimationCategory.Interaction;
            return PlayerAnimationCategory.Cinematic;
        }

        /// <summary>
        /// 判断需要在运行时强制循环播放的动画。
        /// </summary>
        /// <param name="id">待判断的动画标识。</param>
        /// <returns>动画应循环播放时返回 true。</returns>
        private static bool IsLoopingAnimation(PlayerAnimationId id)
        {
            return id is PlayerAnimationId.Idle
                or PlayerAnimationId.Idle_AFK
                or PlayerAnimationId.Walk
                or PlayerAnimationId.Run;
        }

        /// <summary>
        /// 获取新动画目录条目的默认位移策略。
        /// 走跑采用代码移动，翻滚采用数据驱动位移曲线，其余动画默认保持原地。
        /// </summary>
        /// <param name="id">待判断的动画标识。</param>
        /// <returns>该动画默认采用的水平位移策略。</returns>
        private static PlayerAnimationMovementPolicy GetMovementPolicy(PlayerAnimationId id)
        {
            if (id is PlayerAnimationId.Walk or
                PlayerAnimationId.Run or
                PlayerAnimationId.Walk_Start)
            {
                return PlayerAnimationMovementPolicy.ScriptedMovement;
            }

            return id is PlayerAnimationId.Evade_Front or PlayerAnimationId.Evade_Back ||
                   UsesAuthoredCombatMotion(id)
                ? PlayerAnimationMovementPolicy.AuthoredMotion
                : PlayerAnimationMovementPolicy.KeepInPlace;
        }

        /// <summary>
        /// 固定当前基础控制器必须遵守的核心位移规则，同时允许其他动画继续使用目录中的手动配置。
        /// </summary>
        /// <param name="id">待校验的动画标识。</param>
        /// <param name="configuredPolicy">目录中已经保存的位移策略。</param>
        /// <returns>符合当前玩家状态机约定的最终位移策略。</returns>
        private static PlayerAnimationMovementPolicy EnforceCoreMovementPolicy(
            PlayerAnimationId id,
            PlayerAnimationMovementPolicy configuredPolicy)
        {
            if (id is PlayerAnimationId.Walk or
                PlayerAnimationId.Run or
                PlayerAnimationId.Walk_Start)
            {
                return PlayerAnimationMovementPolicy.ScriptedMovement;
            }

            if (id is PlayerAnimationId.Evade_Front or PlayerAnimationId.Evade_Back)
            {
                return PlayerAnimationMovementPolicy.AuthoredMotion;
            }

            if (UsesAuthoredCombatMotion(id))
            {
                return PlayerAnimationMovementPolicy.AuthoredMotion;
            }

            return configuredPolicy;
        }

        /// <summary>
        /// 返回关键动作在未手动调参时使用的默认位移总距离。
        /// 前翻滚稍长，后撤翻滚稍短，后续可在动画目录中逐条微调。
        /// </summary>
        /// <param name="id">待配置的动画标识。</param>
        /// <returns>该动画完整播放期间计划移动的米数。</returns>
        private static float GetDefaultAuthoredMotionDistance(PlayerAnimationId id)
        {
            return id switch
            {
                PlayerAnimationId.Evade_Front => 2.8f,
                PlayerAnimationId.Evade_Back => 2.2f,
                PlayerAnimationId.Attack_AssaultAid => 0.45f,
                PlayerAnimationId.Attack_AssaultAid_Back => 0.35f,
                PlayerAnimationId.Attack_AssaultAid_Near => 0.4f,
                PlayerAnimationId.Attack_AssaultAid_Near_Back => 0.3f,
                PlayerAnimationId.Attack_Dash_Start_01 => 0.2f,
                PlayerAnimationId.Attack_Dash_Start_02 => 0.2f,
                PlayerAnimationId.Attack_Dash_Loop_01 => 0.55f,
                PlayerAnimationId.Attack_Dash_Slash_01 => 0.55f,
                PlayerAnimationId.Attack_Dash_Cut_01 => 0.45f,
                PlayerAnimationId.Attack_Rush => 0.9f,
                _ => 0f,
            };
        }

        /// <summary>
        /// 创建翻滚默认使用的归一化位移曲线，使动作起步和收尾平滑而总距离保持稳定。
        /// </summary>
        /// <returns>横轴为动画进度、纵轴为累计位移比例的零到一曲线。</returns>
        private static AnimationCurve CreateDefaultAuthoredMotionCurve(PlayerAnimationId id)
        {
            if (id is PlayerAnimationId.Evade_Front or PlayerAnimationId.Evade_Back)
            {
                return new AnimationCurve(
                    new Keyframe(0f, 0f),
                    new Keyframe(0.08f, 0.02f),
                    new Keyframe(0.22f, 0.3f),
                    new Keyframe(0.38f, 0.9f),
                    new Keyframe(0.5f, 1f),
                    new Keyframe(1f, 1f));
            }

            return AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        }

        /// <summary>
        /// 返回每个动画在状态机中应归属的处理类别。
        /// 全部动画标识都在此处被显式归类，后续新增剪辑只需补充对应规则。
        /// </summary>
        /// <param name="id">待归类的动画标识。</param>
        /// <returns>该动画应进入的玩家状态类别。</returns>
        private static PlayerAnimationStateKind GetStateKind(PlayerAnimationId id)
        {
            if (id is PlayerAnimationId.Idle or PlayerAnimationId.Idle_AFK)
            {
                return PlayerAnimationStateKind.LocomotionIdle;
            }

            if (id == PlayerAnimationId.Walk_Start)
            {
                return PlayerAnimationStateKind.LocomotionStart;
            }

            if (id is PlayerAnimationId.Walk or PlayerAnimationId.Run)
            {
                return PlayerAnimationStateKind.LocomotionLoop;
            }

            if (id is PlayerAnimationId.Walk_Start_End or
                PlayerAnimationId.Walk_End or
                PlayerAnimationId.Run_End)
            {
                return PlayerAnimationStateKind.LocomotionStop;
            }

            if (id == PlayerAnimationId.TurnBack)
            {
                return PlayerAnimationStateKind.LocomotionTurn;
            }

            if (id is PlayerAnimationId.Evade_Front or PlayerAnimationId.Evade_Back)
            {
                return PlayerAnimationStateKind.Dodge;
            }

            if (id.ToString().StartsWith("Attack_Normal_", StringComparison.Ordinal))
            {
                return PlayerAnimationStateKind.PrimaryAttack;
            }

            if (id.ToString().StartsWith("Attack_", StringComparison.Ordinal))
            {
                return PlayerAnimationStateKind.CombatAction;
            }

            if (id is PlayerAnimationId.Hit_L_Front or
                PlayerAnimationId.Hit_L_Back or
                PlayerAnimationId.Hit_H_Front or
                PlayerAnimationId.Hit_H_Back or
                PlayerAnimationId.HitFly_Front or
                PlayerAnimationId.HitFly_Back)
            {
                return PlayerAnimationStateKind.Reaction;
            }

            if (id == PlayerAnimationId.Death)
            {
                return PlayerAnimationStateKind.Death;
            }

            if (id.ToString().StartsWith("Switch", StringComparison.Ordinal))
            {
                return PlayerAnimationStateKind.Transition;
            }

            if (id == PlayerAnimationId.QuestStart)
            {
                return PlayerAnimationStateKind.Interaction;
            }

            return PlayerAnimationStateKind.Cinematic;
        }

        /// <summary>
        /// 返回默认允许移动输入接管当前动作的动画进度。
        /// 当前仅翻滚在主体位移结束后开放取消窗口，其余一次性动作默认等待自然结束。
        /// </summary>
        /// <param name="id">待配置的动画标识。</param>
        /// <returns>零到一的最早移动取消进度。</returns>
        private static float GetDefaultCancelStartNormalizedTime(PlayerAnimationId id)
        {
            return id switch
            {
                PlayerAnimationId.Walk_Start or
                PlayerAnimationId.Walk_Start_End or
                PlayerAnimationId.Walk_End or
                PlayerAnimationId.Run_End => 0f,

                PlayerAnimationId.TurnBack => 0.15f,

                PlayerAnimationId.Evade_Front or
                PlayerAnimationId.Evade_Back => 0.42f,

                PlayerAnimationId.Attack_Dash_Slash_01 => 0.55f,
                PlayerAnimationId.Attack_Dash_End_01 => 0.08f,

                PlayerAnimationId.Attack_Rush => 0.62f,
                PlayerAnimationId.Attack_Rush_End => 0.08f,

                PlayerAnimationId.Attack_ParryAid_L or
                PlayerAnimationId.Attack_ParryAid_H => 0.58f,
                PlayerAnimationId.Attack_ParryAid_L_End or
                PlayerAnimationId.Attack_ParryAid_H_End or
                PlayerAnimationId.Attack_Counter_End => 0.08f,

                PlayerAnimationId.Attack_Counter => 0.65f,
                _ => 1f,
            };
        }

        /// <summary>
        /// 返回动画在取消窗口内允许切换到的行为集合。
        /// 移动收势可立即回到移动；战斗收势同时允许移动或翻滚接管。
        /// </summary>
        /// <param name="id">待配置的动画标识。</param>
        /// <returns>允许接管当前动画的行为位标记。</returns>
        private static PlayerAnimationCancelTarget GetDefaultCancelTargets(PlayerAnimationId id)
        {
            if (id is PlayerAnimationId.Walk_Start or
                PlayerAnimationId.Walk_Start_End or
                PlayerAnimationId.Walk_End or
                PlayerAnimationId.Run_End or
                PlayerAnimationId.TurnBack or
                PlayerAnimationId.Evade_Front or
                PlayerAnimationId.Evade_Back)
            {
                return PlayerAnimationCancelTarget.Movement;
            }

            if (id is PlayerAnimationId.Attack_Dash_Slash_01 or
                PlayerAnimationId.Attack_Dash_End_01 or
                PlayerAnimationId.Attack_Rush or
                PlayerAnimationId.Attack_Rush_End or
                PlayerAnimationId.Attack_ParryAid_L or
                PlayerAnimationId.Attack_ParryAid_H or
                PlayerAnimationId.Attack_ParryAid_L_End or
                PlayerAnimationId.Attack_ParryAid_H_End or
                PlayerAnimationId.Attack_Counter or
                PlayerAnimationId.Attack_Counter_End)
            {
                return PlayerAnimationCancelTarget.Movement |
                       PlayerAnimationCancelTarget.Dodge;
            }

            return PlayerAnimationCancelTarget.None;
        }

        /// <summary>
        /// 判断旧目录条目是否必须升级为新版默认取消规则。
        /// 这些动作正是旧版容易产生长后摇或转向锁定的入口，首次升级时不能继续沿用旧数值。
        /// </summary>
        /// <param name="id">待检查的动画标识。</param>
        /// <returns>应该采用新版默认取消起点时返回 true。</returns>
        private static bool RequiresCancelRuleMigration(PlayerAnimationId id)
        {
            return id is PlayerAnimationId.Walk_Start or
                PlayerAnimationId.Walk_Start_End or
                PlayerAnimationId.Walk_End or
                PlayerAnimationId.Run_End or
                PlayerAnimationId.TurnBack or
                PlayerAnimationId.Attack_Dash_End_01 or
                PlayerAnimationId.Attack_Rush_End or
                PlayerAnimationId.Attack_ParryAid_L_End or
                PlayerAnimationId.Attack_ParryAid_H_End or
                PlayerAnimationId.Attack_Counter_End;
        }

        /// <summary>
        /// 判断动作是否应由目录中的距离曲线驱动角色前进。
        /// 原始 FBX 已被生成器制作成原地动画，因此冲刺、突进和突击的逻辑位移必须显式配置在这里。
        /// </summary>
        /// <param name="id">待判断的动画标识。</param>
        /// <returns>动作需要使用数据驱动位移时返回 true。</returns>
        private static bool UsesAuthoredCombatMotion(PlayerAnimationId id)
        {
            return id is PlayerAnimationId.Attack_AssaultAid or
                PlayerAnimationId.Attack_AssaultAid_Back or
                PlayerAnimationId.Attack_AssaultAid_Near or
                PlayerAnimationId.Attack_AssaultAid_Near_Back or
                PlayerAnimationId.Attack_Dash_Start_01 or
                PlayerAnimationId.Attack_Dash_Start_02 or
                PlayerAnimationId.Attack_Dash_Loop_01 or
                PlayerAnimationId.Attack_Dash_Slash_01 or
                PlayerAnimationId.Attack_Dash_Cut_01 or
                PlayerAnimationId.Attack_Rush;
        }

        /// <summary>
        /// 判断是否仍为旧版贯穿整段动作的两关键帧平滑曲线。
        /// 识别到该曲线时会升级为翻滚主体阶段完成位移、收招阶段停止平移的新默认曲线。
        /// </summary>
        /// <param name="curve">需要检查的累计位移曲线。</param>
        /// <returns>曲线符合旧版默认形态时返回 true。</returns>
        private static bool IsLegacyFullDurationMotionCurve(AnimationCurve curve)
        {
            if (curve == null || curve.length != 2)
            {
                return false;
            }

            var first = curve.keys[0];
            var last = curve.keys[1];
            return Mathf.Approximately(first.time, 0f) &&
                   Mathf.Approximately(first.value, 0f) &&
                   Mathf.Approximately(last.time, 1f) &&
                   Mathf.Approximately(last.value, 1f);
        }
    }
}
