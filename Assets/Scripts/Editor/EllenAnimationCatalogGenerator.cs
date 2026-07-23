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

                var clip = AssetDatabase.LoadAllAssetsAtPath(path)
                    .OfType<AnimationClip>()
                    .FirstOrDefault(candidate => candidate.name == idName);
                if (clip == null)
                {
                    Debug.LogError($"未找到 FBX 内名称为 {idName} 的动画剪辑：{path}");
                    continue;
                }

                var category = GetCategory(path);
                var loop = IsLoopingAnimation(id);
                var movementPolicy = GetMovementPolicy(id);
                var rootMotionPositionScale = 1f;
                if (catalog.TryGet(id, out var existingDefinition) &&
                    existingDefinition.HasMovementPolicyConfigured)
                {
                    movementPolicy = existingDefinition.MovementPolicy;
                    rootMotionPositionScale = existingDefinition.RootMotionPositionScale;
                }

                definitions.Add(new PlayerAnimationDefinition(
                    id,
                    category,
                    clip,
                    loop,
                    loop ? 0.15f : 0.1f,
                    movementPolicy,
                    rootMotionPositionScale,
                    loop));
            }

            catalog.ReplaceDefinitions(definitions.ToArray());
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"Ellen 动画目录已生成，共收录 {definitions.Count} 个动画。", catalog);
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
                or PlayerAnimationId.Run
                or PlayerAnimationId.Attack_Dash_Loop_01;
        }

        /// <summary>
        /// 获取新动画目录条目的默认位移策略。
        /// 走跑采用代码移动，翻滚采用 Root Motion，其余动画默认保持原地。
        /// </summary>
        /// <param name="id">待判断的动画标识。</param>
        /// <returns>该动画默认采用的水平位移策略。</returns>
        private static PlayerAnimationMovementPolicy GetMovementPolicy(PlayerAnimationId id)
        {
            if (id is PlayerAnimationId.Walk or PlayerAnimationId.Run)
            {
                return PlayerAnimationMovementPolicy.ScriptedMovement;
            }

            return id is PlayerAnimationId.Evade_Front or PlayerAnimationId.Evade_Back
                ? PlayerAnimationMovementPolicy.RootMotion
                : PlayerAnimationMovementPolicy.KeepInPlace;
        }
    }
}
