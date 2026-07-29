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
    /// 统一 Ellen 原始 FBX 的根节点导入设置，并提供源动画曲线诊断。
    /// 当前运行时统一播放生成的原地副本；转向旋转由原地副本生成器烘焙为 Player 朝向曲线。
    /// </summary>
    public static class EllenRootMotionImportConfigurator
    {
        private const string SourceRoot = "Assets/Arts/Player/Ellen/AnimationFBX";
        private const string CatalogPath = "Assets/Resources/Player/EllenAnimationCatalog.asset";
        private const string CommonPrefix = "Avatar_Female_Size02_Ellen_Ani_";
        private const string RootMotionNodeName = "Bip001";
        private const string RootMotionNodePath = "Avatar_Female_Size02_Ellen/Bip001";

        /// <summary>
        /// 根据当前动画目录批量配置全部 Ellen FBX 的根位移导入规则。
        /// </summary>
        [MenuItem("Train/Animation/同步 Ellen Root Motion 导入设置")]
        public static void ApplySettings()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<PlayerAnimationCatalog>(CatalogPath);
            if (catalog == null)
            {
                Debug.LogError("无法同步 Ellen Root Motion：未找到动画目录资源。");
                return;
            }

            var changedCount = 0;
            var failedPaths = new List<string>();
            var paths = Directory.GetFiles(SourceRoot, "*.fbx", SearchOption.AllDirectories)
                .Select(path => path.Replace('\\', '/'));
            foreach (var path in paths)
            {
                if (AssetImporter.GetAtPath(path) is not ModelImporter importer)
                {
                    continue;
                }

                var clips = importer.clipAnimations;
                var changed = importer.motionNodeName != RootMotionNodeName;
                for (var index = 0; index < clips.Length; index++)
                {
                    var clip = clips[index];
                    if (!TryGetAnimationDefinition(
                            catalog,
                            clip.name,
                            out _,
                            out _))
                    {
                        continue;
                    }

                    if (!clip.lockRootPositionXZ &&
                        clip.lockRootHeightY &&
                        clip.lockRootRotation)
                    {
                        continue;
                    }

                    clip.lockRootPositionXZ = false;
                    clip.lockRootHeightY = true;
                    clip.lockRootRotation = true;
                    clips[index] = clip;
                    changed = true;
                }

                if (!changed)
                {
                    continue;
                }

                try
                {
                    importer.motionNodeName = RootMotionNodeName;
                    importer.clipAnimations = clips;
                    importer.SaveAndReimport();
                    changedCount++;
                }
                catch (Exception exception)
                {
                    failedPaths.Add(path);
                    Debug.LogError($"同步 {path} 的 Root Motion 导入设置失败：{exception.Message}");
                }
            }

            EllenAnimationCatalogGenerator.GenerateCatalog();
            Debug.Log($"已按动画目录同步 Root Motion 导入设置，成功重新导入 {changedCount} 个 Ellen 动画 FBX，失败 {failedPaths.Count} 个。" +
                      $"所有动画已使用实际蒙皮骨架节点 {RootMotionNodeName}（路径 {RootMotionNodePath}）作为 Root Motion Node；" +
                      "运行时动画目录会重新生成原地副本，TurnBack 的骨架根旋转会转为脚本朝向曲线。");

            if (failedPaths.Count > 0)
            {
                Debug.LogError($"以下动画 FBX 未能同步：{string.Join("、", failedPaths)}");
            }
        }

        /// <summary>
        /// 输出关键动画当前的根位移导入结果，确认水平位移统一从蒙皮骨架中提取。
        /// </summary>
        [MenuItem("Train/Animation/诊断 Ellen Root Motion 导入设置")]
        public static void ReportKeySettings()
        {
            ReportClipSetting("Combat/Avatar_Female_Size02_Ellen_Ani_Attack_Normal_01_01.fbx");
            ReportClipSetting("Combat/Avatar_Female_Size02_Ellen_Ani_Evade_Front.fbx");
            ReportClipSetting("Locomotion/Avatar_Female_Size02_Ellen_Ani_TurnBack.fbx");
            ReportClipSetting("Locomotion/Avatar_Female_Size02_Ellen_Ani_Walk.fbx");
        }

        /// <summary>
        /// 输出一个 FBX 动画剪辑的根位移锁定状态。
        /// </summary>
        /// <param name="relativePath">相对于 Ellen 动画根目录的 FBX 路径。</param>
        private static void ReportClipSetting(string relativePath)
        {
            var path = $"{SourceRoot}/{relativePath}";
            if (AssetImporter.GetAtPath(path) is not ModelImporter importer || importer.clipAnimations.Length == 0)
            {
                Debug.LogError($"无法读取 Root Motion 设置：{path}");
                return;
            }

            var clip = importer.clipAnimations[0];
            var animationClip = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<AnimationClip>()
                .FirstOrDefault(candidate => candidate.name == clip.name);
            Debug.Log($"{clip.name}：水平根位移={(clip.lockRootPositionXZ ? "仍留在骨骼姿势中" : "已提取为 Animator.deltaPosition")}，" +
                      $"高度锁定={clip.lockRootHeightY}，旋转锁定={clip.lockRootRotation}，" +
                      $"Root Motion Node={importer.motionNodeName}，" +
                      $"hasRootCurves={animationClip != null && animationClip.hasRootCurves}，" +
                      $"hasMotionCurves={animationClip != null && animationClip.hasMotionCurves}。");

            ReportRootPositionCurves(path, clip.name);
        }

        /// <summary>
        /// 输出 FBX 内靠近骨架顶部的 Transform 路径，帮助定位真正应作为 Root Motion 来源的骨骼。
        /// </summary>
        /// <param name="importer">提供骨架路径的模型导入器。</param>
        /// <param name="clipName">用于输出日志的动画剪辑名。</param>
        private static void ReportTopLevelTransformPaths(ModelImporter importer, string clipName)
        {
            var paths = importer.transformPaths
                .Where(path => !path.Contains("/", StringComparison.Ordinal))
                .ToArray();
            Debug.Log($"{clipName} 的顶层骨架路径：{string.Join("、", paths)}。" );
        }

        /// <summary>
        /// 输出实际蒙皮根骨骼的位置曲线范围，用于确认前滑位移来自哪一套骨架。
        /// </summary>
        /// <param name="path">待检查 FBX 的 Assets 路径。</param>
        /// <param name="clipName">待检查的 FBX 内动画剪辑名。</param>
        private static void ReportRootPositionCurves(string path, string clipName)
        {
            var animationClip = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<AnimationClip>()
                .FirstOrDefault(candidate => candidate.name == clipName);
            if (animationClip == null)
            {
                Debug.LogError($"无法读取动画曲线：{path} / {clipName}");
                return;
            }

            var curveReports = AnimationUtility.GetCurveBindings(animationClip)
                .Where(binding => binding.type == typeof(Transform) &&
                                  binding.path == RootMotionNodePath &&
                                  binding.propertyName.StartsWith("m_LocalPosition", StringComparison.Ordinal))
                .Select(binding => BuildCurveReport(animationClip, binding))
                .ToArray();
            Debug.Log($"{clipName} 的实际蒙皮根骨骼位置曲线：" +
                      $"{(curveReports.Length == 0 ? "已被提取为 Root Motion，未残留普通 Transform 曲线" : string.Join("；", curveReports))}。");
        }

        /// <summary>
        /// 将一条位置曲线整理为便于阅读的起点、终点和变化范围。
        /// </summary>
        /// <param name="animationClip">包含待检查曲线的动画剪辑。</param>
        /// <param name="binding">待检查的动画曲线绑定。</param>
        /// <returns>用于诊断日志的中文曲线摘要。</returns>
        private static string BuildCurveReport(AnimationClip animationClip, EditorCurveBinding binding)
        {
            var curve = AnimationUtility.GetEditorCurve(animationClip, binding);
            if (curve == null || curve.length == 0)
            {
                return $"{binding.propertyName}=无关键帧";
            }

            var values = curve.keys.Select(key => key.value).ToArray();
            return $"{binding.propertyName}：起点={values[0]:F4}，终点={values[^1]:F4}，" +
                   $"范围={values.Min():F4}~{values.Max():F4}";
        }

        /// <summary>
        /// 根据动画剪辑名取得动画标识与目录中配置的位移策略。
        /// </summary>
        /// <param name="catalog">包含全部 Ellen 动画定义的目录资源。</param>
        /// <param name="clipName">FBX 内动画剪辑名。</param>
        /// <param name="animationId">查询成功时返回对应的动画标识。</param>
        /// <param name="movementPolicy">查询成功时返回对应位移策略。</param>
        /// <returns>剪辑名可映射到已配置动画定义时返回 true。</returns>
        private static bool TryGetAnimationDefinition(
            PlayerAnimationCatalog catalog,
            string clipName,
            out PlayerAnimationId animationId,
            out PlayerAnimationMovementPolicy movementPolicy)
        {
            var idName = clipName.Replace(CommonPrefix, string.Empty);
            if (Enum.TryParse(idName, out animationId) &&
                catalog.TryGet(animationId, out var definition))
            {
                movementPolicy = definition.MovementPolicy;
                return true;
            }

            animationId = default;
            movementPolicy = PlayerAnimationMovementPolicy.KeepInPlace;
            return false;
        }
    }
}
