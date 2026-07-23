using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Train.EditorTools
{
    /// <summary>
    /// 用于诊断 Ellen 的动画骨骼绑定，并在确认需要时将整套角色动画切换为 Generic 骨骼动画。
    /// Generic 会保留尾巴、裙摆等不属于 Humanoid 标准骨骼映射的额外骨骼。
    /// </summary>
    public static class EllenAnimationRigTools
    {
        private const string AnimationFolder = "Assets/Arts/Player/Ellen/AnimationFBX";
        private const string ModelPath = "Assets/Arts/Player/Ellen/Model/TPOSS.fbx";
        private const string WalkClipPath = AnimationFolder + "/Locomotion/Avatar_Female_Size02_Ellen_Ani_Walk.fbx";

        /// <summary>
        /// 输出 Walk 动画曲线中是否包含尾巴骨骼，以及当前 TPOSS Animator 是否仍使用 Humanoid Avatar。
        /// </summary>
        [MenuItem("Train/Animation/诊断 Ellen 尾巴绑定")]
        public static void DiagnoseTailBinding()
        {
            var walkClip = AssetDatabase.LoadAllAssetsAtPath(WalkClipPath).OfType<AnimationClip>().FirstOrDefault();
            var tailBindings = walkClip == null
                ? System.Array.Empty<EditorCurveBinding>()
                : AnimationUtility.GetCurveBindings(walkClip)
                    .Where(binding => binding.path.IndexOf("tail", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToArray();
            var animator = Object.FindFirstObjectByType<Animator>();

            Debug.Log($"Ellen Walk 尾巴曲线数量：{tailBindings.Length}；" +
                      $"当前 Animator 是否 Humanoid：{(animator != null && animator.isHuman)}。\n" +
                      "若尾巴曲线大于 0 且 Animator 为 Humanoid，应执行“修复 Ellen 尾巴骨骼动画”。");
        }

        /// <summary>
        /// 将 Ellen 模型与全部动画 FBX 统一切换为 Generic，保留尾巴和裙摆等额外骨骼的动画曲线。
        /// </summary>
        [MenuItem("Train/Animation/修复 Ellen 尾巴骨骼动画")]
        public static void FixTailAnimation()
        {
            var paths = AssetDatabase.FindAssets("t:Model", new[] { AnimationFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Append(ModelPath)
                .Distinct()
                .ToArray();
            var changedCount = 0;

            foreach (var path in paths)
            {
                if (AssetImporter.GetAtPath(path) is not ModelImporter importer)
                {
                    continue;
                }

                if (importer.animationType == ModelImporterAnimationType.Generic &&
                    importer.avatarSetup == ModelImporterAvatarSetup.NoAvatar)
                {
                    continue;
                }

                importer.animationType = ModelImporterAnimationType.Generic;
                importer.avatarSetup = ModelImporterAvatarSetup.NoAvatar;
                importer.SaveAndReimport();
                changedCount++;
            }

            foreach (var animator in Object.FindObjectsByType<Animator>(FindObjectsSortMode.None))
            {
                if (animator.avatar == null || AssetDatabase.GetAssetPath(animator.avatar) != ModelPath)
                {
                    continue;
                }

                Undo.RecordObject(animator, "清除 Ellen Humanoid Avatar");
                animator.avatar = null;
                EditorUtility.SetDirty(animator);
            }

            EditorSceneManager.MarkAllScenesDirty();
            EditorSceneManager.SaveOpenScenes();
            Debug.Log($"Ellen 已切换为 Generic 骨骼动画，重新导入 {changedCount} 个 FBX。" +
                      "现在 Animancer 会直接驱动尾巴、裙摆等额外骨骼。 ");
        }
    }
}
