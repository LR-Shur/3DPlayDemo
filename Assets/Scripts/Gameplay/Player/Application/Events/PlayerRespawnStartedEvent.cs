using UnityEngine;

namespace Train.Gameplay.Player.Application.Events
{
    /// <summary>
    /// 表示玩家已经死亡并开始延迟复活流程。
    /// </summary>
    public readonly struct PlayerRespawnStartedEvent
    {
        public PlayerRespawnStartedEvent(GameObject player, float delaySeconds)
        {
            Player = player;
            DelaySeconds = delaySeconds;
        }

        public GameObject Player { get; }
        public float DelaySeconds { get; }
    }
}
