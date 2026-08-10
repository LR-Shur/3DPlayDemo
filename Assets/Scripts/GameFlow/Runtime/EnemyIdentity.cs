using UnityEngine;

namespace Train.GameFlow.Runtime
{
    /// <summary>
    /// 保存一个敌人实例的唯一标识和敌人原型标识，供关卡事件与任务统计使用。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyIdentity : MonoBehaviour
    {
        [SerializeField] private string _instanceId = "enemy_01";
        [SerializeField] private string _archetypeId = "kaykit_knight";

        /// <summary>获取当前场景中此敌人实例的唯一标识。</summary>
        public string InstanceId => _instanceId;

        /// <summary>获取此敌人所属原型的稳定标识。</summary>
        public string ArchetypeId => _archetypeId;

        /// <summary>
        /// 配置敌人的实例标识和原型标识，并为无效输入提供安全回退值。
        /// </summary>
        public void Configure(string instanceId, string archetypeId)
        {
            _instanceId = string.IsNullOrWhiteSpace(instanceId)
                ? gameObject.name
                : instanceId;
            _archetypeId = string.IsNullOrWhiteSpace(archetypeId)
                ? "unknown"
                : archetypeId;
        }
    }
}
