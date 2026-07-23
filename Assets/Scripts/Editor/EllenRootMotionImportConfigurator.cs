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
    /// 将动画目录中的位移策略同步到 Ellen FBX 的根运动导入设置。
    /// 原地和代码移动动画会烘焙水平根位移，只有 RootMotion 动画保留水平根位移供运行时接收。
    /// </summary>
    public static class EllenRootMotionImportConfigurator
    {
        private const string SourceRoot = "Assets/Arts/Player/Ellen/AnimationFBX";
        private const string CatalogPath = "Assets/Resources/Player/EllenAnimationCatalog.asset";
        private const string CommonPrefix = "Avatar_Female_Size02_Ellen_Ani_";

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
                var changed = false;
                for (var index = 0; index < clips.Length; index++)
                {
                    var clip = clips[index];
                    if (!TryGetMovementPolicy(catalog, clip.name, out var movementPolicy))
                    {
                        continue;
                    }

                    var keepHorizontalRootMotion = movementPolicy == PlayerAnimationMovementPolicy.RootMotion;
                    if (clip.lockRootPositionXZ == !keepHorizontalRootMotion &&
                        clip.lockRootHeightY &&
                        clip.lockRootRotation)
                    {
                        continue;
                    }

                    clip.lockRootPositionXZ = !keepHorizontalRootMotion;
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

            Debug.Log($"已按动画目录同步 Root Motion 导入设置，成功重新导入 {changedCount} 个 Ellen 动画 FBX，失败 {failedPaths.Count} 个。" +
                      "现在攻击和走跑不会保留水平根位移，翻滚会保留供 PlayerMotor 使用。");

            if (failedPaths.Count > 0)
            {
                Debug.LogError($"以下动画 FBX 未能同步：{string.Join("、", failedPaths)}");
            }
        }

        /// <summary>
        /// 输出关键动画当前的根位移导入结果，便于确认攻击被烘焙为原地而翻滚保留位移。
        /// </summary>
        [MenuItem("Train/Animation/诊断 Ellen Root Motion 导入设置")]
        public static void ReportKeySettings()
        {
            ReportClipSetting("Combat/Avatar_Female_Size02_Ellen_Ani_Attack_Normal_01_01.fbx");
            ReportClipSetting("Combat/Avatar_Female_Size02_Ellen_Ani_Evade_Front.fbx");
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
            Debug.Log($"{clip.name}：水平根位移={(clip.lockRootPositionXZ ? "已烘焙为原地" : "保留给运行时")}，" +
                      $"高度锁定={clip.lockRootHeightY}，旋转锁定={clip.lockRootRotation}。");
        }

        /// <summary>
        /// 根据动画剪辑名取得动画目录中配置的位移策略。
        /// </summary>
        /// <param name="catalog">包含全部 Ellen 动画定义的目录资源。</param>
        /// <param name="clipName">FBX 内动画剪辑名。</param>
        /// <param name="movementPolicy">查询成功时返回对应位移策略。</param>
        /// <returns>剪辑名可映射到已配置动画定义时返回 true。</returns>
        private static bool TryGetMovementPolicy(
            PlayerAnimationCatalog catalog,
            string clipName,
            out PlayerAnimationMovementPolicy movementPolicy)
        {
            var idName = clipName.Replace(CommonPrefix, string.Empty);
            if (Enum.TryParse(idName, out PlayerAnimationId id) && catalog.TryGet(id, out var definition))
            {
                movementPolicy = definition.MovementPolicy;
                return true;
            }

            movementPolicy = PlayerAnimationMovementPolicy.KeepInPlace;
            return false;
        }
    }
}
