#if UNITY_EDITOR
using Train.Gameplay.Combat;
using Train.Gameplay.Combat.Buffs;
using Train.Gameplay.Combat.HitEffects;
using Train.Gameplay.Combat.Presentation;
using Train.Gameplay.Player.Application;
using UnityEditor;
using UnityEngine;

namespace Train.EditorTools.Combat
{
    /// <summary>
    /// 在玩家预制体上补齐事件转发、独立 BuffHandle 与雷剑命中特效。
    /// 此安装器可重复执行，不会创建重复组件。
    /// </summary>
    [InitializeOnLoad]
    internal static class GameplayArchitecturePrefabInstaller
    {
        private const string PlayerPrefabPath =
            "Assets/Prefabs/Player/Player_Ellen.prefab";

        static GameplayArchitecturePrefabInstaller()
        {
            EditorApplication.delayCall += EnsurePlayerArchitecture;
        }

        [MenuItem("Tools/Train/Architecture/Install Gameplay Event Relays")]
        private static void EnsurePlayerArchitecture()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (prefab == null || prefab.GetComponent<Health>() == null)
            {
                return;
            }

            var contents = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                var changed = false;
                if (contents.GetComponent<HealthEventRelay>() == null)
                {
                    contents.AddComponent<HealthEventRelay>();
                    changed = true;
                }

                if (contents.GetComponent<BuffHandleComponent>() == null)
                {
                    contents.AddComponent<BuffHandleComponent>();
                    changed = true;
                }

                if (contents.GetComponent<CombatStatModifierComponent>() == null)
                {
                    contents.AddComponent<CombatStatModifierComponent>();
                    changed = true;
                }

                if (contents.GetComponent<PlayerEquipmentStatBinder>() == null)
                {
                    contents.AddComponent<PlayerEquipmentStatBinder>();
                    changed = true;
                }

                var swordHitbox =
                    contents.GetComponentInChildren<SwordHitbox>(true);
                if (swordHitbox != null)
                {
                    swordHitbox.ConfigureDamageType(DamageType.Electric);
                    if (swordHitbox.GetComponent<LightningWeaponHitEffect>() == null)
                    {
                        swordHitbox.gameObject
                            .AddComponent<LightningWeaponHitEffect>();
                        changed = true;
                    }
                }

                if (!changed)
                {
                    return;
                }

                PrefabUtility.SaveAsPrefabAsset(contents, PlayerPrefabPath);
                Debug.Log(
                    "已在 Player_Ellen 上安装事件转发、BuffHandle、装备属性与雷剑命中特效。");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }
    }
}
#endif
