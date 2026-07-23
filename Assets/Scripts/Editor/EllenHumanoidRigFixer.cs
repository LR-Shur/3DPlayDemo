using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Train.EditorTools
{
    /// <summary>
    /// 批量将 Ellen 的动作 FBX 转为 Humanoid，并统一复制 TPOSS 的 Avatar。
    /// 可通过 Unity 菜单手动重新执行，用于后续新增动作资源的导入配置。
    /// </summary>
    public static class EllenHumanoidRigFixer
    {
        private const string SourceRoot = "Assets/Arts/Player/Ellen/AnimationFBX";
        private const string AvatarSourcePath = "Assets/Arts/Player/Ellen/Model/TPOSS.fbx";
        /// <summary>
        /// 从 Unity 菜单手动重新执行全部 Ellen 动作 FBX 的 Humanoid 配置。
        /// </summary>
        [MenuItem("Train/Player/修复 Ellen 动作 Humanoid Avatar")]
        public static void ConfigureAllAnimationFbxAsHumanoid()
        {
            var sourceAvatar = AssetDatabase.LoadAllAssetsAtPath(AvatarSourcePath)
                .OfType<Avatar>()
                .FirstOrDefault();
            if (sourceAvatar == null)
            {
                Debug.LogError($"未找到 Ellen 的 Avatar 来源：{AvatarSourcePath}");
                return;
            }

            var paths = AssetDatabase.FindAssets("t:Model", new[] { SourceRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
                .OrderBy(path => path)
                .ToArray();

            var updatedCount = 0;
            foreach (var path in paths)
            {
                if (AssetImporter.GetAtPath(path) is not ModelImporter importer)
                {
                    continue;
                }

                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                importer.sourceAvatar = sourceAvatar;
                importer.SaveAndReimport();
                updatedCount++;
            }

            EllenAnimationCatalogGenerator.GenerateCatalog();

            Debug.Log($"Ellen 动作 Humanoid 修复完成，共重新导入 {updatedCount} 个 FBX。", sourceAvatar);
        }
    }
}
