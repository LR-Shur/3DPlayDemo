#if UNITY_EDITOR
using System;
using System.Linq;
using Train.Characters.Data;
using UnityEditor;
using UnityEngine;

namespace Train.EditorTools.Characters
{
    /// <summary>
    /// 以固定路径和固定标识幂等构建 Ellen、Belle 与 Billy 的角色配置资产。
    /// 该工具只建立数据引用，不会向任何战斗场景添加角色实例。
    /// </summary>
    public static class CharacterContentBuilder
    {
        private const string RootFolder = "Assets/Data/Characters";
        private const string DefinitionFolder =
            RootFolder + "/Definitions";
        private const string SettingsPath =
            RootFolder + "/DefaultCharacterSettings.asset";
        private const string EllenId = "character.ellen";
        private const string BelleId = "character.belle";
        private const string BillyId = "character.billy";

        /// <summary>
        /// 创建或更新三名角色定义和统一角色设置，可安全重复执行。
        /// </summary>
        [MenuItem("Tools/Train/Content/Build Character Content")]
        public static void Build()
        {
            EnsureFolder(RootFolder);
            EnsureFolder(DefinitionFolder);

            var seeds = CreateSeeds();
            var definitions =
                new CharacterDefinition[seeds.Length];
            for (var i = 0; i < seeds.Length; i++)
            {
                definitions[i] = CreateOrUpdateDefinition(seeds[i]);
            }

            var settings =
                AssetDatabase.LoadAssetAtPath<CharacterSettings>(
                    SettingsPath);
            if (settings == null)
            {
                settings =
                    ScriptableObject.CreateInstance<CharacterSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }

            var serializedSettings = new SerializedObject(settings);
            var characters =
                serializedSettings.FindProperty("_characters");
            characters.arraySize = definitions.Length;
            for (var i = 0; i < definitions.Length; i++)
            {
                characters.GetArrayElementAtIndex(i)
                    .objectReferenceValue = definitions[i];
            }

            serializedSettings
                .FindProperty("_defaultSelectedCharacterId")
                .stringValue = EllenId;
            serializedSettings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = settings;
            Debug.Log(
                "角色内容构建完成：Ellen 默认解锁，Belle 与 Billy 已登记但保持锁定；" +
                "模型和待机动画只作为数据引用，未加入场景。");
        }

        private static CharacterDefinition CreateOrUpdateDefinition(
            CharacterSeed seed)
        {
            var path =
                $"{DefinitionFolder}/{SanitizeAssetName(seed.CharacterId)}.asset";
            var definition =
                AssetDatabase.LoadAssetAtPath<CharacterDefinition>(path);
            if (definition == null)
            {
                definition =
                    ScriptableObject.CreateInstance<CharacterDefinition>();
                AssetDatabase.CreateAsset(definition, path);
            }

            var model = LoadRequiredAsset<GameObject>(seed.ModelPath);
            var idleAnimation = LoadAnimationClip(seed.IdleAnimationPath);
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("_characterId").stringValue =
                seed.CharacterId;
            serialized.FindProperty("_displayName").stringValue =
                seed.DisplayName;
            serialized.FindProperty("_description").stringValue =
                seed.Description;
            serialized.FindProperty("_faction").stringValue =
                seed.Faction;
            serialized.FindProperty("_combatRole").stringValue =
                seed.CombatRole;
            serialized.FindProperty("_unlockedByDefault").boolValue =
                seed.UnlockedByDefault;
            serialized.FindProperty("_modelAsset").objectReferenceValue =
                model;
            serialized.FindProperty("_idleAnimation").objectReferenceValue =
                idleAnimation;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static CharacterSeed[] CreateSeeds()
        {
            return new[]
            {
                new CharacterSeed(
                    EllenId,
                    "艾莲·乔",
                    "维多利亚家政的鲨鱼希人女仆。当前可操控角色，擅长高速近战。",
                    "维多利亚家政",
                    "强攻 / 冰属性",
                    true,
                    "Assets/Arts/Player/Ellen/Model/TPOSS.fbx",
                    "Assets/Arts/Player/Ellen/AnimationFBX/Locomotion/" +
                    "Avatar_Female_Size02_Ellen_Ani_Idle.fbx"),
                new CharacterSeed(
                    BelleId,
                    "铃",
                    "绳匠“法厄同”成员，擅长情报支援与空洞探索。",
                    "Random Play",
                    "支援 / 探索",
                    false,
                    "Assets/Arts/Characters/Belle/Model/" +
                    "Avatar_Female_Size02_Belle_Model.fbx",
                    "Assets/Arts/Characters/Belle/Animations/C_Idle.fbx"),
                new CharacterSeed(
                    BillyId,
                    "比利·奇德",
                    "热情健谈的智能机械人，使用双枪进行远程压制。",
                    "狡兔屋",
                    "强攻 / 物理",
                    false,
                    "Assets/Arts/Characters/Billy/Model/Billy.fbx",
                    "Assets/Arts/Characters/Billy/Animations/B_Idle_B.fbx")
            };
        }

        private static TAsset LoadRequiredAsset<TAsset>(string path)
            where TAsset : UnityEngine.Object
        {
            return AssetDatabase.LoadAssetAtPath<TAsset>(path) ??
                   throw new InvalidOperationException(
                       $"角色内容依赖的资产不存在或类型不匹配：'{path}'。");
        }

        private static AnimationClip LoadAnimationClip(string path)
        {
            if (path.EndsWith(
                    ".asset",
                    StringComparison.OrdinalIgnoreCase))
            {
                return LoadRequiredAsset<AnimationClip>(path);
            }

            var clip = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<AnimationClip>()
                .FirstOrDefault(
                    candidate =>
                        !candidate.name.StartsWith(
                            "__preview__",
                            StringComparison.Ordinal));
            return clip ??
                   throw new InvalidOperationException(
                       $"角色待机 FBX 中没有可用动画片段：'{path}'。");
        }

        private static string SanitizeAssetName(string characterId)
        {
            return characterId.Replace('.', '_');
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var slash = path.LastIndexOf('/');
            var parent = path.Substring(0, slash);
            var name = path.Substring(slash + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        /// <summary>保存构建一名角色配置所需的确定性内容。</summary>
        private readonly struct CharacterSeed
        {
            /// <summary>创建一条角色内容种子。</summary>
            public CharacterSeed(
                string characterId,
                string displayName,
                string description,
                string faction,
                string combatRole,
                bool unlockedByDefault,
                string modelPath,
                string idleAnimationPath)
            {
                CharacterId = characterId;
                DisplayName = displayName;
                Description = description;
                Faction = faction;
                CombatRole = combatRole;
                UnlockedByDefault = unlockedByDefault;
                ModelPath = modelPath;
                IdleAnimationPath = idleAnimationPath;
            }

            /// <summary>获取角色稳定标识。</summary>
            public string CharacterId { get; }

            /// <summary>获取角色中文展示名。</summary>
            public string DisplayName { get; }

            /// <summary>获取角色简介。</summary>
            public string Description { get; }

            /// <summary>获取角色阵营。</summary>
            public string Faction { get; }

            /// <summary>获取角色战斗定位。</summary>
            public string CombatRole { get; }

            /// <summary>获取角色是否默认解锁。</summary>
            public bool UnlockedByDefault { get; }

            /// <summary>获取模型资产路径。</summary>
            public string ModelPath { get; }

            /// <summary>获取待机动画资产路径。</summary>
            public string IdleAnimationPath { get; }
        }
    }
}
#endif
