using UnityEngine;

namespace Train.Gameplay.Player.Data
{
    /// <summary>
    /// 保存玩家移动状态使用的可编辑数值。
    /// 将数值放在资源中，便于在不修改状态代码的情况下进行调参。
    /// </summary>
    [CreateAssetMenu(menuName = "Train/Player/Player Config", fileName = "PlayerConfig")]
    public sealed class PlayerConfig : ScriptableObject
    {
        [SerializeField, Min(0f)] private float _walkSpeed = 2.5f;
        [SerializeField, Min(0f)] private float _runSpeed = 5f;
        [SerializeField, Min(0f)] private float _jumpHeight = 1.2f;
        [SerializeField, Range(0f, 1f)] private float _inputDeadZone = 0.1f;

        /// <summary>
        /// 获取步行状态使用的移动速度。
        /// </summary>
        public float WalkSpeed => _walkSpeed;

        /// <summary>
        /// 获取奔跑状态使用的移动速度。
        /// </summary>
        public float RunSpeed => _runSpeed;

        /// <summary>
        /// 获取角色从地面起跳时达到的最高高度。
        /// </summary>
        public float JumpHeight => _jumpHeight;

        /// <summary>
        /// 获取用于判断是否存在移动输入的死区阈值。
        /// </summary>
        public float InputDeadZone => _inputDeadZone;
    }
}
