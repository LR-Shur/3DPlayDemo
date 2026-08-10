#if UNITY_EDITOR
using System;
using System.Linq;
using Train.Gameplay.Combat;
using Train.Gameplay.Combat.Buffs;
using Train.Gameplay.Combat.HitEffects;
using Train.Gameplay.Player.Core;
using UnityEditor;
using UnityEngine;

namespace Train.EditorTools
{
    /// <summary>
    /// 生成并维护玩家剑 Hitbox 与 Rusk 训练假人预制体。
    /// </summary>
    [InitializeOnLoad]
    public static class CombatSetupTool
    {
        private const string PlayerPrefabPath = "Assets/Prefabs/Player/Player_Ellen.prefab";
        private const string RuskModelPath =
            "Assets/Arts/CharacterModels/AnimancerController/Arts/Rusk/Rusk_ver1.1.fbx";
        private const string DummyPrefabPath =
            "Assets/Prefabs/Combat/TrainingDummy_Rusk.prefab";
        private const string AutoSetupKey = "Train.CombatSetup.v1";

        static CombatSetupTool()
        {
            EditorApplication.delayCall += TryRunAutomaticSetup;
        }

        [MenuItem("Tools/Train/Combat/Create Or Refresh Combat Assets")]
        public static void CreateOrRefreshCombatAssets()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("请先退出播放模式再生成战斗资源。");
                return;
            }

            AssetDatabase.StartAssetEditing();
            try
            {
                SetupPlayerSwordHitbox();
                CreateTrainingDummyPrefab();
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            SessionState.SetBool(AutoSetupKey, true);
            Debug.Log(
                $"战斗系统资源已生成：玩家剑 Hitbox 已配置，训练假人位于 {DummyPrefabPath}");
        }

