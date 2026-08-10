using UnityEngine;

namespace Train.Gameplay.Player.Application.Events
{
    /// <summary>
    /// 表示玩家复活流程已经完成。
    /// </summary>
    public readonly struct PlayerRespawnCompletedEvent
    {
        public PlayerRespawnCompletedEvent(GameObject player)
        {
            Player = player;
        }

        public GameObject Player { get; }
    }
}
