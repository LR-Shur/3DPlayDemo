using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private const string CatalogPath = "Assets/Resources/Player/EllenAnimationCatalog.asset";
        private const string CommonPrefix = "Avatar_Female_Size02_Ellen_Ani_";

        /// <summary>
        /// 在编辑器启动或脚本重载后自动补齐缺失的 Ellen 动画目录资源。
        /// </summary>
        [InitializeOnLoadMethod]
        private static void CreateCatalogWhenMissing()
        {
            EditorApplication.delayCall += () =>
            {
                if (AssetDatabase.LoadAssetAtPath<PlayerAnimationCatalog>(CatalogPath) == null)
                {
                    GenerateCatalog();
                }
            };
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
                var movementCancelStartNormalizedTime = GetDefaultMovementCancelStartNormalizedTime(id);
                if (catalog.TryGet(id, out var existingDefinition) &&
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

                    if (existingDefinition.HasActionTimingConfigured)
                    {
                        movementCancelStartNormalizedTime =
                            existingDefinition.MovementCancelStartNormalizedTime;
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

                var playbackClip = EllenInPlaceAnimationGenerator.CreateOrUpdate(sourceClip, category, id);
                definitions.Add(new PlayerAnimationDefinition(
                    id,
                    category,
                    playbackClip,
                    loop,
                    loop ? 0.15f : 0.1f,
                    stateKind,
                    movementPolicy,
                    rootMotionPositionScale,
                    movementPolicy == PlayerAnimationMovementPolicy.AuthoredMotion
                        ? authoredMotionDistance
                        : 0f,
                    movementPolicy == PlayerAnimationMovementPolicy.AuthoredMotion
                        ? authoredMotionCurve
                        : null,
                    movementCancelStartNormalizedTime,
                    GetCanBeInterrupted(id)));
            }

            catalog.ReplaceDefinitions(definitions.ToArray());
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"Ellen 动画目录已生成，共收录 {definitions.Count} 个动画。" +
                      "全部条目均引用自动生成的原地 .anim 副本；翻滚位移由动画定义中的距离曲线驱动，原始 FBX 保持不变。", catalog);
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
        private static float GetDefaultMovementCancelStartNormalizedTime(PlayerAnimationId id)
        {
            return id switch
            {
                PlayerAnimationId.Evade_Front or
                PlayerAnimationId.Evade_Back => 0.42f,

                PlayerAnimationId.Attack_Dash_Slash_01 => 0.55f,
                PlayerAnimationId.Attack_Dash_End_01 => 0.12f,

                PlayerAnimationId.Attack_Rush => 0.62f,
                PlayerAnimationId.Attack_Rush_End => 0.12f,

                PlayerAnimationId.Attack_ParryAid_L or
                PlayerAnimationId.Attack_ParryAid_H => 0.58f,
                PlayerAnimationId.Attack_ParryAid_L_End or
                PlayerAnimationId.Attack_ParryAid_H_End or
                PlayerAnimationId.Attack_Counter_End => 0.12f,

                PlayerAnimationId.Attack_Counter => 0.65f,
                _ => 1f,
            };
        }

        /// <summary>
        /// 返回动画是否允许被移动输入提前打断。
        /// 当前仅移动循环动作可被输入随时切换；其余动作由具体状态或取消窗口控制。
        /// </summary>
        /// <param name="id">待配置的动画标识。</param>
        /// <returns>动画允许常规中断时返回 true。</returns>
        private static bool GetCanBeInterrupted(PlayerAnimationId id)
        {
            return id is PlayerAnimationId.Idle or PlayerAnimationId.Idle_AFK or
                PlayerAnimationId.Walk or PlayerAnimationId.Run;
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
