using UnityEngine;

namespace Train.WorldInteraction.Data
{
    [CreateAssetMenu(
        menuName = "Train/World Interaction/World Interaction Settings",
        fileName = "WorldInteractionSettings")]
    public sealed class WorldInteractionSettings : ScriptableObject
    {
        [SerializeField, Min(.5f)] private float _npcInteractionRange = 1.6f;
        [SerializeField, Min(.5f)] private float _portalInteractionRange = 2.2f;

        public float NpcInteractionRange => Mathf.Max(.5f, _npcInteractionRange);
        public float PortalInteractionRange => Mathf.Max(.5f, _portalInteractionRange);
    }
}
