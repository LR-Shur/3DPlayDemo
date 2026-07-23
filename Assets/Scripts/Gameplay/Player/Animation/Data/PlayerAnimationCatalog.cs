using System.Collections.Generic;
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

        private Dictionary<PlayerAnimationId, PlayerAnimationDefinition> _lookup;

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

            return _lookup.TryGetValue(id, out definition) && definition.Clip != null;
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
        /// 根据序列化数组重建动画标识到定义的查询表。
        /// </summary>
        private void RebuildLookup()
        {
            _lookup = new Dictionary<PlayerAnimationId, PlayerAnimationDefinition>();

            if (_definitions == null)
            {
                return;
            }

            foreach (var definition in _definitions)
            {
                if (definition != null)
                {
                    _lookup[definition.Id] = definition;
                }
            }
        }
    }
}
