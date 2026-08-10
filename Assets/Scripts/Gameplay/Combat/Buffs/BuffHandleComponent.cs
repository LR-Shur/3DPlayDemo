using System;
using Train.Buffs.Core;
using UnityEngine;

namespace Train.Gameplay.Combat.Buffs
{
    /// <summary>
    /// 单个战斗实体的 Unity Buff 入口。
    /// 每个玩家或敌人只挂一个组件，组件内部持有独立的纯 C# BuffHandle。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BuffHandleComponent : MonoBehaviour
    {
        private BuffHandle _handle;

        /// <summary>
        /// Buff 发生施加、叠层、刷新、移除或过期时触发。
        /// UI 和特效可订阅此事件，但不应直接修改 Buff 集合。
        /// </summary>
        public event EventHandler<BuffChangedEventArgs> Changed;

        /// <summary>
        /// 当前实体 Buff 的只读快照。
        /// </summary>
        public BuffHandleSnapshot Snapshot => EnsureHandle().Snapshot;

        /// <summary>
        /// 创建包含项目内置 Buff 工厂的实体句柄。
        /// </summary>
        private void Awake()
        {
            EnsureHandle();
        }

        /// <summary>
        /// 使用 Unity 帧时间推进 Buff，并自动清理过期效果。
        /// </summary>
        private void Update()
        {
            _handle?.Tick(Time.deltaTime);
        }

        /// <summary>
        /// 将外部传入的 BuffInfo 交给本实体的句柄处理。
        /// </summary>
        /// <param name="info">一次不可变的 Buff 施加信息。</param>
        /// <returns>施加或合并后的 Buff 快照。</returns>
        public BuffSnapshot Apply(BuffInfo info)
        {
            return EnsureHandle().Apply(info);
        }

        /// <summary>
        /// 让本实体的全部 Buff 修正一次入伤数值。
        /// </summary>
        /// <param name="amount">修正前伤害。</param>
        /// <param name="element">伤害元素。</param>
        /// <returns>依稳定顺序应用 Buff 后的伤害。</returns>
        public float ModifyIncomingDamage(
            float amount,
            DamageElement element)
        {
            return EnsureHandle()
                .ModifyIncomingDamage(new DamageContext(amount, element))
                .Amount;
        }

        /// <summary>
        /// 创建并缓存句柄，同时转发领域事件。
        /// </summary>
        private BuffHandle EnsureHandle()
        {
            if (_handle != null)
            {
                return _handle;
            }

            _handle = new BuffHandle(
                BuffFactoryRegistry.CreateWithBuiltIns());
            _handle.Changed += OnHandleChanged;
            return _handle;
        }

        /// <summary>
        /// 将领域事件转发给 Unity 表现组件。
        /// </summary>
        private void OnHandleChanged(
            object sender,
            BuffChangedEventArgs eventArgs)
        {
            Changed?.Invoke(this, eventArgs);
        }

        /// <summary>
        /// 组件销毁时解除事件订阅，避免句柄继续持有表现对象。
        /// </summary>
        private void OnDestroy()
        {
            if (_handle != null)
            {
                _handle.Changed -= OnHandleChanged;
            }
        }
    }
}
