using UnityEngine;

namespace Train.Gameplay.Combat.Factions
{
    /// <summary>
    /// 从碰撞体所在节点向角色根节点查找阵营。
    /// 每一层优先采用显式的 FactionMember，再采用其他 IFactionMember 实现。
    /// </summary>
    public static class FactionResolver
    {
        public static IFactionMember FindInParents(Transform start)
        {
            for (var current = start; current != null; current = current.parent)
            {
                if (current.TryGetComponent<FactionMember>(out var explicitMember))
                {
                    return explicitMember;
                }

                var behaviours = current.GetComponents<MonoBehaviour>();
                foreach (var behaviour in behaviours)
                {
                    if (behaviour is IFactionMember factionMember)
                    {
                        return factionMember;
                    }
                }
            }

            return null;
        }
    }
}
