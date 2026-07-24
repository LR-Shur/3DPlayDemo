using System;
using System.Collections.Generic;
using Train.Gameplay.Player.Animation.Data;
using UnityEditor;
using UnityEngine;

namespace Train.EditorTools
{
    /// <summary>
    /// 校验 Ellen 动画目录是否完整覆盖全部动画标识，并输出各状态类别的数量。
    /// 该工具用于在重新生成目录后快速发现漏收录、空剪辑或错误映射。
    /// </summary>
    public static class EllenAnimationStateMappingValidator
    {
        private const string CatalogPath = "Assets/Resources/Player/EllenAnimationCatalog.asset";

        /// <summary>
        /// 验证全部动画标识都存在有效目录条目，并在控制台输出分类统计。
        /// </summary>
        [MenuItem("Train/Player/验证 Ellen 动画状态映射")]
        public static void Validate()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<PlayerAnimationCatalog>(CatalogPath);
            if (catalog == null)
            {
                Debug.LogError($"找不到 Ellen 动画目录：{CatalogPath}");
                return;
            }

            var missingAnimationIds = new List<PlayerAnimationId>();
            var stateKindCounts = new Dictionary<PlayerAnimationStateKind, int>();

            foreach (PlayerAnimationId animationId in Enum.GetValues(typeof(PlayerAnimationId)))
            {
                if (!catalog.TryGet(animationId, out var definition))
                {
                    missingAnimationIds.Add(animationId);
                    continue;
                }

                stateKindCounts.TryGetValue(definition.StateKind, out var count);
                stateKindCounts[definition.StateKind] = count + 1;
            }

            if (missingAnimationIds.Count > 0)
            {
                Debug.LogError($"Ellen 动画目录校验失败，缺少或未绑定剪辑的动画：{string.Join("、", missingAnimationIds)}");
                return;
            }

            var summary = new List<string>();
            foreach (PlayerAnimationStateKind stateKind in Enum.GetValues(typeof(PlayerAnimationStateKind)))
            {
                stateKindCounts.TryGetValue(stateKind, out var count);
                summary.Add($"{stateKind}={count}");
            }

            Debug.Log($"Ellen 动画状态映射校验通过：共 {Enum.GetValues(typeof(PlayerAnimationId)).Length} 条动画。{string.Join("，", summary)}");
        }
    }
}
