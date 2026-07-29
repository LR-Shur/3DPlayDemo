using System.Collections.Generic;
using Animancer;
using UnityEngine;

namespace Train.Gameplay.Player.Animation.Data
{
    /// <summary>
    /// 集中保存角色全部动画定义，并按动画标识提供快速查询。
    /// 新增动画时只需补充标识并在本资源中增加一项，不需要修改播放类。
    /// </summary>
    [CreateAssetMenu(menuName = "Train/Player/Animation Catalog", fileName = "EllenAnimationCatalog")]
    public sealed class PlayerAnimationCatalog : ScriptableObject
    {
        [SerializeField] private PlayerAnimationDefinition[] _definitions;
        [SerializeField] private PlayerAnimationTransitionRule[] _contextualTransitions;

        private Dictionary<PlayerAnimationId, PlayerAnimationDefinition> _lookup;
        private Dictionary<long, TransitionAssetBase> _contextualTransitionLookup;

        /// <summary>
        /// 在资源载入时建立运行时查询表。
        /// </summary>
        private void OnEnable()
        {
            RebuildLookup();
        }

        /// <summary>
        /// 尝试获取指定标识对应的动画定义。
        /// </summary>
        /// <param name="id">需要查找的动画标识。</param>
        /// <param name="definition">查询成功时返回对应的动画定义。</param>
        /// <returns>目录中存在且已绑定动画剪辑时返回 true。</returns>
        public bool TryGet(PlayerAnimationId id, out PlayerAnimationDefinition definition)
        {
            if (_lookup == null)
            {
                RebuildLookup();
            }

            return _lookup.TryGetValue(id, out definition) &&
                   definition.Transition != null &&
                   definition.Transition.IsValid;
        }

        /// <summary>
        /// 读取目录中已经保存的玩法元数据，不要求旧条目已经完成 TransitionAsset 迁移。
        /// 此方法仅供编辑器生成器保留手工调节过的位移与取消窗口数据。
        /// </summary>
        /// <param name="id">需要查找的动画标识。</param>
        /// <param name="definition">查询成功时返回原有动画定义。</param>
        /// <returns>序列化目录中存在该标识时返回 true。</returns>
        public bool TryGetStoredDefinition(
            PlayerAnimationId id,
            out PlayerAnimationDefinition definition)
        {
            if (_lookup == null)
            {
                RebuildLookup();
            }

            return _lookup.TryGetValue(id, out definition);
        }

        /// <summary>
        /// 尝试获取只在指定来源与目标组合中使用的专属 TransitionAsset。
        /// 没有配置规则时调用方应继续使用目标动画定义自己的默认 TransitionAsset。
        /// </summary>
        /// <param name="from">当前正在播放的来源动画标识。</param>
        /// <param name="to">准备播放的目标动画标识。</param>
        /// <param name="transition">查询成功时返回专属过渡资源。</param>
        /// <returns>存在有效的来源相关过渡规则时返回 true。</returns>
        public bool TryGetContextualTransition(
            PlayerAnimationId from,
            PlayerAnimationId to,
            out TransitionAssetBase transition)
        {
            if (_contextualTransitionLookup == null)
            {
                RebuildLookup();
            }

            return _contextualTransitionLookup.TryGetValue(
                       CreateTransitionKey(from, to),
                       out transition) &&
                   transition != null &&
                   transition.IsValid;
        }

        /// <summary>
        /// 使用新的定义集合替换目录内容，并同步刷新查询表。
        /// 此方法由 Ellen 动画目录生成工具调用。
        /// </summary>
        /// <param name="definitions">要写入目录的完整动画定义集合。</param>
        public void ReplaceDefinitions(PlayerAnimationDefinition[] definitions)
        {
            _definitions = definitions;
            RebuildLookup();
        }

        /// <summary>
        /// 使用新的来源相关过渡集合替换目录内容，并同步刷新查询表。
        /// </summary>
        /// <param name="transitions">需要写入目录的完整来源相关过渡集合。</param>
        public void ReplaceContextualTransitions(
            PlayerAnimationTransitionRule[] transitions)
        {
            _contextualTransitions = transitions;
            RebuildLookup();
        }

        /// <summary>
        /// 根据序列化数组重建动画标识到定义的查询表。
        /// </summary>
        private void RebuildLookup()
        {
            _lookup = new Dictionary<PlayerAnimationId, PlayerAnimationDefinition>();
            _contextualTransitionLookup =
                new Dictionary<long, TransitionAssetBase>();

            if (_definitions != null)
            {
                foreach (var definition in _definitions)
                {
                    if (definition != null)
                    {
                        _lookup[definition.Id] = definition;
                    }
                }
            }

            if (_contextualTransitions == null)
            {
                return;
            }

            foreach (var transitionRule in _contextualTransitions)
            {
                if (transitionRule != null &&
                    transitionRule.Transition != null)
                {
                    _contextualTransitionLookup[
                        CreateTransitionKey(
                            transitionRule.From,
                            transitionRule.To)] = transitionRule.Transition;
                }
            }
        }

        /// <summary>
        /// 将两个动画标识组合成不会互相冲突的运行时查询键。
        /// </summary>
        /// <param name="from">来源动画标识。</param>
        /// <param name="to">目标动画标识。</param>
        /// <returns>可用于字典查询的六十四位组合键。</returns>
        private static long CreateTransitionKey(
            PlayerAnimationId from,
            PlayerAnimationId to)
        {
            return ((long)(int)from << 32) | (uint)(int)to;
        }
    }
}