        private static void TryRunAutomaticSetup()
        {
            if (SessionState.GetBool(AutoSetupKey, false) ||
                EditorApplication.isCompiling ||
                EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<MonoScript>(
                    "Assets/Scripts/Gameplay/Combat/SwordHitbox.cs") == null)
            {
                EditorApplication.delayCall += TryRunAutomaticSetup;
                return;
            }

            try
            {
                CreateOrRefreshCombatAssets();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private static void SetupPlayerSwordHitbox()
        {
            var root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            if (root == null)
            {
                throw new InvalidOperationException($"找不到玩家预制体：{PlayerPrefabPath}");
            }

            try
            {
                var swordTransform = FindSwordTransform(root.transform);
                if (swordTransform == null)
                {
                    throw new InvalidOperationException(
                        "在 Player_Ellen 中找不到名称包含 Sword、Blade 或 Weapon 的剑节点。");
                }

                var hitboxParent = FindSwordAttachment(swordTransform);
                var existingHitbox = root.GetComponentInChildren<SwordHitbox>(true);
                var hitboxObject = existingHitbox != null
                    ? existingHitbox.gameObject
                    : new GameObject("SwordHitbox");

                hitboxObject.transform.SetParent(hitboxParent, false);

                var boxCollider = hitboxObject.GetComponent<BoxCollider>();
                if (boxCollider == null)
                {
                    boxCollider = hitboxObject.AddComponent<BoxCollider>();
                }

                boxCollider.isTrigger = true;
                FitSwordCollider(
                    hitboxParent,
                    swordTransform.GetComponentsInChildren<Renderer>(true),
                    boxCollider);
                boxCollider.enabled = false;

                var swordHitbox = hitboxObject.GetComponent<SwordHitbox>();
                if (swordHitbox == null)
                {
                    swordHitbox = hitboxObject.AddComponent<SwordHitbox>();
                }

                swordHitbox.Configure(root.transform, 25f, 2f);
                swordHitbox.ConfigureDamageType(DamageType.Electric);
                if (hitboxObject.GetComponent<LightningWeaponHitEffect>() == null)
                {
                    hitboxObject.AddComponent<LightningWeaponHitEffect>();
                }

                if (root.GetComponent<BuffHandleComponent>() == null)
                {
                    root.AddComponent<BuffHandleComponent>();
                }

                var combat = root.GetComponent<PlayerCombat>();
                if (combat == null)
                {
                    combat = root.AddComponent<PlayerCombat>();
                }

                SetObjectReference(combat, "_swordHitbox", swordHitbox);

                var controller = root.GetComponent<PlayerController>();
                if (controller != null)
                {
                    SetObjectReference(controller, "_combat", combat);
                }

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
                Debug.Log(
                    $"已将 SwordHitbox 挂到玩家节点 {GetHierarchyPath(hitboxParent)}，" +
                    $"尺寸 {boxCollider.size}。");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void CreateTrainingDummyPrefab()
        {
            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Prefabs/Combat");

            var ruskModel = AssetDatabase.LoadAssetAtPath<GameObject>(RuskModelPath);
            if (ruskModel == null)
            {
                throw new InvalidOperationException($"找不到 Rusk 模型：{RuskModelPath}");
            }

            var root = new GameObject("TrainingDummy_Rusk");
            try
            {
                root.AddComponent<BuffHandleComponent>();
                var visual = PrefabUtility.InstantiatePrefab(ruskModel) as GameObject;
                if (visual == null)
                {
                    throw new InvalidOperationException("无法实例化 Rusk 模型。");
                }

                visual.name = "Rusk_Visual";
                visual.transform.SetParent(root.transform, false);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;

                var bounds = CalculateLocalRendererBounds(root.transform);
                var capsule = root.AddComponent<CapsuleCollider>();
                capsule.direction = 1;
                capsule.center = bounds.center;
                capsule.height = Mathf.Max(0.2f, bounds.size.y);
                capsule.radius = Mathf.Max(
                    0.1f,
                    Mathf.Min(bounds.size.x, bounds.size.z) * 0.45f);
                capsule.height = Mathf.Max(capsule.height, capsule.radius * 2f);

                var rigidbody = root.AddComponent<Rigidbody>();
                rigidbody.isKinematic = true;
                rigidbody.useGravity = false;
                rigidbody.interpolation = RigidbodyInterpolation.Interpolate;

                var health = root.AddComponent<Health>();
                health.SetMaxHealth(100f);
                root.AddComponent<TrainingDummy>();

                PrefabUtility.SaveAsPrefabAsset(root, DummyPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static Transform FindSwordTransform(Transform root)
        {
            return root
                .GetComponentsInChildren<Transform>(true)
                .Select(transform => new
                {
                    Transform = transform,
                    Score = ScoreSwordCandidate(transform),
                })
                .Where(candidate => candidate.Score > 0)
                .OrderByDescending(candidate => candidate.Score)
                .ThenBy(candidate => candidate.Transform.GetSiblingIndex())
                .Select(candidate => candidate.Transform)
                .FirstOrDefault();
        }

        private static int ScoreSwordCandidate(Transform transform)
        {
            var name = transform.name.ToLowerInvariant();
            if (name == "swordhitbox" || transform.GetComponent<SwordHitbox>() != null)
            {
                return 0;
            }

            var score = 0;
            if (name.Contains("sword")) score += 100;
            if (name.Contains("blade")) score += 90;
            if (name.Contains("weapon")) score += 70;
            if (name.Contains("wep")) score += 40;
            if (transform.GetComponent<Renderer>() != null) score += 20;
            if (transform.GetComponentsInChildren<Renderer>(true).Length > 0) score += 10;
            if (name.Contains("hand")) score -= 20;
            return score;
        }

        private static Transform FindSwordAttachment(Transform swordTransform)
        {
            var skinnedRenderer = swordTransform.GetComponent<SkinnedMeshRenderer>();
            if (skinnedRenderer == null ||
                skinnedRenderer.bones == null ||
                skinnedRenderer.bones.Length == 0)
            {
                return swordTransform;
            }

            var boneWeights = new float[skinnedRenderer.bones.Length];
            var mesh = skinnedRenderer.sharedMesh;
            if (mesh != null)
            {
                foreach (var weight in mesh.boneWeights)
                {
                    AddBoneWeight(boneWeights, weight.boneIndex0, weight.weight0);
                    AddBoneWeight(boneWeights, weight.boneIndex1, weight.weight1);
                    AddBoneWeight(boneWeights, weight.boneIndex2, weight.weight2);
                    AddBoneWeight(boneWeights, weight.boneIndex3, weight.weight3);
                }
            }

            var attachment = skinnedRenderer.bones
                .Select((bone, index) => new
                {
                    Bone = bone,
                    Weight = boneWeights[index],
                    NameScore = bone != null ? ScoreAttachmentBone(bone.name) : 0,
                })
                .Where(candidate =>
                    candidate.Bone != null &&
                    candidate.Bone != swordTransform &&
                    candidate.Bone.name != "SwordHitbox" &&
                    candidate.Bone.GetComponent<SwordHitbox>() == null)
                .OrderByDescending(candidate => candidate.Weight)
                .ThenByDescending(candidate => candidate.NameScore)
                .Select(candidate => candidate.Bone)
                .FirstOrDefault();

            if (attachment == null && skinnedRenderer.rootBone != null)
            {
                attachment = skinnedRenderer.rootBone
                    .GetComponentsInChildren<Transform>(true)
                    .OrderByDescending(bone => ScoreAttachmentBone(bone.name))
                    .FirstOrDefault(bone => ScoreAttachmentBone(bone.name) > 0);
            }

            return attachment != null ? attachment : swordTransform;
        }

        private static void AddBoneWeight(float[] totals, int index, float weight)
        {
            if (index >= 0 && index < totals.Length)
            {
                totals[index] += weight;
            }
        }

        private static int ScoreAttachmentBone(string boneName)
        {
            var name = boneName.ToLowerInvariant();
            var score = 0;
            if (name.Contains("prop")) score += 100;
            if (name.Contains("weapon")) score += 90;
            if (name.Contains("sword")) score += 80;
            if (name.Contains("hand")) score += 20;
            return score;
        }

        private static void FitSwordCollider(
            Transform colliderSpace,
            Renderer[] renderers,
            BoxCollider collider)
        {
            if (renderers.Length == 0)
            {
                collider.center = new Vector3(0f, 0f, 0.45f);
                collider.size = new Vector3(0.14f, 0.14f, 0.9f);
                return;
            }

            var bounds = CalculateBoundsInSpace(colliderSpace, renderers);
            var size = bounds.size;
            if (size.magnitude < 0.05f)
            {
                collider.center = new Vector3(0f, 0f, 0.45f);
                collider.size = new Vector3(0.14f, 0.14f, 0.9f);
                return;
            }

            collider.center = bounds.center;
            collider.size = new Vector3(
                Mathf.Max(0.08f, size.x),
                Mathf.Max(0.08f, size.y),
                Mathf.Max(0.08f, size.z));
        }

        private static Bounds CalculateLocalRendererBounds(Transform root)
        {
            return CalculateBoundsInSpace(
                root,
                root.GetComponentsInChildren<Renderer>(true));
        }

        private static Bounds CalculateBoundsInSpace(Transform space, Renderer[] renderers)
        {
            if (renderers == null || renderers.Length == 0)
            {
                return new Bounds(new Vector3(0f, 1f, 0f), new Vector3(1f, 2f, 1f));
            }

            var first = true;
            var localBounds = new Bounds();
            foreach (var renderer in renderers)
            {
                var bounds = renderer.bounds;
                var min = bounds.min;
                var max = bounds.max;
                for (var x = 0; x < 2; x++)
                {
                    for (var y = 0; y < 2; y++)
                    {
                        for (var z = 0; z < 2; z++)
                        {
                            var worldPoint = new Vector3(
                                x == 0 ? min.x : max.x,
                                y == 0 ? min.y : max.y,
                                z == 0 ? min.z : max.z);
                            var localPoint = space.InverseTransformPoint(worldPoint);
                            if (first)
                            {
                                localBounds = new Bounds(localPoint, Vector3.zero);
                                first = false;
                            }
                            else
                            {
                                localBounds.Encapsulate(localPoint);
                            }
                        }
                    }
                }
            }

            return localBounds;
        }

        private static void SetObjectReference(
            UnityEngine.Object target,
            string propertyName,
            UnityEngine.Object value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException(
                    $"{target.GetType().Name} 中不存在序列化字段 {propertyName}。");
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = path.Substring(0, path.LastIndexOf('/'));
            var name = path.Substring(path.LastIndexOf('/') + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static string GetHierarchyPath(Transform transform)
        {
            var path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = $"{transform.name}/{path}";
            }

            return path;
        }
    }
}
#endif
